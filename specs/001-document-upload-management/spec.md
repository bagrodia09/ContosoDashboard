# Feature Specification: Document Upload and Management

**Feature Branch**: `001-document-upload-management`  
**Created**: 2026-09-15  
**Status**: Draft  
**Input**: Stakeholder requirements in `StakeholderDocs/document-upload-and-management-feature.md`; MVP priority is uploading and viewing personal documents.

> Specifications MUST be independently testable, include offline behavior and
> training-only limitations where relevant, and state authorization and
> validation expectations explicitly. Do not imply production readiness for this
> training application.

## Clarifications

### Session 2026-09-15

- Q: How should personal-document visibility work in the P1 MVP? → A: Owner-only visibility by default; personal documents remain private unless explicitly shared or associated with a project.
- Q: For the offline training MVP, how should the stakeholder's malware-scanning requirement be handled? → A: Defer real malware scanning; perform size, extension/type, content-type, and metadata validation only.
- Q: For selecting multiple files, how should document metadata be entered? → A: Enter metadata separately for each file.
- Q: Should the P1 MVP allow a user to associate an uploaded personal document with a project? → A: Strictly personal; no project association in P1.
- Q: If a multi-file upload contains both valid and invalid files, how should the P1 MVP handle the batch? → A: Reject the entire batch and persist none.
- Q: Which Office file extensions should the feature accept? → A: Modern formats only: `.docx`, `.xlsx`, and `.pptx`.
- Q: Should duplicate document titles be allowed for the same user? → A: Allow duplicate titles; identify documents by `DocumentId`.
- Q: For later administrator access-pattern reports, which activity events should be included? → A: Successful uploads, downloads, deletions, and shares only.
- Q: Should the per-file batch result contract use the proposed fields and status, or a different business-facing result? → A: Use display file name, status, validation errors, and persisted flag; use `ValidatedNotPersisted` for valid files in a rejected batch.
- Q: How should SC-003 measure the 90% first-attempt completion target? → A: Use at least 10 representative training users; the numerator is users completing the valid P1 flow on their first attempt, and file selection, metadata submission, and upload submission count as the primary actions.
- Q: When stream length is unavailable, which upload-progress UI should the P1 MVP use? → A: An indeterminate progress bar from upload start until success or error.
- Q: What HTTP contract should a rejected mixed-validity upload batch use? → A: Return HTTP 400 with the per-file result array and no persisted files or metadata.

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
  
  Assign priorities (P1, P2, P3, etc.) to each story, where P1 is the most critical.
  Think of each story as a standalone slice of functionality that can be:
  - Developed independently
  - Tested independently
  - Deployed independently
  - Demonstrated to users independently
-->

### User Story 1 - Upload and view personal documents (Priority: P1)

As an authenticated employee, I want to upload a personal document with the required metadata and view the documents I have uploaded, so that I can keep and find my work files in the dashboard.

**Why this priority**: This is the proposed first MVP slice. It provides useful centralized storage while limiting the initial journey to personal documents and the user's own document list.

**Independent Test**: Using the existing mock-authentication user, upload one supported file with valid metadata while offline, then open My Documents and confirm that the newly stored document and its metadata are listed. This slice delivers value without project, sharing, task, dashboard, or reporting integrations.

**Acceptance Scenarios**:

1. **Given** an authenticated employee and a supported file no larger than 25 MB, **When** the employee selects the file, supplies a title and category, and submits the upload, **Then** the system validates the file and metadata, stores the file and metadata, and shows a success message.
2. **Given** an authenticated employee with uploaded personal documents, **When** the employee opens My Documents, **Then** the employee sees each owned document's title, category, upload date, file size, and associated project value (blank when not associated).
3. **Given** an upload with an unsupported type, a file over 25 MB, or missing required metadata, **When** the employee submits it, **Then** the system rejects it before persistence and explains the corrective action.
4. **Given** no internet connection, **When** the employee uploads and views a valid personal document, **Then** the P1 journey remains usable with the training application's local services.

---

### User Story 2 - Browse and search accessible documents (Priority: P2)

