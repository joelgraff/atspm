# Pre-Feature Hardening Agent Prompt Pack

Use one prompt at a time in Copilot Chat. Keep one issue per PR.

## Prompt 01 - TS module resolution upgrade (P0)
You are implementing Issue 1 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Remove deprecated TypeScript module resolution usage and keep WebUI behavior stable.

In-scope:
- Atspm/WebUI/tsconfig.json
- Atspm/WebUI/package.json
- Minimal related WebUI files only if required by lint/build/test.

Out-of-scope:
- C# backend code
- API service logic

Constraints:
- No framework migration.
- Keep Next.js scripts intact.
- Minimal diff preferred.

Validation:
1. cd Atspm/WebUI && npm run lint
2. cd Atspm/WebUI && npm test -- --watch=false
3. cd Atspm/WebUI && npm run build

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 02 - Split-fail null identifier fix (P0)
You are implementing Issue 2 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Replace null location identifier workaround with explicit, correct behavior.

In-scope:
- Atspm/ReportApi/ReportServices/LeftTurnSplitFailService.cs
- Relevant ReportApi tests

Out-of-scope:
- Endpoint signatures
- DB schema changes

Constraints:
- Preserve API contract.
- Add tests for valid/missing/invalid identifier paths.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run relevant ReportApi tests
3. ./Atspm/scripts/smoke-all.sh

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 03 - EF base tracking refactor (P0)
You are implementing Issue 3 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Remove attach/detach/update tracking hacks from repository base.

In-scope:
- Atspm/Infrastructure/Repositories/ATSPMRepositoryEFBase.cs
- Relevant InfrastructureTests

Out-of-scope:
- API controller behavior
- Domain redesign

Constraints:
- Preserve repository public contracts.
- Add regression tests for update scenarios.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run relevant InfrastructureTests
3. ./Atspm/scripts/smoke-all.sh

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 04 - Deploy workflow hardening (P0)
You are implementing Issue 4 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Resolve deploy workflow TODOs for secrets and VPC/DB connectivity sizing.

In-scope:
- .github/workflows/deploy.yml
- .github/workflows/build.yml (only if needed)
- Related docs if required

Out-of-scope:
- Application runtime code

Constraints:
- No plaintext secrets.
- Keep least-privilege design.

Validation:
1. Validate workflow syntax
2. Provide expected deployment verification steps

Deliverables:
- Implement changes
- Return changed files + validation summary + risks

## Prompt 05 - Move report logging to service layer (P1)
You are implementing Issue 5 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Align logging responsibility with report service layer.

In-scope:
- Atspm/Infrastructure/LogMessages/ReportsLoggerLogMessages.cs
- Targeted files under Atspm/ReportApi/ReportServices

Out-of-scope:
- Auth logic

Constraints:
- Preserve log semantics where possible.
- Minimal behavioral change.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run relevant ReportApi tests

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 06 - Resolve analysis workflow hacks (P1)
You are implementing Issue 6 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Replace hack paths with deterministic tested behavior.

In-scope:
- Atspm/Application/Analysis/Workflows/PhaseTerminationWorkflow.cs
- Atspm/Application/Analysis/WorkflowSteps/GetDetectorEvents.cs
- Relevant ApplicationTests under analysis workflows/steps

Out-of-scope:
- Unrelated workflow families

Constraints:
- Preserve output contract and integration points.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run targeted ApplicationTests

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 07 - Replace archive placeholder logic (P1)
You are implementing Issue 7 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Replace placeholder archive TODO with production-grade logic.

In-scope:
- Atspm/Infrastructure/WorkflowSteps/ArchiveDataEvents.cs
- Relevant InfrastructureTests

Out-of-scope:
- Event decoder redesign

Constraints:
- Preserve observable behavior unless explicitly justified.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run targeted InfrastructureTests

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 08 - ConfigApi boundary cleanup (P1)
You are implementing Issue 8 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Fix misplaced controller responsibilities and DTO/model boundary debt.

In-scope:
- Atspm/ConfigApi/Controllers/LocationController.cs
- Atspm/ConfigApi/Controllers/DeviceController.cs
- Minimal supporting service files

Out-of-scope:
- Breaking endpoint changes

