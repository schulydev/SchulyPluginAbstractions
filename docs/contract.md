# Contract reference

Everything in the package lives in the `Schuly.Plugin.Abstractions` namespace and targets
`net10.0`. All signatures below are copied verbatim from source under
`src/Schuly.Plugin.Abstractions/`.

Plugins never touch the backend's entities or database. The host registers these services in
every plugin's container:

| Service | Purpose |
|---|---|
| `IPluginUserContext` | The current user / school-user |
| `ISchoolData` | Write school data (see below) |
| `IPluginVault` | The plugin's own encrypted secret store |
| `IPluginFileStore` | Store downloaded files in Schuly's document storage |

A plugin keeps its own state (accounts, sync cursors) in its own database via
`PluginServiceContext.ConnectionString`.

## `ISchulyPlugin`

The plugin entry point. The backend instantiates one per plugin and drives it through its
lifecycle.

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
| `Name` | Stable plugin identifier. | Read at discovery. |
| `Version` | Plugin's own version string. | Read at discovery. |
| `ConfigureServices(IServiceCollection, PluginServiceContext)` | Register services, handlers, and options into the host DI container. | At startup, before the app is built. |
| `ConfigureEndpoints(IEndpointRouteBuilder)` | Map the plugin's HTTP endpoints. | At startup, after services are built. |
| `MigrateAsync(IServiceProvider, CancellationToken)` | Run plugin-owned EF Core migrations (`db.Database.MigrateAsync()`). | At startup, after the service provider is available. |

### `PluginServiceContext`

The context passed to `ConfigureServices`.

```csharp
public record PluginServiceContext(string ConnectionString, IConfiguration Configuration);
```

| Member | Purpose |
|---|---|
| `ConnectionString` | The plugin's database connection string (the host scopes each plugin to its own database). |
| `Configuration` | The host `IConfiguration`, for reading plugin options. |

## `IPluginBackgroundTask`

Recurring background work. The backend's `PluginBackgroundTaskHost` invokes `ExecuteAsync`
on the task's declared `Schedule`.

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
| `Name` | Task identifier (for logging/diagnostics). |
| `Schedule` | The task's default schedule; the host operator can override the cadence. |
| `ExecuteAsync(IServiceProvider, CancellationToken)` | One execution of the work. Resolve scoped services from `serviceProvider`. |

### `PluginSchedule`

The plugin's default schedule for a background task, as a scheduler-agnostic value - it
depends on nothing beyond the BCL. The host maps it onto its own scheduler; the
host operator can override the cadence per deployment.

```csharp
public sealed record PluginSchedule(string Cron, int Retries = 0, IReadOnlyList<TimeSpan>? RetryIntervals = null, bool RunOnStartup = false)
```

| Member | Purpose |
|---|---|
| `Cron` | Standard 5-field cron expression (`minute hour day-of-month month day-of-week`). Validated in the constructor and on every `with`-expression; an invalid expression throws `ArgumentException`. |
| `Retries` | Number of times the host retries a failed execution. Must be `>= 0`. |
| `RetryIntervals` | Delay before each retry. When there are more retries than intervals, the host reuses the last interval for the remaining attempts. `null` means the host's own default backoff applies. |
| `RunOnStartup` | Whether the host should also run the task once immediately at startup. |

| Factory | Produces |
|---|---|
| `Every(TimeSpan interval)` | A fixed-interval schedule. Only whole minutes dividing 60 (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60) or whole hours dividing 24 (1, 2, 3, 4, 6, 8, 12, 24) are accepted - anything else can't be expressed as a true fixed-cadence cron and throws `ArgumentOutOfRangeException`. |
| `Daily(int hour, int minute = 0)` | A schedule that fires once a day at `hour:minute`. |
| `Hourly(int minute = 0)` | A schedule that fires once an hour at `minute`. |

```csharp
public sealed class SyncTimetableTask : IPluginBackgroundTask
{
    public string Name => "schulware.sync-timetable";
    public PluginSchedule Schedule => PluginSchedule.Every(TimeSpan.FromMinutes(30));

    public Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken) => Task.CompletedTask;
}
```

For a schedule the factories can't express, construct `PluginSchedule` directly with a cron
string and, optionally, retry behaviour:

```csharp
public PluginSchedule Schedule => new("0 6 * * MON-FRI", Retries: 3, RetryIntervals: [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)]);
```