As an employee, I want to sort, filter, and search documents that I am allowed to access, and download or preview supported documents, so that I can locate existing work quickly.

**Why this priority**: The stakeholder document identifies finding documents as a core business need, but this is separate from the P1 personal upload-and-list journey.

**Independent Test**: Seed owned, project, and shared documents with metadata, then verify sorting, category/project/date filters, searches across title/description/tags/uploader/project, permission-filtered results, downloads, and browser preview for PDF and image files.

**Acceptance Scenarios**:

1. **Given** accessible documents with different metadata, **When** an employee sorts, filters, or searches, **Then** only matching accessible documents are shown.
2. **Given** an employee with access to a document, **When** the employee downloads it or previews a supported PDF/image, **Then** the requested content is returned without exposing inaccessible documents.

---

### User Story 3 - Manage project and task documents (Priority: P3)

As a project participant or project manager, I want documents associated with projects and tasks to be visible in the relevant project/task context, so that project work has a shared document location.

**Why this priority**: Project association and task integration are stakeholder requirements that extend beyond personal documents.

**Independent Test**: Associate a document with a project and task, then verify project-team visibility, project-manager upload rights, task attachment visibility, direct task-page upload, and automatic task-project association.

**Acceptance Scenarios**:

1. **Given** a project team member and a project document, **When** the member opens the project, **Then** the member can view and download that project document.
2. **Given** a project manager, **When** the manager uploads or manages a project document, **Then** the action is allowed only for the manager's project.

---

### User Story 4 - Update and delete owned documents (Priority: P4)

As a document owner, I want to edit metadata, replace a file, or permanently delete my document after confirmation, so that my document list remains accurate.

**Why this priority**: The stakeholder document requires lifecycle management, but it is not needed to demonstrate the P1 upload-and-view slice.

**Independent Test**: As an owner, edit title/description/category/tags, replace the file, and delete after confirmation; verify denied actions for non-owners unless an explicitly authorized project role applies.

**Acceptance Scenarios**:

1. **Given** an owned document, **When** its owner edits metadata or replaces its file, **Then** the updated metadata/content is shown and the replacement follows the same validation and authorization rules.
2. **Given** an owned document, **When** its owner confirms deletion, **Then** the document content and metadata are permanently removed; cancelling leaves them unchanged.

---

### User Story 5 - Share documents and receive notifications (Priority: P5)

As a document owner, I want to share a document with specific users or teams, so that authorized recipients can find it in Shared with Me and receive an in-app notification.

**Why this priority**: Sharing is a later collaboration capability and must not expand the P1 personal-document scope.

**Independent Test**: Share an owned document with a user and a team, then verify notification delivery, recipient visibility in Shared with Me, and denial for an unauthorized recipient or actor.

**Acceptance Scenarios**:

1. **Given** an owner and an eligible recipient, **When** the owner shares a document, **Then** the recipient receives an in-app notification and the document appears in Shared with Me.
2. **Given** a user without sharing authority, **When** that user attempts to share or access a document, **Then** the action is denied.

---

### User Story 6 - See document activity in dashboard and audit views (Priority: P6)

As a dashboard user or administrator, I want document activity surfaced in the dashboard and audit reporting, so that recent work and access patterns are visible.

**Why this priority**: Recent Documents, document counts, activity tracking, notifications, and administrator reports are later integrations rather than prerequisites for the MVP.

**Independent Test**: Create representative document activity and verify the user's five most recent uploads, dashboard count, project notifications, activity records, and administrator reports for document types, uploaders, and access patterns.

**Acceptance Scenarios**:

1. **Given** a user with uploaded documents, **When** the user opens the dashboard, **Then** the Recent Documents widget shows the five most recent uploads and the summary includes a document count.
2. **Given** document activity and an administrator, **When** the administrator requests a report, **Then** the report includes the stakeholder-defined upload-type, uploader, and access-pattern views.

---

### Edge Cases