Constraints:
- Preserve endpoint contracts.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. ./Atspm/scripts/smoke-all.sh

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 09 - Detached-loading refactor (P1)
You are implementing Issue 9 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Move detached-loading concern out of repository abstraction.

In-scope:
- Atspm/Application/Repositories/ConfigurationRepositories/ILocationRepository.cs
- Atspm/Infrastructure/Repositories/ConfigurationRepositories/LocationEFRepository.cs
- Minimal dependent callers

Out-of-scope:
- Broad repository interface redesign

Constraints:
- Preserve behavior and call contracts.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. Run targeted tests for location versioning flows

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 10 - Watchdog config migration (P1)
You are implementing Issue 10 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Consolidate watchdog options into unified configuration model.

In-scope:
- Atspm/Application/Business/Watchdog/WatchdogLoggingOptions.cs
- Atspm/Application/Business/Watchdog/WatchdogEmailOptions.cs
- Related configuration wiring files

Out-of-scope:
- Watchdog feature redesign

Constraints:
- Preserve runtime behavior and compatibility where possible.

Validation:
1. dotnet build Atspm/ATSPM.sln
2. ./Atspm/scripts/smoke-all.sh

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary + risks

## Prompt 11 - Add XML docs for AtspmMath (P2)
You are implementing Issue 11 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Add missing XML docs for public AtspmMath members.

In-scope:
- Atspm/Application/AtspmMath.cs

Out-of-scope:
- Math algorithm changes

Constraints:
- Documentation-only or near-documentation-only changes.

Validation:
1. dotnet build Atspm/ATSPM.sln

Deliverables:
- Implement changes
- Run validation
- Return changed files + validation summary

## Prompt 12 - Obsolete API retirement policy cleanup (P2)
You are implementing Issue 12 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Reduce obsolete/commented legacy noise and document deprecation path.

In-scope:
- Atspm/Application/Extensions/ModelExtensions.cs
- Targeted files under Atspm/Application/Repositories/ConfigurationRepositories
- Changelog/docs if needed

Out-of-scope:
- Breaking removals without approval

Constraints:
- Preserve compatibility.

Validation:
1. dotnet build Atspm/ATSPM.sln

Deliverables:
- Implement changes
- Run validation
- Return changed files + compatibility notes

## Prompt 13 - Platform docs/FAQ normalization (P2)
You are implementing Issue 13 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Align docs and FAQ seed content with current Linux/container-first operation.

In-scope:
- Atspm/README.md
- Atspm/Data/Configuration/FaqConfiguration.cs

Out-of-scope:
- Runtime logic changes

Constraints:
- Keep user-facing guidance clear and consistent.

Validation:
1. Check links/paths in updated docs
2. dotnet build Atspm/ATSPM.sln (for seeded FAQ source compilation)

Deliverables:
- Implement changes
- Run validation
- Return changed files + summary

## Prompt 14 - LTGR interface extraction (P2)
You are implementing Issue 14 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Move local interfaces from LTGR view into dedicated types file.

In-scope:
- Atspm/WebUI/src/features/leftTurnGapReport/components/LTGRReportView.tsx
- Atspm/WebUI/src/features/leftTurnGapReport (types file)

Out-of-scope:
- UI behavior changes

Constraints:
- No component behavior changes.

Validation:
1. cd Atspm/WebUI && npm run lint
2. cd Atspm/WebUI && npm test -- --watch=false

Deliverables:
- Implement changes
- Run validation
- Return changed files + summary

## Prompt 15 - Enforce CI quality gates (P1)
You are implementing Issue 15 from [docs/pre-feature-hardening-issues-ai-ready.md](docs/pre-feature-hardening-issues-ai-ready.md).

Goal:
- Enforce pre-feature quality gates in CI.

In-scope:
- .github/workflows/build.yml
- .github/workflows/deploy.yml
- Atspm/scripts/smoke-all.sh
- Atspm/README.md (workflow notes)

Out-of-scope:
- Runtime feature refactors

Constraints:
- Keep CI runtime practical.
- Favor incremental rollout if needed.

Validation:
1. Validate workflow syntax
2. Confirm required checks are configured

Deliverables:
- Implement changes
- Return changed files + gate behavior summary + risks
