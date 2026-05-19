# Pre-Feature Hardening Issue Drafts

Use this file as a copy/paste source for creating GitHub issues.

## Issue 1
Title: P0: Upgrade WebUI module resolution for TS7 compatibility
Labels: priority-p0, webui, tooling, tech-debt
Milestone: Pre-Feature Hardening Sprint 1

Description:
TypeScript config in `Atspm/WebUI/tsconfig.json` uses a deprecated moduleResolution mode that will break in TS7.

Tasks:
- [ ] Update moduleResolution to a supported setting for current Next.js + TS toolchain.
- [ ] Run WebUI lint, build, and tests.
- [ ] Document the chosen setting and rationale in `Atspm/README.md`.

Definition of Done:
- [ ] No TS deprecation warning for moduleResolution.
- [ ] WebUI CI checks pass.

## Issue 2
Title: P0: Remove null location identifier workaround in split-fail service
Labels: priority-p0, backend, reportapi, bug-risk
Milestone: Pre-Feature Hardening Sprint 1

Description:
Current logic in `Atspm/ReportApi/ReportServices/LeftTurnSplitFailService.cs` relies on null location identifier as a workaround.

Tasks:
- [ ] Define expected behavior when identifier is missing.
- [ ] Replace null workaround with explicit logic.
- [ ] Add/extend tests for valid, missing, and edge-case identifiers.

Definition of Done:
- [ ] Hack comment removed.
- [ ] Tests pass for all defined scenarios.

## Issue 3
Title: P0: Refactor EF base repository change-tracking debt
Labels: priority-p0, infrastructure, persistence, tech-debt
Milestone: Pre-Feature Hardening Sprint 1

Description:
Change-tracking TODO/HACK debt in `Atspm/Infrastructure/Repositories/ATSPMRepositoryEFBase.cs` risks subtle update bugs.

Tasks:
- [ ] Replace temporary tracking logic with a consistent EF pattern.
- [ ] Remove attach/unattach ambiguity paths.
- [ ] Add tests for partial updates and navigation property changes.

Definition of Done:
- [ ] Existing TODO/HACK comments resolved or converted into tracked follow-ups.
- [ ] Repository tests cover changed behavior.

## Issue 4
Title: P0: Complete Cloud Run deploy workflow hardening
Labels: priority-p0, devops, ci-cd, security
Milestone: Pre-Feature Hardening Sprint 1

Description:
Deployment workflow contains unresolved TODOs in `.github/workflows/deploy.yml`.

Tasks:
- [ ] Finalize secrets strategy (no inline placeholders).
- [ ] Finalize VPC and database connectivity settings.
- [ ] Validate deployment and rollback path in CI/CD docs.

Definition of Done:
- [ ] Workflow has no unresolved deploy TODO markers.
- [ ] Deployment run is successful and repeatable.

## Issue 5
Title: P1: Move report logging responsibility to service layer
Labels: priority-p1, architecture, backend, reportapi
Milestone: Pre-Feature Hardening Sprint 2

Description:
Logging concern is noted as misplaced in `Atspm/Infrastructure/LogMessages/ReportsLoggerLogMessages.cs`.

Tasks:
- [ ] Shift report-specific logging from controller path to service path.
- [ ] Keep controller logging minimal and request-focused.
- [ ] Update tests or snapshots that depend on logging paths.

Definition of Done:
- [ ] Logging responsibility aligns with architecture.
- [ ] No functional regressions.

## Issue 6
Title: P1: Resolve analysis workflow hacks in phase termination and detector steps
Labels: priority-p1, application, analytics, tech-debt
Milestone: Pre-Feature Hardening Sprint 2

Description:
Known hack points in:
- `Atspm/Application/Analysis/Workflows/PhaseTerminationWorkflow.cs`
- `Atspm/Application/Analysis/WorkflowSteps/GetDetectorEvents.cs`

Tasks:
- [ ] Clarify algorithm assumptions and edge cases.
- [ ] Replace temporary logic with deterministic implementation.
- [ ] Add focused tests for previously hacked paths.

Definition of Done:
- [ ] Hack comments removed.
- [ ] New tests prove expected behavior.

## Issue 7
Title: P1: Replace placeholder AI-suggested archive logic
Labels: priority-p1, infrastructure, workflow, tech-debt
Milestone: Pre-Feature Hardening Sprint 2

Description:
Temporary note remains in `Atspm/Infrastructure/WorkflowSteps/ArchiveDataEvents.cs`.

Tasks:
- [ ] Implement final archive behavior.
- [ ] Confirm compatibility with existing event ingestion flow.
- [ ] Add validation tests for archive output.

Definition of Done:
- [ ] Placeholder TODO removed.
- [ ] Archive flow tested and documented.

## Issue 8
Title: P1: Fix ConfigApi boundary violations in controllers
Labels: priority-p1, configapi, api-design, tech-debt
Milestone: Pre-Feature Hardening Sprint 2

