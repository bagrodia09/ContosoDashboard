<!--
Sync Impact Report
- Version change: template/unversioned -> 1.0.0
- Modified principles: five template placeholders replaced with ContosoDashboard
  training, architecture, storage/security, authorization/validation, and testing
  principles.
- Added sections: Training Constraints; Delivery and Review Workflow.
- Removed sections: none (template sections were retained and made concrete).
- Templates updated: .specify/templates/plan-template.md (✅),
  .specify/templates/spec-template.md (✅), .specify/templates/tasks-template.md
  (✅), .github/agents/speckit.tasks.agent.md (✅).
- Command templates: ⚠ pending/not present at
  .specify/templates/commands/*.md; repository uses .github/agents and
  .github/prompts instead.
- Runtime guidance: README.md and StakeholderDocs/document-upload-and-management-feature.md
  already align with the required offline, mock-authentication, and storage
  abstraction rules (✅ reviewed; no edits required).
- Follow-up TODOs: none. Ratification date is inferred from the first tracked
  constitution commit (2025-12-15).
-->

# ContosoDashboard Constitution

## Core Principles

### I. Training-Only Scope and Offline Operation
ContosoDashboard MUST remain a training application and MUST operate for its core
scenarios without internet access, cloud subscriptions, or external service
dependencies. Documentation, features, and acceptance criteria MUST label mock
authentication, local persistence, and other simplified controls as training-only.
No change may imply production readiness, warranties, compliance, or a supported
security posture. This keeps the course reproducible and prevents training
shortcuts from being mistaken for production architecture.

### II. Preserve the Deliberate Application Architecture
Features MUST fit the existing ASP.NET Core 8 Blazor Server architecture and its
Models, Data, Services, and Pages separation. Existing mock cookie authentication,
claims, roles, and protected-page flow MUST be preserved unless a constitution
amendment explicitly authorizes a change. New infrastructure dependencies MUST be
introduced through dependency injection and small, testable services rather than
cross-layer shortcuts. This preserves the learning objectives and keeps the
application runnable offline.

### III. Local Storage with a Cloud-Migration Abstraction
File features MUST store content in a dedicated local directory outside `wwwroot`
(for example, `AppData/uploads`) and MUST never expose user-supplied names as
physical paths. All business logic MUST depend on `IFileStorageService`, with a
local implementation for training and a future cloud implementation replaceable
through dependency injection. Implementations MUST generate a unique relative path
before persistence, use safe path handling, and execute the sequence
generate-path → save-file → save-metadata. This provides safe training storage
while preserving a credible migration seam.

### IV. Defense-in-Depth Authorization and Input Validation
Authorization MUST be enforced at the service layer for every read, write,
download, replace, share, and delete operation, in addition to page, endpoint, or
middleware checks. Services MUST prevent IDOR and enforce the existing role,
project, team, and ownership rules. File handling MUST validate size, extension,
content type, and required metadata before storage, return actionable errors, and
reject unsupported or unsafe input. Virus scanning is not available offline; the
training limitation MUST be documented and the storage boundary MUST remain
replaceable for a production scanner. These controls make acceptance tests
verifiable and prevent UI-only security.

### V. Testable Acceptance and Change Discipline
Every feature specification MUST define independently testable user journeys,
explicit acceptance scenarios, edge cases, and measurable success criteria.
Implementation plans MUST include a Constitution Check, and tasks MUST include
unit/integration/acceptance coverage for authorization, validation, storage
ordering, and offline behavior where applicable. Tests MUST be runnable offline
and MUST cover both allowed and denied paths. A change is not complete until
documentation, tests, and the relevant acceptance criteria agree.

## Training Constraints

The application uses mock authentication with selectable users and no passwords;
it is not an identity provider. LocalDB and local filesystem storage are the
supported training dependencies. The initial document feature does not promise
real malware scanning, external sharing, collaborative editing, version history,
quota management, mobile clients, or cloud durability. Requirements that assume
internet connectivity, Azure services, production-grade malware scanning, or
regulatory compliance conflict with Principle I and MUST be marked out of scope
or explicitly deferred to a production migration.

Known conflicts in the stakeholder document are resolved as follows:

- “Scan uploaded files for viruses and malware” requires an external scanner
  that is unavailable offline; the training implementation MUST validate size,
  type, and metadata and MUST document malware scanning as a production
  migration requirement.
- Upload and search targets described as “typical network” or “within two
  seconds” are not release gates for an offline LocalDB demonstration; plans
  MUST replace them with measurable local acceptance tests or mark them as
  deferred benchmarks.
- Three-month adoption and production-style audit/compliance outcomes are
  business hypotheses, not training acceptance criteria; they MUST NOT be
  represented as evidence of production readiness.

## Delivery and Review Workflow

Before implementation, the plan MUST identify the affected architectural layer,
storage boundary, authorization decisions, validation rules, offline behavior,
and test strategy. Reviewers MUST verify that files remain outside `wwwroot`,
business logic uses `IFileStorageService`, service-level authorization is
present, and failure paths do not leave metadata for an unsaved file. Reviewers
MUST also check that mock-authentication and training-only limitations remain
visible in user-facing and developer documentation. Any justified deviation MUST
be recorded in the plan's Complexity Tracking table and linked to an acceptance
criterion.

## Governance

This constitution is authoritative for feature specifications, plans, tasks,
implementation, and review. An amendment MUST state the motivation, affected
principles, compatibility impact, and required template/document updates. The
amendment author MUST update dependent templates and guidance in the same change,
or record a dated follow-up with an owner. Every feature review MUST perform the
Constitution Check and reject unrecorded violations.

Versions use semantic versioning: MAJOR for backward-incompatible governance or
principle removal/redefinition, MINOR for new or materially expanded principles
or sections, and PATCH for clarifications and non-semantic wording changes.
The last-amended date MUST be the date of the approved amendment in ISO format.
Ratification is retained as the original adoption date. The constitution MUST be
reviewed when a feature changes authentication, storage, authorization, offline
operation, or test obligations.

**Version**: 1.0.0 | **Ratified**: 2025-12-15 | **Last Amended**: 2026-09-15
