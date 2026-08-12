# <p align="center">SchulyPluginAbstractions</p>
<p align="center">
  <img src="./assets/app_icon.png" width="160" alt="Schuly Logo">
</p>
<p align="center">
  <strong>Plugin contract for the Schuly backend - distributed as a NuGet package</strong>
</p>
<p align="center">
  <a href="https://github.com/schulydev/SchulyPluginAbstractions/stargazers"><img src="https://img.shields.io/github/stars/schulydev/SchulyPluginAbstractions?style=flat&color=3da8ff" alt="GitHub stars"/></a>
  <a href="https://www.nuget.org/packages/Schuly.Plugin.Abstractions"><img src="https://img.shields.io/nuget/v/Schuly.Plugin.Abstractions?color=3da8ff&label=NuGet" alt="NuGet"/></a>
  <a href="https://dotnet.microsoft.com/"><img src="https://img.shields.io/badge/.NET-10.0-3da8ff" alt=".NET"/></a>
  <a href="https://docs.schuly.dev/SchulyPluginAbstractions/"><img src="https://img.shields.io/badge/docs-docs.schuly.dev-3da8ff" alt="Documentation"/></a>
</p>

The stable contract that plugins implement and that [SchulyBackend](https://github.com/schulydev/SchulyBackend) consumes. Versioned and published as a NuGet package so plugin authors don't need to vendor backend source.

## What's in this repo

- `ISchulyPlugin` - entry point: register services, endpoints, migrations
- `IPluginLogin` - account-connect contract, and the source of a plugin's school-system catalog entry
- `IPluginBackgroundTask` - recurring background work
- `IPluginEventHandler<TCommand>` - react to application commands
- `IPluginUserContext` - read user claims / roles from inside a plugin

## Consume in a plugin

```xml
<PackageReference Include="Schuly.Plugin.Abstractions" Version="*" />
```

Implement `ISchulyPlugin` and the backend's plugin host discovers it automatically.

## Documentation

Full documentation lives at **[docs.schuly.dev/SchulyPluginAbstractions](https://docs.schuly.dev/SchulyPluginAbstractions/)**.

| Guide | What it covers |
|---|---|
| [The contract](https://docs.schuly.dev/SchulyPluginAbstractions/contract) | Every interface, member by member, with worked examples. |
| [Development setup](https://docs.schuly.dev/SchulyPluginAbstractions/setup/development) | Build and test the package locally. |
| [Publishing](https://docs.schuly.dev/SchulyPluginAbstractions/setup/publishing) | How a release reaches NuGet.org. |
| [Versioning](https://docs.schuly.dev/SchulyPluginAbstractions/versioning) | Semver policy and what counts as breaking. |
| [Contributing](https://docs.schuly.dev/SchulyPluginAbstractions/contributing) | Workflow, branch and PR conventions. |

Writing a plugin against this contract? See [Adding a plugin](https://docs.schuly.dev/SchulyPlugins/adding-a-plugin).

## The Schuly ecosystem

| Repo | Purpose |
|---|---|
| [**Schuly**](https://github.com/schulydev/Schuly) | Flutter mobile app |
| [**SchulyBackend**](https://github.com/schulydev/SchulyBackend) | ASP.NET Core API backend |
| [**SchulyKeycloak**](https://github.com/schulydev/SchulyKeycloak) | Keycloak image + the `schuly` realm |
| [**SchulyPluginAbstractions**](https://github.com/schulydev/SchulyPluginAbstractions) | Plugin contract (NuGet) *(this repo)* |
| [**SchulyPlugins**](https://github.com/schulydev/SchulyPlugins) | Official plugins monorepo |
| [**SchulyWebsite**](https://github.com/schulydev/SchulyWebsite) | Landing site ([schuly.dev](https://schuly.dev)) |
| [**SchulyDocs**](https://github.com/schulydev/SchulyDocs) | Documentation site ([docs.schuly.dev](https://docs.schuly.dev)) |
