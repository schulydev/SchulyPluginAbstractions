# Schuly.Plugin.Abstractions

Use these pages to look up the plugin contract or to change it.

`Schuly.Plugin.Abstractions` is the plugin contract for the
[Schuly server](https://github.com/schulydev/SchulyServer), published to NuGet.org as
[`Schuly.Plugin.Abstractions`](https://www.nuget.org/packages/Schuly.Plugin.Abstractions). You reference
the package and implement its interfaces. The server's plugin host finds and runs your implementation.

This repo stays small and stable. The contract changes rarely and follows strict semver, so plugins built
against one host don't break on another.

| Page | Use it to |
|---|---|
| [Contract reference](contract.md) | Look up each interface and record, with real signatures and where each member fits in the plugin lifecycle |
| [Development](setup/development.md) | Set up, build, test, and dry-run a pack |
| [Publishing](setup/publishing.md) | See the NuGet release flow and package metadata |
| [Versioning](versioning.md) | Pick a version bump, and see the labels that drive releases |
| [Contributing](contributing.md) | Follow this repo's dependency rules |
