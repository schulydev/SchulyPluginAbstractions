# Schuly.Plugin.Abstractions — Documentation

`Schuly.Plugin.Abstractions` is the **stable plugin contract** for the
[Schuly backend](https://github.com/schulydev/SchulyBackend), published to NuGet.org
as [`Schuly.Plugin.Abstractions`](https://www.nuget.org/packages/Schuly.Plugin.Abstractions).
Plugin authors reference the package and implement its interfaces; the backend's plugin
host discovers and runs the implementation. This repo is intentionally **small and
stable** — the contract changes rarely and is governed by strict semver, so plugins
built against one host don't break on another.

## Index

- [Contract reference](contract.md) — every interface and record, with real signatures
  and where each member fits in the plugin lifecycle.
- [Setup](setup/) — local development and publishing:
  - [Development](setup/development.md) — prerequisites, build, local pack dry-run.
  - [Publishing](setup/publishing.md) — the NuGet release flow and package metadata.
- [Versioning](versioning.md) — strict semver rules and the labels that drive releases.
- [Contributing](contributing.md) — the enforced issue → branch → PR workflow and the
  dependency rules this repo lives by.
