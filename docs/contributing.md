# Contributing

This repo is the **stable published contract**. Keep it small, keep it stable. Almost every
change is governed by [versioning](versioning.md) - read that first.

## Dependency rules

The abstractions assembly must reference **only**:

- the BCL, and
- the `Microsoft.AspNetCore.App` framework reference.

Do **not** add references to `Schuly.Application` (those types live in
[SchulyBackend](https://github.com/schulydev/SchulyBackend) and are not published).

> Plugins never see the backend's entities or DbContext. They write school data through
> `ISchoolData` with the plain records in `SchoolRecords.cs`, so keep those records free of
> backend types and add fields only in a backward-compatible way.

## Workflow (enforced)

1. **Open a labeled issue** describing the change. Use the right label so release-drafter
   resolves the next version correctly (`breaking-change`, `feature`, `documentation`,
   `CI/CD`, `dependencies`, `bug`, `refactor`).
2. **Branch** from `main`: `feature/<issue#>_PascalCase` or `fix/<issue#>_PascalCase`.
   Never commit to `main`.
3. **Open a PR** with `Closes #<issue>`. The PR body is **Summary + `Closes #<issue>` only** -
   no test plans.
4. **Squash-merge** and delete the branch.

Commit subjects are short and imperative.

## No AI attribution

Never add AI / assistant attribution anywhere - not in commit messages, PR descriptions, or
issue bodies. No `Co-Authored-By` trailers, no "generated with" lines. Ever.

## Picking a version bump

See the table in [versioning](versioning.md). The PR label you choose is what drives the
release version, so label deliberately.
