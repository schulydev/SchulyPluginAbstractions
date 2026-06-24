# Publishing

Publishing is **automatic on a GitHub release** - you never run `dotnet nuget push` by hand.

## Flow (`.github/workflows/nuget-publish.yml`)

Triggered on `release: published`. Two jobs:

1. **`sync-version`** - checks out `main`, strips the leading `v` from the release tag, and
   compares it to the `<version>` in `application.properties`. If they differ it edits the
   file, pushes a `release-sync/<version>` branch, opens a `CI/CD`-labeled PR, and
   **auto-merges it** (`gh pr merge --admin --squash --delete-branch`). This keeps
   `application.properties` (the single source of truth for the package version) in lockstep
   with the tag.
2. **`publish`** (`needs: sync-version`) - checks out `main`, sets up .NET `10.0.x`, then:
   ```sh
   dotnet pack src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj \
     --configuration Release -o ./out
   dotnet nuget push ./out/*.nupkg \
     --api-key ${{ secrets.NUGET_API_KEY }} \
     --source https://api.nuget.org/v3/index.json \
     --skip-duplicate
   ```
   `--skip-duplicate` makes a re-run safe if the version already exists on NuGet.org.

### Where releases come from

Releases are cut by `.github/workflows/auto-release-on-main.yml`, which fires **only when the
shipped backend DLLs change** (`paths: src/Schuly.Plugin.Abstractions/libs/**`). It computes
the next patch version from `application.properties` and runs `gh release create`. Releases
for contract changes are otherwise driven by release-drafter - see [versioning](../versioning.md).

## Package metadata

Edit metadata in the csproj `PropertyGroup`
(`src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj`): `PackageId`,
`Description`, `Authors`, `PackageProjectUrl`, `PackageLicenseExpression` (`MIT`),
`PackageTags`, etc.

### Two READMEs

| File | Audience | Notes |
|---|---|---|
| `README.md` (repo root) | GitHub | Centered logo, badges, raw HTML. |
| `src/Schuly.Plugin.Abstractions/NUGET_README.md` | NuGet.org | Plain markdown with **absolute** image URLs - NuGet.org doesn't render HTML, and relative image paths won't resolve. |

The csproj sets `<PackageReadmeFile>README.md</PackageReadmeFile>` and packs
`NUGET_README.md` **as** `README.md` inside the package:

```xml
<None Include="NUGET_README.md" Pack="true" PackagePath="README.md" />
```

So the NuGet page shows `NUGET_README.md`, while the GitHub repo page shows the root
`README.md`.

### Icon

The package icon is `assets/app_icon.png`, packed as `icon.png`:

```xml
<None Include="../../assets/app_icon.png" Pack="true" PackagePath="icon.png" />
```

and wired up with `<PackageIcon>icon.png</PackageIcon>`.
