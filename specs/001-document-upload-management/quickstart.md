# Quickstart: Document Upload and Management

## Training prerequisites

1. Install the .NET 8 SDK and SQL Server LocalDB.
2. Start the application with `dotnet run` from the repository root.
3. Use the existing mock login; no internet, Azure subscription, or external scanner is required.

## Local storage

- Configure a writable application-data root outside `wwwroot` (the default should be an `AppData/uploads` directory under the application content root).
- Do not commit uploaded files or place them under `wwwroot`.
- The service creates GUID-based internal filenames and stores only portable relative paths in the database.

## Database upgrade safety

The application must not drop or recreate an existing database during startup.

1. Back up the LocalDB database before the first schema upgrade.
2. On a new database, create the current schema and seed the existing training data.
3. On an existing `EnsureCreated` database, run the additive document-schema upgrade after a preflight verifies the expected existing tables and columns.
4. Create the `Documents` table, indexes, and foreign keys in a transaction; preserve all existing rows.
5. Record the schema version and log the result. If preflight or upgrade fails, stop the feature initialization and show an actionable error; do not fall back to `EnsureDeleted` or silent recreation.

## P1 acceptance smoke test

1. Disconnect from the network.
2. Log in as a mock employee.
3. Upload a supported file no larger than 25 MB with a title and category.
4. Confirm the success result and the document in My Documents.
5. Repeat with an unsupported type, oversized file, missing title/category, and a mixed-validity batch; confirm rejection and no persisted file or metadata.
6. Log in as another mock user and confirm the first user's personal document is not listed or downloadable.
