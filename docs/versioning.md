# Versioning

This package is a published contract, so versioning is **strict semver**. The whole point of
the repo is stability: plugins reference the package, and the host provides the assembly at
runtime, so a careless change breaks every plugin in the field.

## Rules

| Change | Bump | PR label |
|---|---|---|
| Change a method signature, add a method to an existing interface, or rename a member | **MAJOR** | `breaking-change` |
| Add a new optional interface, or a default-implemented method | **MINOR** | `feature` |
| Docs / metadata / packaging tweaks | **PATCH** | *(default - no label needed)* |

## How the version is resolved

- `application.properties` holds the current `<version>`; it is the single source of truth and
  is read into `$(Version)` by `src/Directory.Build.props` at build/pack time.
- **release-drafter** (`.github/release-drafter.yml`) resolves the *next* version from the
  labels on merged PRs: `breaking-change` → major, `feature` → minor, everything else →
  patch. It also drafts the changelog.
- Cutting the release then runs the publish flow, which syncs `application.properties` to the
  tag before packing. See [publishing](setup/publishing.md).

## Assembly version stability

`Directory.Build.props` pins the **assembly** version to `MAJOR.MINOR.0.0` (while
`FileVersion`/`InformationalVersion` carry the full version). A plugin built against any
`MAJOR.MINOR.x` binds to a single `MAJOR.MINOR.0.0` assembly, so patch bumps don't force a
rebuild of every plugin and don't cause type-load failures across patch versions. Only a
**MINOR or MAJOR** bump changes the binding assembly version - another reason to follow the
table above exactly.

## Before 1.0

While the version is `0.x`, a breaking change bumps **MINOR** (`0.3` → `0.4`) instead of
MAJOR, so cut the release with an explicit `gh release create v0.<next>.0` rather than the
version release-drafter suggests for a `breaking-change` label.

## Compatibility at load time

The backend compares the `Schuly.Plugin.Abstractions` assembly version a plugin was built
against with its own and refuses an incompatible plugin with a clear log message instead of
failing later with a type-load error. Compatible means the same MAJOR for `1.x` and later, and
the same MAJOR.MINOR for `0.x`.