- A multi-file selection contains both valid and invalid files; the result MUST make clear which files were accepted or rejected and MUST NOT persist a rejected file.
- A file is exactly 25 MB versus just over 25 MB.
- A file has a permitted extension but an inconsistent or unsupported content type.
- The title is blank or whitespace-only; duplicate titles are allowed because documents are identified by `DocumentId`.
- A selected project is not one the current user may access or manage.
- Local file storage fails after validation; metadata MUST NOT remain for a file that was not saved.
- A document path, original filename, or search input contains traversal characters or unexpected Unicode.
- A user attempts to read, download, replace, share, or delete a document outside their authorization.
- A preview is requested for a file type other than PDF or image.
- The user loses connectivity during a training upload; the application MUST show an actionable failure and preserve no unsaved metadata.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow an authenticated user to select one or more files for upload.
- **FR-002**: The system MUST accept `.pdf`, `.docx`, `.xlsx`, `.pptx`, `.txt`, `.jpg`, `.jpeg`, and `.png` files, with their corresponding supported MIME types, and MUST reject unsupported types with an actionable message. Legacy and macro-enabled Office formats are out of scope.
- **FR-003**: The system MUST reject each file larger than 25 MiB (26,214,400 bytes) with an actionable message; a file exactly 26,214,400 bytes is accepted if all other validation passes.
- **FR-004**: The uploader MUST provide a non-empty document title and select one category from Project Documents, Team Resources, Personal Files, Reports, Presentations, or Other.
- **FR-005**: The uploader MAY provide a description and custom tags. Project association is not available in the P1 personal-document journey and is deferred to later project-document stories.
- **FR-005a**: P1 metadata MUST enforce maximum lengths of 200 characters for title, 2,000 characters for description, and 1,000 characters for tags.
- **FR-006**: The system MUST capture upload date/time, uploader identity, file size, and MIME/content type for each accepted document; the content-type value MUST support the stakeholder-specified long values.
- **FR-007**: The system MUST validate size, extension/type, content type, required metadata, and applicable project authorization before persistence.
- **FR-008**: The system MUST store file content outside `wwwroot` through `IFileStorageService`, use a unique relative path that never directly uses a user-supplied filename, and retain a portable relative path in metadata.
- **FR-009**: The upload workflow MUST generate a unique path, save the file, and then save metadata; a failed file save MUST NOT leave a document metadata record. Atomic multi-file rejection and per-file result behavior are defined canonically in FR-014a.
- **FR-010**: The system MUST enforce authorization in the service layer for every document read, download, preview, upload, replace, share, and delete operation, in addition to applicable page or endpoint checks.
- **FR-011**: In the P1 journey, My Documents MUST list all documents uploaded by the current user and show title, category, upload date, file size, and an empty associated-project value because P1 documents are strictly personal.
- **FR-012**: The system MUST support offline P1 upload and viewing using local training storage and mock authentication, without cloud or external service dependencies.
- **FR-013**: The system MUST provide file-based upload progress for each selected file and an aggregate batch progress indicator, each reported as a percentage from 0% through 100% when the stream length is known. For an indeterminate stream, the P1 UI MUST show an indeterminate progress bar from upload start until success or error. The system MUST show success only after the complete batch is persisted, and MUST show an error result when validation, storage, or metadata persistence fails.
- **FR-014**: For a multi-file selection, the system MUST collect and validate title, category, description, and tags separately for each file. The P1 form MUST omit project association because P1 uploads are strictly personal.
- **FR-014c**: In multipart requests, metadata is positional: metadata item `i` belongs to file `i`. The files and metadata arrays MUST have equal lengths; a mismatch MUST return HTTP 400 before storage is called.
- **FR-014a**: If any file or its metadata in a multi-file P1 submission is invalid, the system MUST reject the entire batch, persist no files or metadata from that batch, and return a per-file result containing the display file name, status, validation errors, and persisted flag; files that passed validation MUST have status `ValidatedNotPersisted` and `persisted=false`. This is the canonical batch rejection and cleanup requirement.
- **FR-014b**: A rejected mixed-validity upload batch MUST return HTTP 400 with the FR-014a per-file result array and MUST persist no files or metadata.
- **FR-015**: The system MUST support later sorting by title, upload date, category, and file size; filtering by category, project, and date range; and searching title, description, tags, uploader, and project, while returning only authorized results.
- **FR-016**: The system MUST support later download of any accessible document and in-browser preview of accessible PDF and image documents.
- **FR-017**: The system MUST support later owner metadata edits and file replacement using the same validation, storage ordering, and authorization rules.
- **FR-018**: The system MUST support later permanent deletion after confirmation by an authorized owner or project manager; soft-delete/recovery is out of scope.
- **FR-019**: The system MUST support later sharing with specific users or teams, in-app notifications for recipients, and a Shared with Me view.
- **FR-020**: The system MUST support later project/task associations, project-member viewing/downloading, project-manager project uploads, task-page attachment visibility, task-page uploads, and automatic task-project association.
- **FR-020a**: A later document association MUST contain exactly one of `projectId` or `taskId`; a task association MUST validate and derive its project context from the selected task.
- **FR-021**: The system MUST support later dashboard Recent Documents (the five most recent user uploads), dashboard document counts, notifications for new project documents, and administrator activity reports.
- **FR-022**: The system MUST log later successful uploads, downloads, deletions, and share actions with document ID, actor, UTC timestamp, and operation outcome sufficient to produce administrator reports. Activity-event retrieval and report aggregation are separate operations. Administrator reports MUST accept a UTC date-range and document-category filter and output document-type counts, uploader counts, and successful access counts grouped by operation. Denied attempts are not included in access-pattern reports.
- **FR-023**: The training MVP MUST defer real malware scanning because it operates offline; size, extension/type, content type, and metadata validation MUST occur before persistence.
- **FR-024**: The training implementation MUST document that real malware scanning and production security assurances are unavailable offline and are a production migration concern; the storage and validation boundary MUST remain replaceable for a future production scanner.
- **FR-025**: The system MUST preserve the existing mock-authentication claims, role, project, team, and ownership rules; it MUST NOT introduce real identity-provider or password requirements.
- **FR-026**: The document identifier MUST be an integer and category values MUST be stored as text, consistent with the stakeholder constraints.
- **FR-027**: The P1 personal-document visibility rule MUST be owner-only by default; personal documents remain private unless explicitly shared or associated with a project.