## `IPluginEventHandler<TCommand>`

React to a backend command. `TCommand` is contravariant (`in TCommand`).

```csharp
public interface IPluginEventHandler<in TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `HandleAsync(TCommand, CancellationToken)` | Handle one dispatched command. |

## `IPluginUserContext`

Read the current user / school-user from inside a plugin.

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

A plugin's account-connect contract - **and the source of its school-system catalog
entry**. The plugin exposes a `SchoolSystem` descriptor; the backend collects it from
every loaded plugin and seeds the catalog (seed-if-missing by `Key`), so the operator
no longer supplies catalog config. The backend then exposes a single unified login
endpoint, resolves the `IPluginLogin` whose `SystemKey` matches the requested system,
and calls `ConnectAsync` with the login-field values the app collected from that
descriptor. The plugin reads the current user via `IPluginUserContext`, authenticates
against its provider, persists the account, and returns its id. No provider auth lives
in the backend.

```csharp
public interface IPluginLogin
{
    SchoolSystemDescriptor SchoolSystem { get; }   // the catalog entry this login serves
    string SystemKey => SchoolSystem.Key;          // default member - defaults to SchoolSystem.Key

    Task<PluginLoginResult> ConnectAsync(
        IReadOnlyDictionary<string, string> fields,
        string? displayName,
        CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `SchoolSystem` | The catalog descriptor the app renders: `Key`, `DisplayName`, `LoginMethod`, `PrivateAuthStrategy` (`"token"`/`"scrape"`), `StatelessBasePath`, `PluginBasePath`, `SortOrder`, and the `LoginFields` the app collects. |
| `SystemKey` | The catalog system key this login handles, e.g. `"schulnetz"`. Default member returning `SchoolSystem.Key`; you only implement `SchoolSystem`. |
| `ConnectAsync(IReadOnlyDictionary<string,string>, string?, CancellationToken)` | Connect an account from the collected login fields, keyed by the descriptor's `LoginFields` keys (e.g. `"email"`, `"password"`, `"baseUrl"`). `displayName` is an optional friendly name. |

`SchoolSystemDescriptor` (and its `LoginFields` of `SchoolSystemLoginFieldDescriptor`)
live in `Schuly.Plugin.Abstractions`; build them in your `IPluginLogin`:

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

`LoginMethodPath` answers a `LoginMethodResult`. Set `LinkedProvider` when the school signs in with a linked account, or `BrowserLoginPath` when it signs in through a browser (for example a cantonal login no server can fill in). The app POSTs `{ "baseUrl": ... }` to `BrowserLoginPath` and gets a `BrowserLoginStart(AuthUrl, CallbackUrl, State, Context, CompletePath)`. It opens `AuthUrl`, waits for a URL starting with `CallbackUrl` whose `state` matches, and submits that URL and `Context` as the `browserCallbackUrl` and `browserContext` fields: to `ConnectAsync` in account mode, or to `CompletePath` in private mode.

`Suggestions` lists known values the app offers to pick from, as `LoginFieldSuggestion(Label, Value)`.
The user can still type any other value.

### `PluginLoginResult`

Outcome of `ConnectAsync`.

```csharp
public record PluginLoginResult(bool Success, Guid? AccountId = null, string? Message = null);
```

| Member | Purpose |
|---|---|
| `Success` | Whether the connect succeeded. |
| `AccountId` | The persisted account id when successful. |
| `Message` | Optional human-readable detail (e.g. an error reason). |

## `ISchoolData`

The plugin's write access to Schuly's school data. The host stamps every row with the calling
plugin as its source and only lets a plugin write to students it provisioned itself.

```csharp
public interface ISchoolData
{
    Task<Guid> EnsureSchoolAsync(SchoolRecord school, CancellationToken cancellationToken = default);
    Task<Guid> EnsureStudentAsync(Guid schoolId, Guid applicationUserId, StudentRecord student, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncAgendaAsync(Guid schoolUserId, IReadOnlyCollection<AgendaRecord> entries, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncGradesAsync(Guid schoolUserId, IReadOnlyCollection<GradeRecord> grades, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncExamsAsync(Guid schoolUserId, IReadOnlyCollection<ExamRecord> exams, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncAbsencesAsync(Guid schoolUserId, IReadOnlyCollection<AbsenceRecord> absences, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncDocumentsAsync(Guid schoolUserId, IReadOnlyCollection<DocumentRecord> documents, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetDocumentIdsAsync(Guid schoolUserId, CancellationToken cancellationToken = default);
    Task<SyncResult> SyncSemesterReportsAsync(Guid schoolUserId, IReadOnlyCollection<SemesterReportRecord> reports, CancellationToken cancellationToken = default);
}
```

| Member | Purpose |
|---|---|
| `EnsureSchoolAsync` | Find a school by website (or name), creating it if missing. Returns its id. |
| `EnsureStudentAsync` | Find the user's student profile at the school, creating it if missing, and fill blank fields. Returns the school-user id. |
| `Sync*Async` | Upsert records by `ExternalId`: unknown ids are added, known ones updated. Returns `SyncResult(Added, Updated)`. |
| `GetDocumentIdsAsync` | External ids of documents already synced, so a plugin can skip re-downloading them. |
| `SyncSemesterReportsAsync` | Upsert semester reports by program code, school year and half-year; each report's subjects are replaced as a whole. |

`SyncGradesAsync` creates the subject (class) and exam for each grade as needed and enrols the
student in it. `SyncExamsAsync` does the same for exams without a grade yet (for example upcoming
tests); an exam is matched by subject and name, so a later grade lands on the same exam. Hosts older
than 0.8 ignore it (the default implementation does nothing).

### Records

```csharp
public sealed record SchoolRecord(string Name, string? Website = null, string? LogoUrl = null);
public sealed record AgendaRecord(string ExternalId, AgendaKind Kind, string Title, DateTimeOffset Start, DateTimeOffset? End = null, string? Place = null, string? Description = null);
public sealed record GradeRecord(string ExternalId, string Subject, string ExamName, decimal Score, decimal Weight = 1, DateOnly? ExamDate = null) { public decimal? ClassAverage { get; init; } }
public sealed record ExamRecord(string Subject, string Name, DateOnly Date) { public string? Description { get; init; } public decimal? ClassAverage { get; init; } }
public sealed record AbsenceRecord(string ExternalId, DateTimeOffset From, DateTimeOffset Until, string? Reason = null, bool IsLate = false) { public bool? IsExcused { get; init; } }
public sealed record DocumentRecord(string ExternalId, string Title, string? FileName = null, string? Category = null, string? Comment = null, string? EnteredBy = null, StoredFile? File = null);
public enum AgendaKind { Event = 0, Lesson = 1, Test = 2, Holiday = 3 }
public sealed record SemesterReportRecord(string ProgramCode, int SchoolYearStart, int SemesterHalf, string ClassName, IReadOnlyList<SemesterSubjectRecord> Subjects, string? PromotionDecision = null, decimal? GradeAverage = null, int? ExcusedAbsences = null, int? UnexcusedAbsences = null);
public sealed record SemesterSubjectRecord(string SubjectCode, string SubjectName, decimal? Grade = null, string? Marker = null, string? SubjectTypeMarker = null);
```

`StudentRecord` has optional init-only profile fields (`FirstName`, `LastName`, `Email`,
`PrivateEmail`, `PhoneNumber`, `Street`, `City`, `Zip`, `Birthday`, `EntryDate`, `LeaveDate`,
`ProfilePictureUrl`).

**Times are `DateTimeOffset`.** Convert provider wall-clock times with the provider's time
zone before building a record; Schuly stores UTC only. A date-only entry (a holiday, a full-day
absence) starts at UTC midnight of its day.

**`ExternalId` must be stable** across syncs for the same item - the provider's own id where
there is one, otherwise a key built from fields that don't change.

## `IPluginVault`

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

Each plugin gets its own namespace; values are encrypted at rest (AES-256-GCM) and readable
only through this vault.

## `IPluginFileStore`

```csharp
public interface IPluginFileStore
{
    Task<StoredFile> SaveAsync(Stream content, string fileName, string? contentType, CancellationToken cancellationToken = default);
}

public sealed record StoredFile(string Key, long SizeBytes);
```

Stores a downloaded file in Schuly's encrypted document storage. Pass the returned
`StoredFile` in a `DocumentRecord` so the app can download it.

---

See [versioning](versioning.md) for the rules that govern changes to any of these members.
