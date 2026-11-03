# Publishing

Publish a new package version by publishing a GitHub release. You never run `dotnet nuget push` by hand.

## Cut a release

Cut releases by hand with `gh release create vX.Y.Z`, using the version release-drafter resolves from the
merged PR labels. See [versioning](../versioning.md).

## What the workflow does

[`nuget-publish.yml`](https://github.com/schulydev/SchulyPluginAbstractions/blob/main/.github/workflows/nuget-publish.yml)
runs on `release: published`. It has two jobs:

1. **`sync-version`** checks out `main`, strips the leading `v` from the release tag, and compares it to
   `<version>` in `application.properties`. If they differ, it:
   1. Edits the file and pushes a `release-sync/<version>` branch.
   2. Opens a PR labeled `CI/CD` and `skip-changelog`.
   3. Auto-merges it with `gh pr merge --admin --squash --delete-branch`.

   This keeps `application.properties`, the single source of truth for the package version, in step with
   the tag.
2. **`publish`** (`needs: sync-version`) checks out `main`, sets up .NET `10.0.x`, then runs:

   ```sh
   dotnet pack src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj \
     --configuration Release -o ./out
   dotnet nuget push ./out/*.nupkg \
     --api-key ${{ secrets.NUGET_API_KEY }} \
     --source https://api.nuget.org/v3/index.json \
     --skip-duplicate
   ```

   `--skip-duplicate` makes a re-run safe if the version already exists on NuGet.org.

## Package metadata

Edit metadata in the `PropertyGroup` of
[`Schuly.Plugin.Abstractions.csproj`](https://github.com/schulydev/SchulyPluginAbstractions/blob/main/src/Schuly.Plugin.Abstractions/Schuly.Plugin.Abstractions.csproj):
`PackageId`, `Description`, `Authors`, `PackageProjectUrl`, `RepositoryUrl`, `RepositoryType`,
`PackageLicenseExpression` (`MIT`), and `PackageTags`.

### READMEs

The repo has two READMEs:

| File | Audience | Notes |
|---|---|---|
| `README.md` (repo root) | GitHub | Centered logo, badges, raw HTML. |
| `src/Schuly.Plugin.Abstractions/NUGET_README.md` | NuGet.org | Plain Markdown with absolute image URLs. NuGet.org doesn't render HTML, and relative image paths don't resolve. |

The csproj sets `<PackageReadmeFile>README.md</PackageReadmeFile>` and packs `NUGET_README.md` as
`README.md` inside the package:

```xml
<None Include="NUGET_README.md" Pack="true" PackagePath="README.md" />
```

So the NuGet page shows `NUGET_README.md`, and the GitHub repo page shows the root `README.md`.

### Icon

The package icon is `assets/app_icon.png`, packed as `icon.png` and set with
`<PackageIcon>icon.png</PackageIcon>`:

```xml
<None Include="../../assets/app_icon.png" Pack="true" PackagePath="icon.png" />
```
