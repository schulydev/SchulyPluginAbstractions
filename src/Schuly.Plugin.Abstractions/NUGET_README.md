# Schuly.Plugin.Abstractions

![Schuly](https://raw.githubusercontent.com/schulydev/Schuly/main/assets/app_icon.png)

The stable plugin contract for the [Schuly backend](https://github.com/schulydev/SchulyBackend). Implement these interfaces to extend the backend with background tasks, event handlers, and integrations — without vendoring backend source.

## What's in the package

- `ISchulyPlugin` — entry point: register services, endpoints, migrations
- `IPluginBackgroundTask` — recurring background work
- `IPluginEventHandler<TCommand>` — react to application commands
- `IPluginUserContext` — read user claims / roles from inside a plugin

## Install

```sh
dotnet add package Schuly.Plugin.Abstractions
```

## Use

```csharp
using Schuly.Plugin.Abstractions;

public class MyPlugin : ISchulyPlugin
{
    public string Name => "MyPlugin";
    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services, PluginServiceContext ctx) { }
    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) { }
    public Task MigrateAsync(IServiceProvider sp, CancellationToken ct = default) => Task.CompletedTask;
}
```

The backend's plugin host discovers and runs your plugin automatically.

## Links

- [SchulyBackend](https://github.com/schulydev/SchulyBackend) — consumes this package
- [SchulyPlugins](https://github.com/schulydev/SchulyPlugins) — official plugins
- [Source](https://github.com/schulydev/SchulyPluginAbstractions)
- [schuly.dev](https://schuly.dev)

## Versioning

Semver. Any interface change is tagged `breaking-change` and bumps the major version.

## License

MIT
