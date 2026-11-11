# Contract reference

Use this page to look up each interface and record in the plugin contract.

The package targets `net10.0`. Types live in the `Schuly.Plugin.Abstractions` namespace, except the
linking types (`ILinkProvider` and related), which live in `Schuly.Plugin.Abstractions.Linking`. The
signatures below match the source in
[`src/Schuly.Plugin.Abstractions/`](https://github.com/schulydev/SchulyPluginAbstractions/tree/main/src/Schuly.Plugin.Abstractions).

Plugins never touch the backend's entities or database. The host registers these services in each plugin's
container:

| Service | Purpose |
|---|---|
| `IPluginUserContext` | The current user and school user |
| `ISchoolData` | Write school data |
| `IPluginVault` | The plugin's own encrypted secret store |
| `IPluginFileStore` | Store downloaded files in Schuly's document storage |
| `ILinkedAccounts` | Accounts a user linked with an outside provider, shared by all plugins |

The host also provides `ILogger<T>`, `IHttpClientFactory`, `IHttpContextAccessor`, and `IConfiguration`.

A plugin keeps its own state, such as accounts and sync cursors, in its own database through
`PluginServiceContext.ConnectionString`.

## `ISchulyPlugin`

The plugin entry point. The host creates one instance per plugin and takes it through the lifecycle. It
calls the methods in the order shown in the table.

```csharp
public interface ISchulyPlugin
{
    string Name { get; }
    string Version { get; }
    void ConfigureServices(IServiceCollection services, PluginServiceContext context);
    void ConfigureEndpoints(IEndpointRouteBuilder endpoints);
    Task MigrateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}
```

| Member | Purpose | When |
|---|---|---|
| `Name` | Stable plugin identifier. The host derives the plugin's database name, vault namespace, and environment variable prefix from it. | Read at load. |
| `Version` | The plugin's own version string. | Read at load. |
| `ConfigureServices(IServiceCollection, PluginServiceContext)` | Register services, handlers, and options in the plugin's own service container. | First, while the host builds the plugin's container. |
| `MigrateAsync(IServiceProvider, CancellationToken)` | Run plugin-owned EF Core migrations (`db.Database.MigrateAsync()`). | Second, after the plugin's container is built. |
| `ConfigureEndpoints(IEndpointRouteBuilder)` | Map the plugin's HTTP endpoints. | Third, after migrations. |

### `PluginServiceContext`

The context passed to `ConfigureServices`.

```csharp
public record PluginServiceContext(string ConnectionString, IConfiguration Configuration);
```

| Member | Purpose |
|---|---|
| `ConnectionString` | The plugin's database connection string. The host gives each plugin its own database. |
| `Configuration` | The host `IConfiguration` with the plugin's `<AssemblyName>.yml` and `SCHULY_PLUGIN_<NAME>_` environment variables on top. |

## `IPluginBackgroundTask`

Recurring background work. The host's scheduler runs `ExecuteAsync` on the task's `Schedule`. A task never
overlaps with itself.

```csharp
public interface IPluginBackgroundTask
{
    string Name { get; }
    PluginSchedule Schedule { get; }
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
```

| Member | Purpose |
|---|---|
| `Name` | Task identifier, for logging and diagnostics. The host also uses it to find the task and as the key for schedule overrides, so keep it stable and unique within the plugin. |
| `Schedule` | The task's default schedule. The host operator can override the cadence. |
| `ExecuteAsync(IServiceProvider, CancellationToken)` | One execution of the work. Resolve scoped services from `serviceProvider`. |

### `PluginSchedule`

The plugin's default schedule for a background task, as a scheduler-agnostic value with no dependency
beyond the BCL. The host maps it onto its own scheduler, and the host operator can override the cadence per
deployment. Treat these values as a default, not a guarantee.

```csharp
public sealed record PluginSchedule(string Cron, int Retries = 0, IReadOnlyList<TimeSpan>? RetryIntervals = null, bool RunOnStartup = false)
```

| Member | Purpose |
|---|---|
| `Cron` | Standard 5-field cron expression (`minute hour day-of-month month day-of-week`). Validated in the constructor and on every `with`-expression. An invalid expression throws `ArgumentException`. |
| `Retries` | Number of times the host retries a failed execution. Must be `>= 0`, or it throws `ArgumentOutOfRangeException`. |
| `RetryIntervals` | Delay before each retry. When there are more retries than intervals, the host reuses the last interval for the remaining attempts. `null` means the host's default applies. A negative interval throws `ArgumentOutOfRangeException`. |
| `RunOnStartup` | Whether the host also runs the task once immediately at startup. |

| Factory | Produces |
|---|---|
| `Every(TimeSpan interval)` | A fixed-interval schedule. It accepts only whole minutes dividing 60 (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60) or whole hours dividing 24 (1, 2, 3, 4, 6, 8, 12, 24). Anything else can't be expressed as a true fixed-cadence cron and throws `ArgumentOutOfRangeException`. |
| `Daily(int hour, int minute = 0)` | A schedule that fires once a day at `hour:minute`. An out-of-range value throws `ArgumentOutOfRangeException`. |
| `Hourly(int minute = 0)` | A schedule that fires once an hour at `minute`. An out-of-range value throws `ArgumentOutOfRangeException`. |

```csharp
public sealed class SyncTimetableTask : IPluginBackgroundTask
{
    public string Name => "schulware.sync-timetable";
    public PluginSchedule Schedule => PluginSchedule.Every(TimeSpan.FromMinutes(30));

    public Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken) => Task.CompletedTask;
}
```

For a schedule the factories can't express, construct `PluginSchedule` directly with a cron string and,
optionally, retry behavior:

```csharp
public PluginSchedule Schedule => new("0 6 * * MON-FRI", Retries: 3, RetryIntervals: [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)]);
```

## `IPluginEventHandler<TCommand>`

React to a backend command or event. `TCommand` is contravariant (`in TCommand`). Register the handler in
`ConfigureServices`. If a handler throws, the host logs the error and continues with the other handlers.

```csharp
public interface IPluginEventHandler<in TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `HandleAsync(TCommand, CancellationToken)` | Handle one dispatched command. |

### `PluginUserPurgeEvent`

The host dispatches this event when a Schuly user is being deleted. Handle it with
`IPluginEventHandler<PluginUserPurgeEvent>` to wipe the plugin's own data for that user: its database rows
and the vault secrets keyed by the user. The host deletes core-owned data itself, but it can reach a
plugin's storage only through the plugin.

```csharp
public sealed record PluginUserPurgeEvent(Guid ApplicationUserId);
```

## `IPluginUserContext`

Read the current user and school user from inside a plugin.

```csharp
public interface IPluginUserContext
{
    Task<Guid> GetCurrentUserIdAsync(CancellationToken cancellationToken = default);
    Task<Guid?> GetCurrentSchoolUserIdAsync(CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `GetCurrentUserIdAsync(CancellationToken)` | The current application user's id. |
| `GetCurrentSchoolUserIdAsync(CancellationToken)` | The current school-user id, or `null` if none is in context. |

## `IPluginLogin`

A plugin's account-connect contract, and the source of its school-system catalog entry.

1. The plugin exposes a `SchoolSystem` descriptor. The host collects it from each loaded plugin and adds it
   to the catalog by `Key`, so the operator supplies no catalog config. The display name, logo, enabled
   state, and sort order are seeded once. The login settings follow the plugin each time it loads.
2. The host exposes one unified login endpoint. It resolves the `IPluginLogin` whose `SystemKey` matches the
   requested system and calls `ConnectAsync` with the login-field values the app collected.
3. The plugin reads the current user through `IPluginUserContext`, authenticates against its provider,
   saves the account, and returns its id. No provider auth lives in the backend.

```csharp
public interface IPluginLogin
{
    SchoolSystemDescriptor SchoolSystem { get; }

    string SystemKey => SchoolSystem.Key;

    Task<PluginLoginResult> ConnectAsync(IReadOnlyDictionary<string, string> fields, string? displayName, CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `SchoolSystem` | The catalog entry this login serves, which the app renders. See `SchoolSystemDescriptor`. |
| `SystemKey` | The catalog system key this login handles, such as `"schulnetz"`. A default member that returns `SchoolSystem.Key`, so you implement only `SchoolSystem`. |
| `ConnectAsync(IReadOnlyDictionary<string,string>, string?, CancellationToken)` | Connect an account from the collected login fields, keyed by the descriptor's `LoginFields` keys (such as `"email"`, `"password"`, `"baseUrl"`). `displayName` is an optional friendly name. |

### `SchoolSystemDescriptor`

Build this record in your `IPluginLogin`.

| Member | Purpose |
|---|---|
| `Key` | Required. The system key, such as `"schulnetz"`. |
| `DisplayName` | Required. The name the app shows. |
| `LoginMethod` | `"credentials"` (default) or `"oauth-webview"`. |
| `LogoUrl` | Optional logo. |
| `PrivateAuthStrategy` | `"token"` (headless login mints a bearer token) or `"scrape"` (credentials replayed per fetch). |
| `StatelessBasePath` | Private-mode endpoints, such as `"/api/plugins/schulware/stateless"`. |
| `PluginBasePath` | Account-mode endpoints, such as `"/api/plugins/schulware"`. |
| `LoginMethodPath` | Anonymous endpoint the app calls (GET, `?baseUrl=`) once the school's URL is entered. It answers a `LoginMethodResult`. |
| `Enabled` | Whether the system is enabled. Defaults to `true`. |
| `SortOrder` | Position in the catalog. |
| `LoginFields` | The `SchoolSystemLoginFieldDescriptor` inputs the app renders to collect what `ConnectAsync` needs. |

The constant `SchoolSystemDescriptor.LinkedAccountFieldKey` (`"linkedAccountId"`) is the login field that
carries the chosen linked account's id.

### `SchoolSystemLoginFieldDescriptor`

| Member | Purpose |
|---|---|
| `Key` | Required. Submitted with the collected value, such as `"baseUrl"`. |
| `Label` | Required. The field label. |
| `Type` | `"url"`, `"text"` (default), or `"password"`. |
| `Placeholder` | Optional placeholder. |
| `DefaultValue` | Optional default value. |
| `Required` | Defaults to `true`. |
| `ReplacedByLinkedAccount` | Hidden when the school signs in with a linked account, such as email, password, and 2FA fields. |
| `Suggestions` | Known values the app offers to pick from, such as schools and their URLs, as `LoginFieldSuggestion`. The user can still enter any other value. |

```csharp
public SchoolSystemDescriptor SchoolSystem => new()
{
    Key = "schulnetz",
    DisplayName = "Schulnetz",
    LoginMethod = "credentials",
    PrivateAuthStrategy = "token",
    StatelessBasePath = "/api/plugins/schulware/stateless",
    PluginBasePath = "/api/plugins/schulware",
    LoginFields =
    [
        new()
        {
            Key = "baseUrl", Label = "Schulnetz URL", Type = "url", Required = true,
            Suggestions = [new("BBB Berufsfachschule Baden", "https://schulnetz.bbbaden.ch")],
        },
        new() { Key = "email",    Label = "Email",         Type = "text",     Required = true },
        new() { Key = "password", Label = "Password",      Type = "password", Required = true },
    ],
};
```

### Login method and browser sign-in

```csharp
public sealed record LoginFieldSuggestion(string Label, string Value);
public sealed record LoginMethodResult(string? LinkedProvider) { public string? BrowserLoginPath { get; init; } }
public sealed record BrowserLoginStart(string AuthUrl, string CallbackUrl, string State, string Context, string? CompletePath) { public const string CallbackFieldKey = "browserCallbackUrl"; public const string ContextFieldKey = "browserContext"; }
```

`LoginMethodPath` answers a `LoginMethodResult`. A null `LinkedProvider` with no `BrowserLoginPath` means
plain credentials.

- **Linked account.** Set `LinkedProvider` when the school signs in with a linked account. The app shows
  that provider's linked accounts and submits `linkedAccountId` instead of the fields marked
  `ReplacedByLinkedAccount`.
- **Browser sign-in.** Set `BrowserLoginPath` when the school signs in through a browser, such as a
  cantonal login no server can fill in. The app hides the fields marked `ReplacedByLinkedAccount` and POSTs
  `{ "baseUrl": ... }` to `BrowserLoginPath`, which returns a `BrowserLoginStart`. The app opens `AuthUrl`,
  waits for a URL starting with `CallbackUrl` whose `state` matches `State`, and submits that URL and
  `Context` as the `browserCallbackUrl` and `browserContext` fields: to `ConnectAsync` in account mode, or
  to `CompletePath` in private mode.

### `PluginLoginResult`

The outcome of `ConnectAsync`.

```csharp
public record PluginLoginResult(bool Success, Guid? AccountId = null, string? Message = null);
```

| Member | Purpose |
|---|---|
| `Success` | Whether the connect succeeded. |
| `AccountId` | The saved account's id when successful. |
| `Message` | Optional human-readable detail, such as an error reason. |

## `ISchoolData`

The plugin's write access to Schuly's school data. The host stamps each row with the calling plugin as its
source and lets a plugin write only to students it provisioned. Writing to another plugin's student throws
`UnauthorizedAccessException`.

```csharp
public interface ISchoolData
{
    Task<Guid> EnsureSchoolAsync(SchoolRecord school, CancellationToken cancellationToken = default);
    Task<Guid> EnsureStudentAsync(Guid schoolId, Guid applicationUserId, StudentRecord student, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncAgendaAsync(Guid schoolUserId, IReadOnlyCollection<AgendaRecord> entries, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncGradesAsync(Guid schoolUserId, IReadOnlyCollection<GradeRecord> grades, CancellationToken cancellationToken = default);
    Task<SyncResult> ReplaceGradesAsync(Guid schoolUserId, IReadOnlyCollection<GradeRecord> grades, DateOnly since, CancellationToken cancellationToken = default) => SyncGradesAsync(schoolUserId, grades, cancellationToken);
    Task<SyncResult> SyncExamsAsync(Guid schoolUserId, IReadOnlyCollection<ExamRecord> exams, CancellationToken cancellationToken = default) => Task.FromResult(new SyncResult(0, 0));
    Task<SyncResult> SyncAbsencesAsync(Guid schoolUserId, IReadOnlyCollection<AbsenceRecord> absences, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncDocumentsAsync(Guid schoolUserId, IReadOnlyCollection<DocumentRecord> documents, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncSemesterReportsAsync(Guid schoolUserId, IReadOnlyCollection<SemesterReportRecord> reports, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetDocumentIdsAsync(Guid schoolUserId, CancellationToken cancellationToken = default);
}

public sealed record SyncResult(int Added, int Updated);
```

| Member | Purpose |
|---|---|
| `EnsureSchoolAsync` | Find a school by website (or by name when it has none), creating it if missing. Returns its id. |
| `EnsureStudentAsync` | Find the user's student profile at the school, creating it if missing, and fill blank fields. Returns the school-user id. |
| `SyncAgendaAsync`, `SyncGradesAsync`, `SyncAbsencesAsync`, `SyncDocumentsAsync` | Upsert records by `ExternalId`: unknown ids are added, known ones updated. Returns `SyncResult(Added, Updated)`. |
| `ReplaceGradesAsync` | `SyncGradesAsync`, then removes the plugin's grades with an exam dated on or after `since` that are not in the list. Pass the start of the period the plugin read completely. Undated grades are kept; an empty list removes nothing. Hosts older than 0.12 only upsert. |
| `SyncExamsAsync` | Upsert exams by subject and name. `ExamRecord` has no `ExternalId`. Returns `SyncResult(Added, Updated)`. |
| `SyncSemesterReportsAsync` | Upsert semester reports by program code, school year, and half-year. Each report's subjects are replaced as a whole. Returns `SyncResult(Added, Updated)`. |
| `GetDocumentIdsAsync` | External ids of documents this plugin already synced for the student, so it can skip re-downloading them. |

`SyncGradesAsync` creates the subject (class) and exam for each grade as needed and enrolls the student.
`SyncExamsAsync` does the same for exams without a grade yet, such as upcoming tests. Because an exam is
matched by subject and name, a later grade lands on the same exam. Hosts older than 0.8 ignore
`SyncExamsAsync`: the default implementation does nothing.

### Records

```csharp
public sealed record SchoolRecord(string Name, string? Website = null, string? LogoUrl = null);
public sealed record StudentRecord { public string? FirstName { get; init; } public string? LastName { get; init; } public string? Email { get; init; } public string? PrivateEmail { get; init; } public string? PhoneNumber { get; init; } public string? Street { get; init; } public string? City { get; init; } public string? Zip { get; init; } public DateOnly? Birthday { get; init; } public DateOnly? EntryDate { get; init; } public DateOnly? LeaveDate { get; init; } public string? ProfilePictureUrl { get; init; } }
public enum AgendaKind { Event = 0, Lesson = 1, Test = 2, Holiday = 3 }
public sealed record AgendaRecord(string ExternalId, AgendaKind Kind, string Title, DateTimeOffset Start, DateTimeOffset? End = null, string? Place = null, string? Description = null);
public sealed record GradeRecord(string ExternalId, string Subject, string ExamName, decimal Score, decimal Weight = 1, DateOnly? ExamDate = null) { public decimal? ClassAverage { get; init; } }
public sealed record ExamRecord(string Subject, string Name, DateOnly Date) { public string? Description { get; init; } public decimal? ClassAverage { get; init; } }
public sealed record AbsenceRecord(string ExternalId, DateTimeOffset From, DateTimeOffset Until, string? Reason = null, bool IsLate = false) { public bool? IsExcused { get; init; } }
public sealed record SemesterReportRecord(string ProgramCode, int SchoolYearStart, int SemesterHalf, string ClassName, IReadOnlyList<SemesterSubjectRecord> Subjects, string? PromotionDecision = null, decimal? GradeAverage = null, int? ExcusedAbsences = null, int? UnexcusedAbsences = null);
public sealed record SemesterSubjectRecord(string SubjectCode, string SubjectName, decimal? Grade = null, string? Marker = null, string? SubjectTypeMarker = null);
public sealed record DocumentRecord(string ExternalId, string Title, string? FileName = null, string? Category = null, string? Comment = null, string? EnteredBy = null, StoredFile? File = null);
```

- **Times are `DateTimeOffset`.** Convert provider wall-clock times with the provider's time zone before you
  build a record. Schuly stores UTC only. A date-only entry, such as a holiday or a full-day absence, starts
  at UTC midnight of its day.
- **`ExternalId` must be stable** across syncs for the same item. Use the provider's own id where there is
  one, otherwise a key built from fields that don't change. The host rejects an id longer than 300
  characters with `ArgumentException`.
- **`GradeRecord`:** `Subject` becomes the class, and `ExamName` the exam within it. `ClassAverage` is the
  class average the school reports, when it publishes one.
- **`ExamRecord`** matches the exam a `GradeRecord` with the same `Subject` and `Name` creates.
- **`AbsenceRecord.IsExcused`** says whether the school has accepted an excuse. It's `null` when the school
  doesn't say.
- **`SemesterReportRecord`:** `SchoolYearStart` is the year the school year begins (2025 for 25/26), and
  `SemesterHalf` is 1 or 2.
- **`SemesterSubjectRecord`** has a numeric `Grade`, or a `Marker` such as `"disp"` when there is none.
- **`DocumentRecord.File`** is what `IPluginFileStore.SaveAsync` returned, if you downloaded the file.

## `IPluginVault`

The plugin's own encrypted secret store for tokens and credentials. Each plugin gets its own namespace.
Values are encrypted at rest (AES-256-GCM) and readable only through this vault.

```csharp
public interface IPluginVault
{
    void Set(string key, string value);
    string? Get(string key);
    bool TryGet(string key, [NotNullWhen(true)] out string? value);
    bool Contains(string key);
    bool Remove(string key);
    void Clear();
    int Count { get; }
}
```

## `IPluginFileStore`

Stores a downloaded file, such as a report card, in Schuly's encrypted document storage. Pass the returned
`StoredFile` in a `DocumentRecord` so the app can download it.

```csharp
public interface IPluginFileStore
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string? contentType, CancellationToken cancellationToken = default);
}

public sealed record StoredFile(string Key, long SizeBytes);
```

## `ILinkedAccounts`

Accounts a user linked with an outside identity provider, such as Microsoft, shared by every plugin: one
plugin links the account, another signs in with it. Each call is scoped to the given user. Secrets are
encrypted at rest.

```csharp
public interface ILinkedAccounts
{
    Task<IReadOnlyList<LinkedAccount>> ListAsync(Guid applicationUserId, string provider, CancellationToken cancellationToken = default);
    Task<LinkedAccount?> GetAsync(Guid applicationUserId, Guid linkedAccountId, CancellationToken cancellationToken = default);
    Task<LinkedAccount> SaveAsync(Guid applicationUserId, LinkedAccountRecord account, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(Guid applicationUserId, Guid linkedAccountId, CancellationToken cancellationToken = default);
    Task<string?> GetSecretAsync(Guid applicationUserId, Guid linkedAccountId, string name, CancellationToken cancellationToken = default);
    Task SetSecretAsync(Guid applicationUserId, Guid linkedAccountId, string name, string value, CancellationToken cancellationToken = default);
    Task<bool> SetPictureAsync(Guid applicationUserId, Guid linkedAccountId, Stream? content, string? contentType, CancellationToken cancellationToken = default) => Task.FromResult(false);
    Task<bool> SetSchoolUserPictureAsync(Guid applicationUserId, Guid schoolUserId, Stream? content, string? contentType, CancellationToken cancellationToken = default) => Task.FromResult(false);
}

public sealed record LinkedAccountRecord(string Provider, string ExternalId, string Email, string? DisplayName);
public sealed record LinkedAccount(Guid Id, string Provider, string ExternalId, string Email, string? DisplayName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
```

| Member | Purpose |
|---|---|
| `SaveAsync` | Adds the account, or updates it when the user already linked this `ExternalId` at the provider. |
| `RemoveAsync` | Removes the account, its secrets and its picture. |
| `SetPictureAsync` | Since 0.11.3. Stores the account's profile picture (PNG, JPEG or WebP, at most 2 MB) in the host's encrypted storage; `null` content removes it. Only the account owner sees it, and it is deleted with the account. Returns `false` when the host rejects the picture or is older and runs the default. |
| `SetSchoolUserPictureAsync` | Since 0.11.4. Stores the picture of a school account (student profile) the user owns, such as the photo on a student ID card, under the same rules as `SetPictureAsync`. Account rows whose `SchoolUserId` points at it show it ahead of a linked account's picture. Returns `false` when the school user is not the user's, the host rejects the picture or is older and runs the default. |
| `LinkedProviders.Microsoft` | The provider key `"microsoft"`. |
| `LinkedSecrets.RefreshToken` | The secret name `"refresh_token"`: the provider's OAuth refresh token. |
| `LinkedSecrets.SessionCookies` | The secret name `"session_cookies"`: the provider's sign-in session as a JSON array of `{domain, name, path, value}` cookies. |

## `ILinkProvider`

Host-rendered account linking (namespace `Schuly.Plugin.Abstractions.Linking`, since 0.11). A plugin declares
a descriptor and drives a multi-step flow. The host and the clients render whatever steps it returns, so no
client hardcodes plugin-specific screens. Register it with
`services.AddScoped<ILinkProvider, XLinkProvider>()`.

The host reads each descriptor once at load. It logs and skips an invalid or duplicate one.

```csharp
public interface ILinkProvider
{
    LinkProviderDescriptor Descriptor { get; }
    Task<LinkStepResult> StartAsync(LinkStartContext context, CancellationToken cancellationToken = default);
    Task<LinkStepResult> SubmitAsync(LinkSubmitContext context, CancellationToken cancellationToken = default);
    Task<LinkStepResult> PollAsync(LinkPollContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LinkedAccountSummary>> ListAccountsAsync(Guid applicationUserId, CancellationToken cancellationToken = default);
    Task<bool> UnlinkAsync(Guid applicationUserId, string accountId, CancellationToken cancellationToken = default);
}
```

| Type | Purpose |
|---|---|
| `LinkProviderDescriptor` | `Id` (`^[a-z0-9][a-z0-9-]{1,40}$`), `DisplayName`, `Description`, `Icon`, `Capabilities`, `Flows`, `SortOrder`. |
| `LinkIcon` | `Lucide` (kebab-case icon name, such as `"graduation-cap"`) or `Svg` (inline markup). The host sanitizes the SVG and serves it from its own URL with a strict CSP. |
| `ProviderCapability` | A feature the provider's linked accounts unlock. Clients ship generic UIs per kind and call the plugin at `BasePath`. Known kinds: `mail`, `chat`, `drive`, `notes`, `calendar`, `absence_reporting`, `school_data`. |
| `LinkFlowDescriptor` | `Id` (same pattern as the provider id), `Label`, `Description`, `Platforms` (`LinkPlatforms`, defaults to `All`). |
| `LinkStepResult(Step, State)` | The next step plus opaque plugin state. `Done(accountId, title, state)` and `Fail(message)` build the terminal steps. Their step ids are `DoneStepId` (`"done"`) and `ErrorStepId` (`"error"`). |
| `LinkStep` | `Id`, which the client echoes on submit. `Kind` (`Form`, `Otp`, `PushCode`, `DeviceCode`, `Redirect`, `Select`, `Done`, `Error`). `Title`, `Description`, inline `Error`, and `SubmitLabel`. The payload for the kind: `Fields`, `Otp`, `PushCode`, `DeviceCode`, `Redirect`, `Options`, `Done`. `Actions` are alternative buttons, submitted as the action id. |
| `LinkField` | Form field: `Key` (`^[A-Za-z][A-Za-z0-9_]{0,40}$`), `Label`, `Type`, `Placeholder`, `Help`, `DefaultValue`, `Required`, `Secret`, `MaxLength`, `Pattern` (.NET regex), `Options`, `AllowCustom`. |
| `LinkOption` | `LinkProviderId`, when set, means "link an account at that provider first". The client runs that provider's link flow, then resubmits this step with action `refresh`. |
| `LinkRedirect` | `HostCallback`: the browser returns to `LinkStartContext.HostCallbackUrl`. `Capture`: a mobile WebView intercepts the first URL starting with `CallbackPrefix`, then submits `callbackUrl` and `cookies` for `CaptureCookieDomains`. |
| `LinkedAccountSummary` | Row returned by `ListAccountsAsync`: `Status` (`Ok`, `Syncing`, `Expired`, `Error`; clients poll while `Syncing`) and optional `LastSyncedAt`. Since 0.11.3 the optional `SchoolUserId` (the student profile the account syncs into) or `LinkedAccountId` (its `ILinkedAccounts` row) let the host show that profile's or account's picture on the row. |

The positional records and enums:

```csharp
public enum LinkPlatform { Web, Mobile }
[Flags] public enum LinkPlatforms { None = 0, Web = 1, Mobile = 2, All = Web | Mobile }
public enum LinkStepKind { Form, Otp, PushCode, DeviceCode, Redirect, Select, Done, Error }
public enum LinkFieldType { Text, Email, Password, Select, Url, TotpSecret }
public enum LinkRedirectMode { HostCallback, Capture }
public enum LinkedAccountStatus { Ok, Syncing, Expired, Error }

public sealed record ProviderCapability(string Kind, string Label, string? Icon, string? BasePath);
public sealed record LinkStartContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string? ReauthAccountId, string HostCallbackUrl);
public sealed record LinkSubmitContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string StepId, string? State, IReadOnlyDictionary<string, string> Values, string? Action, string? ReauthAccountId, string HostCallbackUrl);
public sealed record LinkPollContext(Guid ApplicationUserId, string FlowId, LinkPlatform Platform, string StepId, string? State, string? ReauthAccountId);
public sealed record LinkStepResult(LinkStep Step, string? State);
public sealed record LinkOption(string Value, string Label, string? Detail = null, string? Icon = null) { public string? LinkProviderId { get; init; } }
public sealed record LinkAction(string Id, string Label);
public sealed record LinkOtp(int Length, string? Label, bool NumericOnly = true);
public sealed record LinkPushCode(string? Number, DateTimeOffset ExpiresAt, int PollIntervalSeconds);
public sealed record LinkDeviceCode(string UserCode, string VerificationUri, DateTimeOffset ExpiresAt, int PollIntervalSeconds);
public sealed record LinkRedirect(string Url, LinkRedirectMode Mode, string? CallbackPrefix, IReadOnlyList<string> CaptureCookieDomains);
public sealed record LinkDone(string AccountId, string? Title);
public sealed record LinkedAccountSummary(string Id, string Title, string? Detail, LinkedAccountStatus Status, DateTimeOffset? LinkedAt, string? Message, DateTimeOffset? LastSyncedAt = null) { public Guid? SchoolUserId { get; init; } public Guid? LinkedAccountId { get; init; } }
```

### Security rules

- The host carries `State` (encrypted, in memory, at most 10 minutes) and never sends it to clients. It must
  never hold passwords or other user-entered secrets.
- Neither the host nor the plugin logs or stores fields with `Secret = true`, or any submitted value. Don't
  put submitted values in error messages.
- User-entered secrets (passwords, TOTP secrets, OTP codes, refresh tokens) must never be logged, saved in
  plain text, or placed in `State`. Captured cookies are secrets too, so never log them.
- `Capture` redirects are mobile-only. The host rejects them for the web platform. Use `HostCallback` only
  when the provider really sends the browser back to the host.
- Verification and redirect URLs must be https.

### Validators

The host uses these pure helpers, which have no dependencies:

- `LinkStepValidator.Validate(LinkStep step, IReadOnlyDictionary<string, string> values, string? action)`
  checks a submit against the current step.
  - It rejects unknown keys.
  - It enforces required fields, `MaxLength` (default 2048), `Pattern` (100 ms timeout), email shape, https
    URLs (http only for localhost), and select options (unless `AllowCustom`).
  - An Otp step takes key `code` (exactly `Length` chars, digits if `NumericOnly`). A Select step takes key
    `value`. A Capture redirect takes `callbackUrl` and optional `cookies`.
  - `action` must be one of the step's actions or the reserved `refresh` or `cancel`. With an action, the
    required checks are skipped.
  - Messages name field keys, never values.
- `LinkDescriptorValidator.ValidateDescriptor(LinkProviderDescriptor)` and
  `ValidateStep(LinkStep, LinkPlatform)` check ids, keys, https-only URLs, and Capture-on-web.
  `IsValidId(string?)` and `IsValidFieldKey(string?)` check a single id or key.

Both validators return `LinkValidationResult`:

```csharp
public sealed record LinkValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

Its `Valid` property is the success result, and `FromErrors(errors)` builds a result from a list of errors.

See [versioning](versioning.md) for the rules that govern changes to any of these members.
