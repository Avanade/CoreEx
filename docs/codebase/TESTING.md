# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

- Primary test framework: NUnit 4.3.2.
- Assertion/mocking tools: AwesomeAssertions, UnitTestEx.NUnit, coverlet.collector, and sample-specific mock helpers such as MockHttpClientFactory described in samples/README.md.
- Commands:

```bash
dotnet test CoreEx.sln
dotnet test tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj
dotnet test samples/tests/Contoso.Products.Test.Api/Contoso.Products.Test.Api.csproj
[TODO] no dedicated committed coverage command beyond standard dotnet test with coverlet.collector references was found.
```

### 2) Test Layout

- Test file placement pattern: reusable library tests live under tests/; sample tests live under samples/tests/ with separate projects for unit, API, relay, subscriber, common test data, and E2E runner.
- Naming convention: project names end in .Test.Unit, .Test.Api, .Test.Subscribe, .Test.Relay, or .Test.Common; files end in Tests.cs or split a suite into partial files like ProductMutateTests.Create.cs.
- Setup files and where they run: sample integration projects copy appsettings.unittest.json, embed Resources/**/*, and keep shared YAML test data in *.Test.Common projects; sample README describes OneTimeSetUp database migration, cache clearing, event capture, and HTTP client replacement.

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|----------------|-------|
| Unit | yes | CoreEx library primitives and sample validators/domain behavior | tests/CoreEx.Test.Unit and Contoso.Products.Test.Unit are present |
| Integration | yes | Sample APIs, subscriber hosts, and relay hosts | Contoso.Products.Test.Api, Contoso.Shopping.Test.Api, Contoso.Products.Test.Subscribe, and Contoso.Products.Test.Relay are present |
| E2E | yes | Cross-service sample scenarios against running APIs | Contoso.E2E.Runner is an interactive console runner |

### 4) Mocking and Isolation Strategy

- Main mocking approach: UnitTestEx tester base classes drive HTTP and generic tests; sample tests wrap outbox/service bus publishers and replace downstream HTTP clients with mocks.
- Isolation guarantees: sample integration setup migrates and reseeds SQL data, clears FusionCache/Redis state, and captures emitted events before assertions.
- Common failure mode in tests: [TODO] no committed flaky-test catalog or failure analysis file was found.

### 5) Coverage and Quality Signals

- Coverage tool + threshold: coverlet.collector is referenced; [TODO] no threshold was found.
- Current reported coverage: [TODO] no committed coverage report or badge was found.
- Known gaps/flaky areas: [TODO] none were explicitly documented in the inspected files.

### 6) Evidence

- Directory.Packages.props
- tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj
- tests/CoreEx.Test.Unit/ExceptionTests.cs
- samples/tests/Contoso.Products.Test.Api/Contoso.Products.Test.Api.csproj
- samples/tests/Contoso.Products.Test.Api/ProductMutateTests.Create.cs
- samples/tests/Contoso.E2E.Runner/Contoso.E2E.Runner.csproj
- samples/tests/Contoso.E2E.Runner/appsettings.json
- samples/README.md
