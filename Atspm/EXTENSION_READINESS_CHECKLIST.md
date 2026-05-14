# ATSPM Extension Readiness Checklist (Linux + Cross-Platform)

## 1. Security and config hygiene
- [ ] Remove any committed real secrets from tracked files and rotate credentials.
- [ ] Keep local runtime settings in `.env` only; keep templates in `.env.example`.
- [ ] Add secret scanning in CI (for example, Gitleaks).

## 2. Reproducible toolchains
- [ ] Pin .NET SDK with `global.json`.
- [ ] Pin Node.js version with `WebUI/.nvmrc`.
- [ ] Document required versions in onboarding docs.

## 3. Linux portability hardening
- [ ] Replace all hardcoded Windows absolute paths in tests.
- [ ] Remove backslash-only path fragments and use `Path.Combine`.
- [ ] Audit downloader and file URI logic for UNC-only assumptions.

## 4. Repository structure cleanup
- [ ] Consolidate duplicate-cased roots (`ATSPM` vs `Atspm`) into one canonical path.
- [ ] Ensure all active projects and test projects are included consistently in solution/CI.

## 5. Runtime and API extension readiness
- [ ] Define protocol abstraction contracts for remote ITS device integrations.
- [ ] Standardize retry, timeout, and backoff policies for device communication.
- [ ] Add structured telemetry for per-device protocol operations and failures.
- [ ] Define published API compatibility and versioning policy before external release.

## 6. Linux local environment bootstrap
- [ ] Install .NET 8 SDK, Node LTS, and Docker Engine + Compose plugin.
- [ ] Validate with `dotnet build`, `dotnet test`, `npm ci`, `npm run build`, and `docker compose up --build`.
- [ ] Add CI jobs that mirror Linux local commands.

## 7. Documentation refresh
- [ ] Update system requirement text to reflect modern cross-platform deployment options.
- [ ] Make one authoritative root onboarding path for Linux developers.
