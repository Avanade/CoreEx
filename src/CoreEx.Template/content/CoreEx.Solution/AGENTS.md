# app-name -- CoreEx Solution Baseline

This solution has repository/test scaffolding only. Do not infer Application/Infrastructure/Contracts, a database, messaging or a runtime host from this baseline.

## Feature Configuration

- **Solution shape:** Baseline only; record the actual deployments and shared libraries when added.
- **Package management:** Central versions in `Directory.Packages.props`; package declarations are not adopted runtime dependencies.
- **Data provider:** None configured.
- **Reference data:** Disabled.
- **Domain project:** Absent.
- **Railway-Oriented Programming:** Not selected; follow the actual capability's established style.
- **Transactional outbox:** Disabled.
- **Messaging:** None configured.
- **File tooling:** Absent; `coreex-codegen` is an optional design-time add-on.
<!-- #if test-common -->
- **Shared tests:** `tests\app-name.Test.Common\app-name.Test.Common.csproj`; reference from consuming test projects only.
<!-- #else -->
- **Shared tests:** Not scaffolded.
<!-- #endif -->

## Scaffold boundaries

Use `/coreex-scaffold` from a matching `coreex-ai` bundle to add missing hosts/tooling or retrofit repository policy. `coreex-functions --shared-core false` requires no business libraries but still follows repository package policy. `coreex-codegen` requires no database or reference data and has no runtime presence.

Record actual adopted features here and in host guidance as capabilities land. Do not treat an unused central PackageVersion as proof that a feature is enabled. Create and register Test.Common separately from adding its ProjectReference to a test project; neither registration nor the library's existence wires that reference automatically.
