# <p align="center">SchulyPluginAbstractions</p>
<p align="center">
  <img src="./assets/app_icon.png" width="96" alt="Schuly Logo">
</p>
<p align="center">
  <strong>The contract every Schuly plugin implements.</strong>
</p>
<p align="center">
  <a href="https://www.nuget.org/packages/Schuly.Plugin.Abstractions"><img src="https://img.shields.io/nuget/v/Schuly.Plugin.Abstractions?color=3da8ff&label=NuGet" alt="NuGet"/></a>
  <img src="https://img.shields.io/badge/.NET-10-3da8ff" alt=".NET 10"/>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-3da8ff" alt="MIT license"/></a>
</p>

## Install

```sh
dotnet add package Schuly.Plugin.Abstractions
```

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Schuly.Plugin.Abstractions;

public sealed class MyPlugin : ISchulyPlugin
{
    public string Name => "My School";
    public string Version => "1.0.0";

    public void ConfigureServices(IServiceCollection services, PluginServiceContext context) { }

    public void ConfigureEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/api/plugins/myschool/hello", () => "Hello from my plugin");

    public Task MigrateAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
```

That's a whole plugin. [SchulyServer](https://github.com/schulydev/SchulyServer) finds the class, and the plugin never touches the server's source or database.

## Beyond that

Implement these when your plugin needs them:

- `IPluginLogin`: connect a school account, and put your school system in the app's picker
- `IPluginBackgroundTask`: recurring work on a `PluginSchedule`
- `IPluginEventHandler<TCommand>`: react to application commands
- `ILinkProvider`: link an outside account

The server hands you `ISchoolData`, `IPluginVault`, `IPluginFileStore`, `IPluginUserContext` and `ILinkedAccounts`.

[The contract](https://docs.schuly.dev/SchulyPluginAbstractions/contract) has every member, [versioning](https://docs.schuly.dev/SchulyPluginAbstractions/versioning) says what counts as breaking, and [Add a plugin](https://docs.schuly.dev/SchulyPlugins/adding-a-plugin) goes from here to a loaded plugin.

## License

[MIT](LICENSE)
