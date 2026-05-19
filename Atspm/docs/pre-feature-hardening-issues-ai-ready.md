# Pre-Feature Hardening AI Execution Backlog

This document is designed for AI coding agents.

## Global Agent Rules

1. Work one issue per PR.
2. Do not change files outside the listed in-scope paths.
3. Do not introduce schema changes unless explicitly requested.
4. Preserve public API and endpoint contracts unless explicitly requested.
5. Run all validation commands before finalizing.
6. Final report must include:
- Changed files
- Behavior changes
- Validation output summary
- Risks and rollback notes

## Common Validation Commands

1. dotnet build Atspm/ATSPM.sln
2. ./Atspm/scripts/smoke-all.sh
3. cd Atspm/WebUI && npm run lint && npm test -- --watch=false

## Issue 1

Title: P0: Upgrade WebUI module resolution for TS7 compatibility
Priority: P0
Milestone: Sprint 1
Labels: webui, tooling, tech-debt

Objective:
- Remove deprecated TypeScript module resolution configuration and keep WebUI build/test behavior stable.

In-scope files:
- Atspm/WebUI/tsconfig.json
- Atspm/WebUI/package.json
- Atspm/WebUI (only files required by lint/build/test fixes)

Out-of-scope files:
- Any C# project files
- API service runtime code

Non-goals:
- No framework migration
- No broad TS refactor

Implementation constraints:
- Keep Next.js and existing scripts intact.
- Prefer minimal config updates over source-wide edits.

Validation commands:
- cd Atspm/WebUI && npm run lint
- cd Atspm/WebUI && npm test -- --watch=false
- cd Atspm/WebUI && npm run build

Definition of done:
- No deprecated moduleResolution warning.
- WebUI lint/test/build pass.

Risk level:
- Medium

Rollback plan:
- Revert tsconfig and package changes as one commit.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 2

Title: P0: Remove null location identifier workaround in split-fail service
Priority: P0
Milestone: Sprint 1
Labels: backend, reportapi, bug-risk

Objective:
- Replace null location identifier workaround with explicit, correct behavior.

