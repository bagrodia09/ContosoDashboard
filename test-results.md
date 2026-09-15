# Test Results

## Date

2026-09-16

## Scope

Verified the document-upload MVP after fixing application startup failures in
`DocumentSchemaInitializer`.

## Fixes verified

- Scalar SQL Server count queries now expose the `Value` column required by
  EF Core `SqlQueryRaw<int>`.
- LocalDB backup runs before the schema-update transaction.
- Existing database initialization no longer fails with:
  - `Invalid column name 'Value'`
  - `Cannot perform a backup or restore operation within a transaction`

## Automated verification

| Check | Result |
|---|---|
| Application build | Passed |
| Document feature tests | Passed: 16 passed, 0 failed |
| `git diff --check` | Passed |
| Application startup | Passed |
| HTTP probe to `http://localhost:5000` | Passed: HTTP 200 |

Build output contains existing warnings for package version resolution,
nullable references, and the intentional dynamic SQL used for the SQL Server
backup command. No build errors remain.

## Browser upload verification

The upload flow was verified at `http://localhost:5000` using the existing
mock authentication:

1. Logged in as `Ni Kang (Employee)`.
2. Opened **My Documents**.
3. Selected a valid `test-upload.txt` file.
4. Confirmed the per-file metadata form appeared with:
   - Title
   - Category
   - Description
   - Tags
5. Submitted the upload.
6. Confirmed the success message:

   `1 file(s) uploaded successfully.`

7. Confirmed the document appeared in **Uploaded by you** with:
   - Title: `test-upload.txt`
   - Category: `Personal Files`
   - Size: `39 bytes`
   - Download link: `/api/documents/2/content`
8. Opened the download link and confirmed the stored file content was returned:

   `Offline document upload verification.`

## Result

The tested file-upload journey is working successfully:

- Mock authentication works.
- The authenticated user can open **My Documents**.
- A valid file can be selected and uploaded.
- Upload success is reported only after persistence.
- The uploaded document appears in the owner-only list.
- The authorized content endpoint returns the uploaded file.

## Additional browser observation

The browser console reported a separate stylesheet MIME warning for
`/ContosoDashboard.styles.css`. It did not prevent login, navigation, upload,
listing, or content retrieval and is outside the document-upload failure that
was fixed here.

## Recommended regression checks

Run the following negative cases manually before release:

- Unsupported extension.
- MIME type that does not match the extension.
- File larger than 25 MiB.
- Blank title.
- Blank category.
- Mixed-validity multi-file batch.
- Upload as a second mock user and verify documents remain owner-only.
