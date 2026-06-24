# Contract reference

Everything in the package lives in the `Schuly.Plugin.Abstractions` namespace and targets
`net10.0`. The contract is **5 interfaces** plus two small records. All signatures below are
copied verbatim from source under `src/Schuly.Plugin.Abstractions/`.

> The package also ships the backend's `Schuly.Domain.dll` and `Schuly.Infrastructure.dll`
> alongside the abstractions assembly (see the csproj), so plugins can use the backend's
> typed entities and DbContext for direct DB access. See
> [development](setup/development.md) for how those are referenced.

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
on the configured `Interval`.

```csharp
public interface IPluginBackgroundTask
{
    string Name { get; }
    TimeSpan Interval { get; }
    Task ExecuteAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken);
}
```

| Member | Purpose |
|---|---|
| `Name` | Task identifier (for logging/diagnostics). |
| `Interval` | How often the host runs the task. |
| `ExecuteAsync(IServiceProvider, CancellationToken)` | One execution of the work. Resolve scoped services from `serviceProvider`. |

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

A plugin's account-connect contract — **and the source of its school-system catalog
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
    string SystemKey => SchoolSystem.Key;          // default member — defaults to SchoolSystem.Key

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
        new() { Key = "baseUrl",  Label = "Schulnetz URL", Type = "url",      Required = true },
        new() { Key = "email",    Label = "Email",         Type = "text",     Required = true },
        new() { Key = "password", Label = "Password",      Type = "password", Required = true },
    ],
};
```

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

---

See [versioning](versioning.md) for the rules that govern changes to any of these members.
