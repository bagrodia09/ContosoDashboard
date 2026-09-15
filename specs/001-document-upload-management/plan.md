# Implementation Plan: Document Upload and Management

**Branch**: `001-document-upload-management` | **Date**: 2026-09-15 | **Spec**: [spec.md](./spec.md)

## Summary

Extend the existing net8.0 ASP.NET Core Blazor Server training application with the P1 offline personal-document upload and My Documents flow, while defining seams for the later stakeholder stories. Add an integer-keyed EF Core `Document` model, an additive LocalDB schema upgrade, a local `IFileStorageService` implementation rooted outside `wwwroot`, per-file validation, owner-only service authorization, and an authorized file-serving endpoint. Use an atomic batch workflow and cleanup compensation so invalid or failed uploads leave neither orphaned files nor metadata. Preserve the existing mock authentication, pages, services, seed data, and offline-only deployment.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: ASP.NET Core Blazor Server, EF Core 8 SQL Server provider, SQL Server LocalDB, existing cookie mock authentication  
**Storage**: Existing SQL Server LocalDB for metadata; local filesystem under configured `AppData/uploads` outside `wwwroot` for content  
**Testing**: Existing repository test conventions if present; add offline unit/integration/acceptance coverage for the feature without external services  
**Target Platform**: Local Windows training environment running the existing ASP.NET Core web application  
**Project Type**: Single web application  
**Performance Goals**: Preserve stakeholder candidate limits as deferred local benchmarks: 25 MB upload up to 30 seconds, list/search up to 2 seconds for 500 documents, preview up to 3 seconds  
**Constraints**: Must work offline; no Azure or external scanner; files outside `wwwroot`; service-level authorization; no destructive database recreation; P1 owner-only personal documents; atomic multi-file uploads  
**Scale/Scope**: P1 personal upload/list plus contracts and model seams for later search, project, lifecycle, sharing, integration, and reporting stories

## Constitution Check

| Principle | Plan evidence | Status |
|---|---|---|
| Training-only/offline | LocalDB, local filesystem, mock auth, deferred real malware scanning, no Azure dependencies | PASS |
| Existing architecture/auth | Changes remain in the existing project, Models/Data/Services/Pages, and preserve cookie claims/roles | PASS |
| Storage abstraction | `IFileStorageService` and `LocalFileStorageService`; root outside `wwwroot`; GUID paths; relative metadata paths | PASS |
| Authorization/validation | `DocumentService` checks owner and current claims; file endpoint calls the service before opening content; validator runs before storage | PASS |
| Testable acceptance | Offline tests cover valid/invalid uploads, atomic cleanup, authorization denial, schema upgrade, and My Documents | PASS |
| Data preservation | Replace unconditional `EnsureCreated` behavior with preflighted additive schema initialization; never drop/recreate existing databases | PASS |

No violations require Complexity Tracking.

## Design

### Application changes

1. Add `Models/Document.cs` with `int DocumentId`, text category, `nvarchar(255)` content type, ownership, safe relative storage path, optional future project/task keys, and validation-friendly metadata.
2. Add `Document` to `ApplicationDbContext`, configure lengths, indexes, required relationships, and category/content-type constraints using the existing EF conventions.
3. Add `Services/IFileStorageService.cs` and `Services/LocalFileStorageService.cs`. Resolve a configured root outside `wwwroot`; reject rooted paths, traversal, and mismatched resolved roots. Generate GUID-based names and return portable relative paths. Save to a temporary path and atomically move when practical.
4. Add document validation and service contracts/models. `DocumentService` obtains the authenticated user from the existing claims context, enforces owner-only P1 access, validates all batch entries before saving, saves files before metadata, and deletes saved files if a later save fails.
5. Register the services in `Program.cs` with dependency injection and a configured storage root. Preserve existing authentication, authorization policies, middleware, pages, and seed behavior.
6. Add an authenticated Blazor page for upload/My Documents. Keep UI checks supplemental; all decisions remain in `DocumentService`.
7. Add an authenticated file-serving endpoint (controller/minimal endpoint consistent with the application) that parses integer IDs, asks `DocumentService` for an authorized stream, and returns the stored content type. Never construct a path from a request filename or expose the storage directory as static files.

### Database schema and `EnsureCreated` transition

The current startup call to `EnsureCreated` is suitable only for a brand-new empty training database and does not update an already-created schema. Implement a dedicated startup schema initializer:

- Detect whether the database is empty/new versus an existing `EnsureCreated` database.
- For an existing database, verify expected existing tables/columns and take the documented LocalDB backup before upgrade.
- Execute an idempotent, additive transaction that creates `Documents`, indexes, and foreign keys if absent, without dropping or altering existing user tables/data.
- Record a schema version (or equivalent migration marker) so repeat startup is safe.
- On mismatch, backup failure, permission failure, or SQL error, log the precise error and fail initialization; do not call `EnsureDeleted`, do not silently recreate, and do not claim the feature is available.
- For future schema evolution, add EF migrations after this baseline. Do not pretend an EF migration history exists for databases originally created only with `EnsureCreated`; document the one-time baseline/upgrade procedure.

### Upload failure and cleanup

- Validate the complete batch before opening storage writes.
- Generate one unique path per file.
- Save all files through `IFileStorageService`.
- Save metadata in one EF transaction.
- If any file save fails, delete files already saved in the batch.
- If metadata transaction fails, delete every newly saved file and leave no database rows.
- Surface the original failure and cleanup failures through logging; never convert a failed upload into a success response.

## Project Structure

```text
ContosoDashboard/
├── Models/Document.cs
├── Data/ApplicationDbContext.cs
├── Services/
│   ├── IFileStorageService.cs
│   ├── LocalFileStorageService.cs
│   ├── IDocumentService.cs
│   ├── DocumentService.cs
│   └── DocumentValidationService.cs
├── Pages/
│   └── Documents.razor
├── Endpoints/DocumentEndpoints.cs
├── Program.cs
└── appsettings.json
tests/ (or the repository's established test project location)
├── unit/
├── integration/
└── acceptance/
```

**Structure Decision**: Extend the existing single project and its current layer separation. Add tests in the repository's established test project if one exists; otherwise create only the smallest offline test project required by the implementation workflow.

## Testing Plan

- Unit: extension/content-type/size/metadata validation; category and MIME length; GUID/relative path and traversal defenses.
- Unit: owner-only authorization for list/upload/read/file serving; denied IDOR cases.
- Integration: storage-before-metadata ordering; failed file save cleanup; failed metadata transaction cleanup; atomic mixed-validity batch.
- Integration: additive schema upgrade against a copy of an existing `EnsureCreated` LocalDB database, proving existing rows remain.
- Acceptance: offline mock-user upload and My Documents listing; invalid upload messages; second mock user cannot list/download the first user's personal document.
- Regression: existing authentication, dashboard, project, task, notification, and seed flows remain unchanged.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Dedicated additive schema initializer during the `EnsureCreated` transition | Existing databases have no reliable migration history and must retain data | Continuing `EnsureCreated` does not add tables; dropping/recreating loses training data |
