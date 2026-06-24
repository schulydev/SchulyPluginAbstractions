# Notes for Claude (and humans) - SchulyPluginAbstractions

The stable plugin contract for the Schuly backend, published as `Schuly.Plugin.Abstractions` on NuGet.org.

## Workflow rules (enforced)

- Never work on `main`. Create an issue (labeled) → branch `feature/<issue#>_PascalCase`
  or `fix/<issue#>_PascalCase` → PR (labeled) with `Closes #<issue>` → squash-merge +
  delete branch.
- Use **bun** as the package manager / task runner - never npm, npx, or node directly.
- Use CLI tooling whenever one exists (`gh issue create`, `gh pr create`, generators, etc.).
- No AI / Claude attribution in commits or PRs. Ever.
- No test plans in PRs. PR body is **Summary** + `Closes #<issue>` only.
- Commit subject: short imperative.
- PR labels: `bug`, `enhancement`, `feature`, `refactor`, `CI/CD`, `dependencies`, `documentation`.

## Contents

4 interfaces - that's it. Keep this repo **small and stable**.

| Interface | Purpose |
|---|---|
| `ISchulyPlugin` | Plugin entry point: `ConfigureServices`, `ConfigureEndpoints`, `MigrateAsync` |
| `IPluginBackgroundTask` | Recurring background work (`Name`, `Interval`, `ExecuteAsync`) |
| `IPluginEventHandler<TCommand>` | React to backend commands |
| `IPluginUserContext` | Read current user / school-user from inside a plugin |

Plus the `PluginServiceContext` record (`ConnectionString`, `IConfiguration`) passed to `ConfigureServices`.

## What this repo **must not** depend on

- `Schuly.Application` / `Schuly.Domain` / `Schuly.Infrastructure` - those are in [SchulyBackend](https://github.com/schulydev/SchulyBackend) and not published. Only BCL + `Microsoft.AspNetCore.App` framework references are allowed.

## Versioning rules

**Semver, strictly.**

- Changing a method signature, adding a method to an existing interface, renaming a member → **major bump**. Label PR `breaking-change`.
- New optional interface or default-implemented method → **minor bump**. Label `feature`.
- Doc / metadata / packaging tweaks → **patch**. Default.

Release-drafter resolves the next version from labels.

## Pack + publish

Automatic on release. To dry-run locally:

```sh
dotnet pack Schuly.Plugin.Abstractions.csproj --configuration Release -o ./out
```

The version comes from `application.properties` via `Directory.Build.props` - no need to pass `-p:Version=`.

Publish workflow (`nuget-publish.yml`) on `release: published`:
1. `sync-version` - bumps `application.properties` from the release tag, auto-merges
2. `publish` - packs and pushes to `https://api.nuget.org/v3/index.json` with `--skip-duplicate`

## Package metadata

Edit in the csproj `PropertyGroup`. Note two READMEs:
- `README.md` - GitHub-facing (centered logo, badges, HTML)
- `NUGET_README.md` - packed as the NuGet README (plain markdown, absolute image URLs - NuGet.org doesn't render HTML)

Icon is `assets/app_icon.png`, packed as `icon.png` via `<PackageIcon>icon.png</PackageIcon>`.
