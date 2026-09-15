# Research: Document Upload and Management

## Decisions

### Existing application architecture

- **Decision:** Add the feature to the existing single ASP.NET Core 8 Blazor Server project using its `Models`, `Data`, `Services`, `Pages`, and endpoint conventions.
- **Rationale:** The constitution requires preserving the current architecture, mock cookie authentication, claims, roles, and offline behavior.
- **Alternatives considered:** A separate API or cloud-backed service was rejected because it would add deployment and network dependencies and change the training architecture.

### Offline file storage boundary

- **Decision:** Store files below an application-data directory outside `wwwroot`, addressed by a generated relative path such as `{userId}/personal/{guid}.{extension}`. Business services depend on `IFileStorageService`; `LocalFileStorageService` resolves and validates paths under the configured root.
- **Rationale:** This prevents static-file exposure and path traversal while leaving a replaceable seam for future cloud storage.
- **Alternatives considered:** Storing in `wwwroot` was rejected because it bypasses authorization. Storing bytes in SQL was rejected because the stakeholder explicitly specifies filesystem storage and a migration abstraction.

### Validation and scanning

- **Decision:** Validate per-file size (25 MB maximum), whitelisted extension/type, MIME value, title, category, tags, and P1 ownership before persistence. Real malware scanning is deferred because the training application must work offline.
- **Rationale:** This is the clarified stakeholder decision and constitution-compliant training limitation.
- **Alternatives considered:** An external scanner or Azure service was rejected for the offline training implementation. Silently accepting unvalidated files was rejected.

### Existing `EnsureCreated` database

- **Decision:** Replace unconditional startup `EnsureCreated` with an explicit, additive schema upgrade path. New databases are created from the current model; existing databases are preflighted and upgraded without dropping tables or data. A backup/preflight failure stops startup with a logged actionable error rather than silently recreating the database.
- **Rationale:** `EnsureCreated` does not evolve an existing schema and can leave an existing database incompatible with a changed model. The upgrade must preserve seeded and user data.
- **Alternatives considered:** Continuing to call `EnsureCreated` was rejected because it cannot add the document schema safely. `EnsureDeleted`/recreate was rejected because it loses existing data. Cloud migration tooling was rejected as out of scope.

### Testing

- **Decision:** Add offline unit and integration tests for validators, storage path safety, service authorization, atomic upload cleanup, schema initialization, and file-serving endpoint authorization. Add an acceptance test for the P1 upload-and-view flow.
- **Rationale:** These tests directly exercise the constitution's required acceptance criteria and both allowed and denied paths.
- **Alternatives considered:** UI-only testing was rejected because authorization and cleanup must be enforced below the UI.
