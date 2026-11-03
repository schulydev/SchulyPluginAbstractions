# Versioning

Use this page to pick the right version bump for a contract change.

This package follows strict semver. Plugins reference the package and the host provides the assembly at
runtime, so a careless change breaks every plugin in the field.

## Rules

| Change | Bump | PR label |
|---|---|---|
| Change a method signature, add a method to an existing interface, or rename a member | **MAJOR** | `breaking-change` |
| Add a new optional interface or a default-implemented method | **MINOR** | `feature` |
| Docs, metadata, or packaging tweaks | **PATCH** | None needed |

## How the version is resolved

- `application.properties` holds the current `<version>`. It's the single source of truth.
  `src/Directory.Build.props` reads it into `$(Version)` at build and pack time.
- **release-drafter** (`.github/release-drafter.yml`) resolves the next version from the labels on merged
  PRs and drafts the changelog:
  - `breaking` or `breaking-change`: major
  - `feature`, `enhancement`, or `minor`: minor
  - anything else: patch
- Cutting the release runs the publish flow, which syncs `application.properties` to the tag before
  packing. See [publishing](setup/publishing.md).

## Assembly version stability

`Directory.Build.props` pins the assembly version to `MAJOR.MINOR.0.0`. `FileVersion` and
`InformationalVersion` carry the full version.

A plugin built against any `MAJOR.MINOR.x` binds to one `MAJOR.MINOR.0.0` assembly. Patch bumps don't force
a rebuild of every plugin and don't cause type-load failures across patch versions. Only a MINOR or MAJOR
bump changes the binding assembly version, which is another reason to follow the table above exactly.

## Before 1.0

While the version is `0.x`, a breaking change bumps MINOR (`0.3` to `0.4`) instead of MAJOR. Cut the
release with an explicit `gh release create v0.<next>.0`, not the version release-drafter suggests for a
`breaking-change` label.

## Compatibility at load time

The backend compares the `Schuly.Plugin.Abstractions` assembly version a plugin was built against with its
own. It refuses an incompatible plugin with a clear log message, so the plugin doesn't fail later with a
type-load error.

| Host contract | A plugin is compatible when |
|---|---|
| `1.x` and later | It has the same MAJOR, and its MINOR is not newer than the host's |
| `0.x` | It has the same MAJOR.MINOR |

So during `0.x`, plugins need a rebuild after every MINOR release.