### P1 file type validation matrix

The extension and MIME type MUST match one of these exact pairs. A mismatch is
rejected before persistence with an actionable validation error. MIME values
are case-insensitive; the stored value preserves the submitted MIME value after
validation.

| Extension | Accepted MIME type |
|---|---|
| `.pdf` | `application/pdf` |
| `.docx` | `application/vnd.openxmlformats-officedocument.wordprocessingml.document` |
| `.xlsx` | `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet` |
| `.pptx` | `application/vnd.openxmlformats-officedocument.presentationml.presentation` |
| `.txt` | `text/plain` |
| `.jpg` | `image/jpeg` |
| `.jpeg` | `image/jpeg` |
| `.png` | `image/png` |

### P1 batch result contract

`Rejected` means the file failed one or more validation rules. `ValidatedNotPersisted`
means the file passed validation but was not stored because another file caused
the atomic batch to fail. Both statuses have `persisted=false`; a rejected batch
never returns a persisted file result.

### Later-story contract boundary

The feature contract MUST define minimal request and response shapes before
implementing later search, project/task association, lifecycle, sharing,
notifications, dashboard, and reporting stories. Those contracts do not
expand the P1 MVP.

### Key Entities *(include if feature involves data)*

- **Document**: An uploaded file's metadata and ownership, including integer identifier, title, description, category text, tags, original filename for display, safe relative storage path, upload time, uploader, file size, and content type. Project/task associations are deferred future fields and are excluded from the P1 schema.
- **Document Share**: A later relationship granting a document to a specific user or team and supporting recipient notification and Shared with Me visibility.
- **Project/Task association**: A later relationship connecting a document to the project or task context in which it is relevant; unavailable for strictly personal P1 uploads.
- **Document activity**: A later record of upload, download, deletion, or share activity used by administrator reporting.

