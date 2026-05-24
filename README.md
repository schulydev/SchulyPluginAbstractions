# <p align="center">SchulyPluginAbstractions</p>
<p align="center">
  <img src="./assets/app_icon.png" width="160" alt="Schuly Logo">
</p>
<p align="center">
  <strong>Plugin contract for the Schuly backend — distributed as a NuGet package</strong>
</p>
<p align="center">
  <a href="https://github.com/schulydev/SchulyPluginAbstractions/stargazers"><img src="https://img.shields.io/github/stars/schulydev/SchulyPluginAbstractions?style=flat&color=3da8ff" alt="GitHub stars"/></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-9.0-3da8ff" alt=".NET"/></a>
  <a href="https://schuly.dev"><img src="https://img.shields.io/badge/site-schuly.dev-3da8ff" alt="Website"/></a>
</p>

The stable contract that plugins implement and that [SchulyBackend](https://github.com/schulydev/SchulyBackend) consumes. Versioned and published as a NuGet package so plugin authors don't need to vendor backend source.

## What's in this repo

- `ISchulyPlugin` — entry point: register services, endpoints, migrations
- `IPluginBackgroundTask` — recurring background work
- `IPluginEventHandler<TCommand>` — react to application commands
- `IPluginUserContext` — read user claims / roles from inside a plugin

## The Schuly ecosystem

| Repo | Purpose |
|---|---|
| [**Schuly**](https://github.com/schulydev/Schuly) | Flutter mobile app |
| [**SchulyBackend**](https://github.com/schulydev/SchulyBackend) | ASP.NET Core API backend |
| [**SchulyPluginAbstractions**](https://github.com/schulydev/SchulyPluginAbstractions) | Plugin contract (NuGet) *(this repo)* |
| [**SchulyPlugins**](https://github.com/schulydev/SchulyPlugins) | Official plugins monorepo |
| [**SchulyWebsite**](https://github.com/schulydev/SchulyWebsite) | Landing site ([schuly.dev](https://schuly.dev)) |

## Consume in a plugin

```xml
<PackageReference Include="Schuly.Plugin.Abstractions" Version="*" />
```

Implement `ISchulyPlugin` and the backend's `PluginBackgroundTaskHost` discovers it automatically.

## Versioning

Semver. Any interface change is marked with the `breaking-change` label and bumps the major version.
