# app-name

This is a CoreEx solution baseline, not a prescribed business-layer architecture. It supplies a solution file, central NuGet versions, build defaults and repository conventions. Add only the hosts and tooling the workload needs.

<!-- #if test-common -->
`tests\app-name.Test.Common` is a non-packable shared test-support library for authored helpers and embedded resources. It contains no test suite, database provisioning or broker configuration. Test projects reference it; runtime projects do not.
<!-- #endif -->

## Adding a standalone Functions deployment

Use `/coreex-scaffold` from the installed version-pinned `coreex-ai` bundle. It generates a missing Functions host/test pair, registers them in `app-name.slnx` under `hosts`/`tests`, and wires the discovered Test.Common project when present. `shared-core` controls business-library references, not central package management.

Use `/coreex-functions` for actual triggers and tests. For generated CSV integrations, add the optional `coreex-codegen` tool and use `/coreex-file`; a database or reference-data scaffold is not required.

## Existing repositories

Do not force this template over existing root files or regenerate a populated host. `/coreex-scaffold` stages the baseline separately, merges only missing or explicitly reviewed root artefacts, registers existing projects, adds test-to-common references and migrates inline package versions without upgrading them. A plain `dotnet new coreex-solution` does not migrate existing project files.

`coreex` remains the full shared-domain scaffold and already supplies its root baseline. Do not apply both root-emitting templates to the same solution.