## Assumptions

- The existing mock-authentication system supplies the current user and existing role/project/team membership information.
- The feature remains a web-only, offline-capable training feature; local filesystem storage is available.
- The stakeholder's proposed 25 MB limit and supported-type list apply to each file.
- The stakeholder's implementation notes are treated as architectural constraints only where they agree with the constitution; the constitution governs offline limitations and service-level authorization.
- P1 does not include project association, sharing, replacement, deletion, search, preview, task/dashboard integration, notifications, or reporting unless a later story is explicitly implemented. P1 uploads are strictly personal and owner-only; later sharing or project association may grant access under their respective authorization rules.
- Performance figures from the stakeholder document (30-second upload, two-second lists/search, three-second preview) are candidate later benchmarks, not offline training release gates.
- The stakeholder's three-month adoption, categorization, and zero-incident metrics are business hypotheses and are not evidence of production readiness.

## Resolved Clarifications

- **Metadata for multiple files**: Resolved as per-file metadata. Each selected file receives its own title, category, description, project, and tags before persistence.
- **Malware scanning**: Resolved for the training MVP by deferring real scanning and requiring pre-persistence size, type, content-type, and metadata validation. Production malware scanning remains a migration requirement.
- **Personal-document visibility**: Resolved for P1 as owner-only by default. Later sharing or project association may grant access through the corresponding authorized flows.
- **P1 project association**: Resolved as strictly personal with no project association. The associated-project field remains present in document-list projections but is blank/null for every P1 personal document. Project association is deferred to later project-document stories.
- **Office formats**: Resolved to modern non-macro formats only: `.docx`, `.xlsx`, and `.pptx`; legacy and macro-enabled formats are out of scope.
- **Duplicate titles**: Resolved as allowed; no title uniqueness constraint applies because `DocumentId` is the identifier.
- **SC-003 measurement**: Resolved to a sample of at least 10 representative training users; measure first-attempt completion as the numerator over the sample and count file selection, metadata submission, and upload submission as primary actions.
- **Administrator activity reports**: Resolved to successful uploads, downloads, deletions, and shares; denied attempts are excluded from access-pattern reports.
- **Administrator report contract**: Resolved to UTC date-range and document-category filters, with document-type counts, uploader counts, and successful access counts grouped by operation.
- **Mixed-validity batches**: Resolved by FR-014a; no separate behavior is defined here.
- **Progress reporting**: Resolved to per-file and aggregate batch progress, with indeterminate progress allowed when stream length is unavailable.
- **Indeterminate progress UI**: Resolved to an indeterminate progress bar from upload start until success or error.
- **MIME validation**: Resolved to the exact extension/MIME matrix above; mismatches are rejected.
- **Batch result semantics**: Resolved so `Rejected` identifies a file-level validation failure and `ValidatedNotPersisted` identifies a valid file withheld by atomic batch rejection.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In offline acceptance tests, 100% of valid P1 uploads at or below 25 MB are either stored with metadata and listed in My Documents or produce a documented actionable failure; no failed save leaves metadata behind.
- **SC-002**: In offline acceptance tests, 100% of P1 My Documents results show title, category, upload date, file size, and associated project for every returned document.
- **SC-003**: In a usability check with at least 10 representative training users, at least 90% of users MUST complete the valid P1 upload-and-view journey on the first attempt. The primary actions are file selection, metadata submission, and upload submission; the journey MUST require no more than three such actions after the user begins the upload flow.
- **SC-004**: 100% of authorization acceptance tests deny access to documents outside the current user's permitted ownership, role, project, team, or share relationship.
- **SC-005**: The P1 upload and My Documents journey completes without internet access and without cloud or external-service dependencies.
- **SC-006**: For later stories, candidate stakeholder benchmarks are up to 30 seconds for a 25 MB upload, up to 2 seconds for a 500-document list/search, and up to 3 seconds for preview; these are measured as deferred local benchmarks, not current MVP release gates.
