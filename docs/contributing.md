# Contributing

These rules apply to this repo. For branches, commits, pull requests, and labels, follow the
[shared workflow](/contribute/).

This repo is the published contract, so keep it small and stable. [Versioning](versioning.md) governs
almost every change. Read it first.

## Dependency rules

The abstractions assembly may reference only:

- the BCL
- the `Microsoft.AspNetCore.App` framework reference

Don't add references to `Schuly.Application`, `Schuly.Domain`, or `Schuly.Infrastructure`. Those types
live in [SchulyServer](https://github.com/schulydev/SchulyServer) and aren't published.

Plugins never see the backend's entities or `DbContext`. They write school data through `ISchoolData`
with the plain records in `SchoolRecords.cs`. Keep those records free of backend types, and add fields
only in a backward-compatible way.

## Pick a version bump

The PR label you choose drives the release version, so label deliberately. In addition to the shared
labels, this repo uses `breaking-change`. See the table in [versioning](versioning.md).