In-scope files:
- Atspm/ReportApi/ReportServices/LeftTurnSplitFailService.cs
- Atspm/ReportApiTests/**

Out-of-scope files:
- Unrelated report services
- Database schema or migrations

Non-goals:
- No endpoint signature changes.

Implementation constraints:
- Keep existing API contract.
- Add tests for missing/invalid/valid identifier paths.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- ./Atspm/scripts/smoke-all.sh
- Run affected ReportApi tests

Definition of done:
- No null placeholder path remains.
- Behavior covered by tests.

Risk level:
- High

Rollback plan:
- Revert service and tests in one commit.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 3

Title: P0: Refactor EF base repository change-tracking debt
Priority: P0
Milestone: Sprint 1
Labels: infrastructure, persistence, tech-debt

Objective:
- Remove hacky attach/detach/update tracking behavior and stabilize repository updates.

In-scope files:
- Atspm/Infrastructure/Repositories/ATSPMRepositoryEFBase.cs
- Atspm/InfrastructureTests/** (relevant tests)

Out-of-scope files:
- API controller behavior
- Domain model redesign

Non-goals:
- No broad repository rewrite.

Implementation constraints:
- Preserve current repository public contract.
- Add regression tests for update scenarios.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- Run InfrastructureTests for repository update paths
- ./Atspm/scripts/smoke-all.sh

Definition of done:
- TODO/HACK update-tracking comments resolved.
- Regression tests pass.

Risk level:
- High

Rollback plan:
- Revert repository base + tests.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 4

Title: P0: Complete Cloud Run deploy workflow hardening
Priority: P0
Milestone: Sprint 1
Labels: devops, ci-cd, security

Objective:
- Finalize deploy workflow TODOs for secrets and connectivity sizing.

In-scope files:
- .github/workflows/deploy.yml
- .github/workflows/build.yml (if needed for wiring)
- docs entries for deployment runbook

Out-of-scope files:
- Application runtime code

Non-goals:
- No cloud architecture redesign.

Implementation constraints:
- Keep least-privilege secret handling.
- Avoid plaintext secrets.

Validation commands:
- CI workflow lint/validation
- Dry-run or non-prod deployment validation if available

Definition of done:
- Workflow TODOs removed and replaced with explicit configuration.

Risk level:
- Medium

Rollback plan:
- Revert workflow changes.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 5

Title: P1: Move report logging responsibility to service layer
Priority: P1
Milestone: Sprint 2
Labels: architecture, backend, reportapi

Objective:
- Align logging placement with service-layer ownership.

In-scope files:
- Atspm/Infrastructure/LogMessages/ReportsLoggerLogMessages.cs
- Atspm/ReportApi/ReportServices/** (targeted changes only)

Out-of-scope files:
- Authentication/authorization code

Non-goals:
- No log format overhaul.

Implementation constraints:
- Preserve existing log message semantics where possible.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- Run relevant ReportApi tests

Definition of done:
- Logging originates from service layer where intended.

Risk level:
- Medium

Rollback plan:
- Revert logging relocation changes.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 6

Title: P1: Resolve analysis workflow hacks in phase termination and detector steps
Priority: P1
Milestone: Sprint 2
Labels: application, analytics, tech-debt

Objective:
- Replace known hack paths with deterministic, tested logic.

In-scope files:
- Atspm/Application/Analysis/Workflows/PhaseTerminationWorkflow.cs
- Atspm/Application/Analysis/WorkflowSteps/GetDetectorEvents.cs
- Atspm/ApplicationTests/Analysis/** (relevant tests)

Out-of-scope files:
- Unrelated workflow families

Non-goals:
- No full workflow framework rewrite.

Implementation constraints:
- Keep output shape and upstream/downstream contracts stable.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- Run relevant ApplicationTests workflow tests

Definition of done:
- Hack comments resolved with tested behavior.

Risk level:
- High

Rollback plan:
- Revert workflow step changes + tests.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 7

Title: P1: Replace placeholder AI-suggested archive logic
Priority: P1
Milestone: Sprint 2
Labels: infrastructure, workflow, tech-debt

Objective:
- Replace placeholder note with production-grade archive behavior.

In-scope files:
- Atspm/Infrastructure/WorkflowSteps/ArchiveDataEvents.cs
- Relevant tests in InfrastructureTests

Out-of-scope files:
- Event decoder/importer redesign

Non-goals:
- No archival storage strategy redesign.

Implementation constraints:
- Keep existing observable output semantics unless explicitly approved.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- Run relevant InfrastructureTests

Definition of done:
- Placeholder TODO removed with tested implementation.

Risk level:
- Medium

Rollback plan:
- Revert archive step changes.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 8

Title: P1: Fix ConfigApi boundary violations in controllers
Priority: P1
Milestone: Sprint 2
Labels: configapi, api-design, tech-debt

Objective:
- Move misplaced responsibilities to correct layer and clarify DTO/model use.

In-scope files:
- Atspm/ConfigApi/Controllers/LocationController.cs
- Atspm/ConfigApi/Controllers/DeviceController.cs
- Atspm/ConfigApi/Services/** (if needed)
- Atspm/ConfigApiTests/** (if present)

Out-of-scope files:
- Breaking API route changes

Non-goals:
- No full API redesign.

Implementation constraints:
- Keep existing endpoint contracts.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- ./Atspm/scripts/smoke-all.sh

Definition of done:
- HACK comments resolved with proper layering.

Risk level:
- Medium

Rollback plan:
- Revert controller/service refactor changes.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 9

Title: P1: Refactor detached entity loading outside repository abstraction
Priority: P1
Milestone: Sprint 2
Labels: architecture, repositories, backend

Objective:
- Move detached-loading concern to a more appropriate layer and simplify repository contract.

In-scope files:
- Atspm/Application/Repositories/ConfigurationRepositories/ILocationRepository.cs
- Atspm/Infrastructure/Repositories/ConfigurationRepositories/LocationEFRepository.cs
- Dependent service files (minimal)

Out-of-scope files:
- Domain model changes unrelated to location versioning

Non-goals:
- No broad repository interface rework beyond this concern.

Implementation constraints:
- Preserve behavior for callers.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- Run tests touching location versioning/edit flows

Definition of done:
- Detached-loading TODOs resolved and behavior covered by tests.

Risk level:
- Medium

Rollback plan:
- Revert interface/implementation adjustments.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 10

Title: P1: Migrate Watchdog options into unified configuration model
Priority: P1
Milestone: Sprint 2
Labels: watchdog, configuration, backend

Objective:
- Move flat options to unified Watchdog configuration model.

In-scope files:
- Atspm/Application/Business/Watchdog/WatchdogLoggingOptions.cs
- Atspm/Application/Business/Watchdog/WatchdogEmailOptions.cs
- Related configuration wiring files

Out-of-scope files:
- Watchdog report logic redesign

Non-goals:
- No feature behavior changes.

Implementation constraints:
- Backward compatibility for existing config keys if feasible.

Validation commands:
- dotnet build Atspm/ATSPM.sln
- ./Atspm/scripts/smoke-all.sh

Definition of done:
- Migration TODOs resolved; configuration loads correctly.

Risk level:
- Medium

Rollback plan:
- Revert config model and binder changes.

Deliverable format:
- Summary, file list, validation results, residual risks.

## Issue 11

Title: P2: Add XML docs for public AtspmMath API members
Priority: P2
Milestone: Sprint 2
Labels: docs, code-quality

Objective:
- Remove missing XML doc warnings for public methods.

In-scope files:
- Atspm/Application/AtspmMath.cs

Out-of-scope files:
- Math behavior changes

Non-goals:
- No algorithm refactor.

Implementation constraints:
- Comments must match actual behavior.

Validation commands:
- dotnet build Atspm/ATSPM.sln

Definition of done:
- No missing XML comments for public members in this file.

Risk level:
- Low

Rollback plan:
- Revert comment-only changes.

Deliverable format:
- Summary, file list, validation results.

## Issue 12

Title: P2: Define and execute obsolete API retirement policy
Priority: P2
Milestone: Sprint 2
Labels: api-governance, cleanup

Objective:
- Reduce obsolete/commented legacy noise and define retirement timeline.

In-scope files:
- Atspm/Application/Extensions/ModelExtensions.cs
- Atspm/Application/Repositories/ConfigurationRepositories/** (targeted)
- Changelog or docs for policy note

Out-of-scope files:
- Breaking removals without explicit approval

Non-goals:
- No broad API versioning overhaul.

Implementation constraints:
- Keep backward compatibility unless issue explicitly allows break.

Validation commands:
- dotnet build Atspm/ATSPM.sln

Definition of done:
- Policy documented and stale commented signatures reduced.

Risk level:
- Medium

Rollback plan:
- Revert interface signature cleanups.

Deliverable format:
- Summary, file list, compatibility notes.

## Issue 13

Title: P2: Normalize platform guidance in docs and FAQ seed content
Priority: P2
Milestone: Sprint 2
Labels: docs, platform

Objective:
- Keep docs and seeded FAQ aligned with current Linux/container-first ops.

In-scope files:
- Atspm/README.md
- Atspm/Data/Configuration/FaqConfiguration.cs

Out-of-scope files:
- Runtime code changes

Non-goals:
- No broad docs rewrite.

Implementation constraints:
- Preserve user-facing intent while updating stale guidance.

Validation commands:
- Optional docs link/path check

Definition of done:
- No contradictory platform messaging in these sources.

Risk level:
- Low

Rollback plan:
- Revert doc/seed content changes.

Deliverable format:
- Summary, file list, content diff rationale.

## Issue 14

Title: P2: Extract interfaces from LTGR report view component
Priority: P2
Milestone: Sprint 2
Labels: webui, cleanup

Objective:
- Move local interfaces into dedicated type file for maintainability.

In-scope files:
- Atspm/WebUI/src/features/leftTurnGapReport/components/LTGRReportView.tsx
- Atspm/WebUI/src/features/leftTurnGapReport/** (types file)

Out-of-scope files:
- Feature behavior changes

Non-goals:
- No UI or API behavior changes.

Implementation constraints:
- Keep component props and rendering unchanged.

Validation commands:
- cd Atspm/WebUI && npm run lint
- cd Atspm/WebUI && npm test -- --watch=false

Definition of done:
- TODO removed and tests pass.

Risk level:
- Low

Rollback plan:
- Revert component/type extraction commit.

Deliverable format:
- Summary, file list, validation results.

## Issue 15

Title: P1: Enforce pre-feature quality gates in CI
Priority: P1
Milestone: Sprint 1
Labels: ci-cd, quality, governance

Objective:
- Enforce baseline build/lint/test/smoke checks before feature merges.

In-scope files:
- .github/workflows/build.yml
- .github/workflows/deploy.yml
- Atspm/scripts/smoke-all.sh
- Atspm/README.md (developer workflow section, if needed)

Out-of-scope files:
- Core runtime refactors

Non-goals:
- No new deployment platform adoption.

Implementation constraints:
- Keep pipeline runtime reasonable.
- Prefer incremental gating over all-at-once heavy jobs.

Validation commands:
- CI workflow run on test branch

Definition of done:
- Required checks enforced and documented.

Risk level:
- Medium

Rollback plan:
- Revert workflow gate commits.

Deliverable format:
- Summary, file list, pipeline evidence links.

## Recommended Agent Work Order

1. Issue 1
2. Issue 2
3. Issue 3
4. Issue 4
5. Issue 15
6. Issue 8
7. Issue 9
8. Issue 10
9. Issue 6
10. Issue 7
11. Issue 5
12. Issue 11
13. Issue 12
14. Issue 13
15. Issue 14