Description:
Controller-level TODO/HACKs in:
- `Atspm/ConfigApi/Controllers/LocationController.cs`
- `Atspm/ConfigApi/Controllers/DeviceController.cs`

Tasks:
- [ ] Move business logic to proper service/controller layer.
- [ ] Clarify DTO vs model boundaries.
- [ ] Keep endpoint contracts backward-compatible.

Definition of Done:
- [ ] HACK comments removed.
- [ ] API behavior unchanged for clients.

## Issue 9
Title: P1: Refactor detached entity loading outside repository abstraction
Labels: priority-p1, architecture, repositories, backend
Milestone: Pre-Feature Hardening Sprint 2

Description:
Detached-loading concerns are flagged in:
- `Atspm/Application/Repositories/ConfigurationRepositories/ILocationRepository.cs`
- `Atspm/Infrastructure/Repositories/ConfigurationRepositories/LocationEFRepository.cs`

Tasks:
- [ ] Define where detached clone/edit behavior belongs.
- [ ] Refactor interface and implementation accordingly.
- [ ] Update dependent callers and tests.

Definition of Done:
- [ ] Repository contract is cleaner and explicit.
- [ ] No regression in location version editing flows.

## Issue 10
Title: P1: Migrate Watchdog options into unified configuration model
Labels: priority-p1, watchdog, configuration, backend
Milestone: Pre-Feature Hardening Sprint 2

Description:
Migration TODOs are present in:
- `Atspm/Application/Business/Watchdog/WatchdogLoggingOptions.cs`
- `Atspm/Application/Business/Watchdog/WatchdogEmailOptions.cs`

Tasks:
- [ ] Consolidate flat options into WatchdogConfiguration.
- [ ] Update binding and defaults.
- [ ] Validate with integration/config tests.

Definition of Done:
- [ ] Flat-option TODOs resolved.
- [ ] Watchdog behavior unchanged in smoke tests.

## Issue 11
Title: P2: Add XML docs for public AtspmMath API members
Labels: priority-p2, docs, code-quality
Milestone: Pre-Feature Hardening Sprint 2

Description:
Public member doc warnings in `Atspm/Application/AtspmMath.cs`.

Tasks:
- [ ] Add XML comments for all public members in file.
- [ ] Ensure analyzer warnings are cleared.
- [ ] Keep comments accurate to behavior.

Definition of Done:
- [ ] No missing XML docs reported for this file.

## Issue 12
Title: P2: Define and execute obsolete API retirement policy
Labels: priority-p2, api-governance, cleanup
Milestone: Pre-Feature Hardening Sprint 2

Description:
Mixed obsolete/commented signatures create maintenance noise.

Tasks:
- [ ] Define deprecation lifecycle and sunset timeline.
- [ ] Remove dead commented obsolete signatures where safe.
- [ ] Track any breaking removals in changelog.

Definition of Done:
- [ ] Obsolete strategy documented.
- [ ] Stale commented signatures reduced or eliminated.

## Issue 13
Title: P2: Normalize platform guidance in docs and FAQ seed content
Labels: priority-p2, docs, platform
Milestone: Pre-Feature Hardening Sprint 2

Description:
Platform wording should reflect current containerized Linux-first workflow.

Tasks:
- [ ] Update setup guidance and references.
- [ ] Update FAQ seed text to current support posture.
- [ ] Ensure doc consistency with smoke scripts.

Definition of Done:
- [ ] No contradictory platform guidance between docs and seeded FAQ.

## Issue 14
Title: P2: Extract interfaces from LTGR report view component
Labels: priority-p2, webui, cleanup
Milestone: Pre-Feature Hardening Sprint 2

Description:
TODO in `Atspm/WebUI/src/features/leftTurnGapReport/components/LTGRReportView.tsx` indicates local type organization debt.

Tasks:
- [ ] Move interfaces to dedicated types file.
- [ ] Update imports and lint.
- [ ] Run relevant component tests.

Definition of Done:
- [ ] TODO removed.
- [ ] Component behavior unchanged.

## Issue 15
Title: P1: Enforce pre-feature quality gates in CI
Labels: priority-p1, ci-cd, quality, governance
Milestone: Pre-Feature Hardening Sprint 1

Description:
Pre-feature checks should be explicit and enforced in CI.

Tasks:
- [ ] Require backend build/tests and WebUI lint/test/build on PR.
- [ ] Add or document smoke gate for release branch or deployment stage.
- [ ] Fail builds when required checks are missing/failing.

Definition of Done:
- [ ] Gate policy documented and enforced in CI.

## Suggested Milestones
- Pre-Feature Hardening Sprint 1
- Pre-Feature Hardening Sprint 2

## Suggested Label Set
- priority-p0
- priority-p1
- priority-p2
- backend
- infrastructure
- webui
- devops
- docs
- tests
- tech-debt
- bug-risk
- architecture
- ci-cd
- configuration
