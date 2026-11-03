# Development

Use this page to build, test, and pack the abstractions package locally.

## Prerequisites

- **.NET SDK 10.0.x.** The project targets `net10.0`.
- **The ASP.NET Core shared framework.** The project has a `Microsoft.AspNetCore.App` framework reference.
  The .NET SDK includes this framework.

## Project layout

```
application.properties                          # holds <version>
global.json                                     # test runner: Microsoft.Testing.Platform
Schuly.Plugin.Abstractions.slnx
src/
├── Directory.Build.props                       # reads <version> from application.properties
├── Schuly.Plugin.Abstractions/
│   ├── ILinkedAccounts.cs                      # + LinkedAccountRecord, LinkedAccount, LinkedProviders, LinkedSecrets
│   ├── IPluginBackgroundTask.cs
│   ├── IPluginEventHandler.cs
│   ├── IPluginFileStore.cs                     # + StoredFile record
│   ├── IPluginLogin.cs                         # + PluginLoginResult record
│   ├── IPluginUserContext.cs
│   ├── IPluginVault.cs
│   ├── ISchoolData.cs                          # + SyncResult record
│   ├── ISchulyPlugin.cs                        # + PluginServiceContext record
│   ├── Linking/
│   │   ├── ILinkProvider.cs                    # + descriptor, step, field, and context records
│   │   ├── LinkDescriptorValidator.cs
│   │   ├── LinkStepValidator.cs
│   │   └── LinkValidationResult.cs
│   ├── NUGET_README.md                         # packed as the NuGet README
│   ├── PluginSchedule.cs                       # PluginSchedule record
│   ├── PluginUserPurgeEvent.cs
│   ├── SchoolRecords.cs                        # records ISchoolData accepts
│   ├── SchoolSystemDescriptor.cs               # + login field, login method, and browser login records
│   └── Schuly.Plugin.Abstractions.csproj
└── Schuly.Plugin.Abstractions.Tests/
    ├── LinkingTests.cs
    ├── PluginScheduleTests.cs
    └── Schuly.Plugin.Abstractions.Tests.csproj
```

The package has no dependencies beyond the `Microsoft.AspNetCore.App` framework reference. It ships no
backend assemblies, so plugins reach school data only through `ISchoolData`.

## Build

The solution file in the repo root (`Schuly.Plugin.Abstractions.slnx`) builds the abstractions project and
its test project together:

```sh
dotnet build
```

To build only the abstractions project, as the publish workflow's pack step does:

```sh
dotnet build src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj
```

## Test

The TUnit test project `Schuly.Plugin.Abstractions.Tests` covers `PluginSchedule`
(`PluginScheduleTests.cs`) and linking validation, `LinkStepValidator` and `LinkDescriptorValidator`
(`LinkingTests.cs`):

```sh
dotnet test
```

The interfaces have no runtime behavior of their own. Downstream consumers (SchulyServer and the plugins)
verify them.

## Pack dry-run

To produce the `.nupkg` locally without publishing, run this from `src/Schuly.Plugin.Abstractions/`:

```sh
dotnet pack Schuly.Plugin.Abstractions.csproj --configuration Release -o ./out
```

From the repo root, use the full path, as the publish workflow does:

```sh
dotnet pack src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj --configuration Release -o ./out
```

Don't pass `-p:Version=`. The version comes from `<version>` in `application.properties`.
`src/Directory.Build.props` reads it at project-load time with a regex and assigns it to `$(Version)`.

`Directory.Build.props` also pins the assembly version to `MAJOR.MINOR.0.0`, so any `MAJOR.MINOR.x` plugin
binds to one stable assembly version. `FileVersion` and `InformationalVersion` keep the full version for
diagnostics.

See [publishing](publishing.md) for how packing fits into the release flow.
