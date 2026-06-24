# Development

Local environment for building and packing the abstractions package.

## Prerequisites

- **.NET SDK 10.0.x** — the project targets `net10.0`
  (`<TargetFramework>net10.0</TargetFramework>` in the csproj).
- The project uses a `Microsoft.AspNetCore.App` framework reference, so the ASP.NET Core
  shared framework must be available (it ships with the .NET SDK).

## Project layout

```
src/
├── Directory.Build.props                      # reads <version> from application.properties
└── Schuly.Plugin.Abstractions/
    ├── ISchulyPlugin.cs                        # + PluginServiceContext record
    ├── IPluginBackgroundTask.cs
    ├── IPluginEventHandler.cs
    ├── IPluginUserContext.cs
    ├── IPluginLogin.cs                         # + PluginLoginResult record
    ├── NUGET_README.md                         # packed as the NuGet README
    ├── Schuly.Plugin.Abstractions.csproj
    └── libs/                                    # shipped backend DLLs
        ├── Schuly.Domain.dll
        └── Schuly.Infrastructure.dll
```

The csproj references `libs/Schuly.Domain.dll` and `libs/Schuly.Infrastructure.dll`
(synced from SchulyBackend) so the project compiles against them, and packs them under
`lib/net10.0/` so plugins get typed DB access. The transitive deps those DLLs need
(`Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`) are declared as
`PackageReference`s in the csproj.

## Build

```sh
dotnet build src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj
```

There is no test project in this repo; the contract is verified by downstream consumers
(SchulyBackend and the plugins).

## Pack dry-run (local)

To produce the `.nupkg` locally without publishing:

```sh
dotnet pack Schuly.Plugin.Abstractions.csproj --configuration Release -o ./out
```

(From the repo root, point at the full path
`src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj` — that's exactly what the
publish workflow runs.)

You do **not** pass `-p:Version=`. The version comes from `application.properties`
(`<version>…</version>`), which `src/Directory.Build.props` reads at project-load time via a
regex and assigns to `$(Version)`. `Directory.Build.props` also pins the **assembly** version
to `MAJOR.MINOR.0.0` so any `MAJOR.MINOR.x` plugin binds to a single stable assembly version,
while `FileVersion` / `InformationalVersion` keep the full version for diagnostics.

See [publishing](publishing.md) for how packing fits into the release flow.
