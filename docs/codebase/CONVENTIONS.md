# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| Files | PascalCase filenames for C# types and test files | ProductService.cs, BasketRepository.cs, ExceptionTests.cs | samples/src/Contoso.Products.Application/ProductService.cs; samples/src/Contoso.Shopping.Infrastructure/Repositories/BasketRepository.cs; tests/CoreEx.Test.Unit/ExceptionTests.cs |
| Functions/methods | PascalCase methods; async methods usually end with Async | CreateAsync, CheckoutAsync, DeleteAsync | samples/src/Contoso.Products.Application/ProductService.cs; samples/src/Contoso.Shopping.Application/BasketService.cs |
| Types/interfaces | Types use PascalCase; interfaces use I-prefix | Basket, ProductController, IProductService, IBasketRepository | samples/src/Contoso.Shopping.Domain/Basket.cs; samples/src/Contoso.Products.Api/Controllers/ProductController.cs; samples/src/Contoso.Products.Application/Interfaces/IProductService.cs; samples/src/Contoso.Shopping.Application/Repositories/IBasketRepository.cs |
| Constants/env vars | Environment variables are uppercase or configuration-key style | TASKHUB, dts-endpoint, E2E__Products__BaseAddress | samples/src/Contoso.Order.Workflow.Worker/Program.cs; samples/README.md |

### 2) Formatting and Linting

- Formatter: .editorconfig defines spaces, 4-space indentation for .cs, and 2-space indentation for json/xml/yaml/props/csproj/sln/sql.
- Linter: [TODO] no dedicated style linter config such as StyleCop or Roslyn ruleset file was found in the inspected repo files; analyzer packages exist for the generator project, and build settings enforce warnings as errors.
- Most relevant enforced rules: Nullable enabled, ImplicitUsings enabled, LangVersion preview, TreatWarningsAsErrors true.
- Run commands: dotnet build CoreEx.sln; dotnet test CoreEx.sln.

### 3) Import and Module Conventions

- Import grouping/order: using directives sit at the top of the file and projects commonly centralize repeated imports in GlobalUsing.cs files.
- Alias vs relative import policy: standard project references and namespace imports are used; no alternate aliasing scheme was found beyond a test-only alias for ExecutionContext.
- Public exports/barrel policy: GlobalUsing.cs is used per project; [TODO] no broader documented export policy was found.

### 4) Error and Logging Conventions

- Error strategy by layer: application and domain code use CoreEx exceptions and Result/BusinessError/NotFoundError flows; API hosts apply UseCoreExExceptionHandler so exceptions map to HTTP responses.
- Logging style and required context fields: typed ILogger<T> is used where explicit logging appears, and sample host appsettings set Logging:LogLevel with Default and category overrides.
- Sensitive-data redaction rules: [TODO] no explicit redaction policy or sanitizer configuration was found in the inspected files.

### 5) Testing Conventions

- Test file naming/location rule: tests live under tests/ and samples/tests/; filenames commonly end in Tests.cs or split partial suites such as ProductMutateTests.Create.cs.
- Mocking strategy norm: UnitTestEx tester base classes, expected outbox publisher wrappers, and MockHttpClientFactory for downstream HTTP isolation are used in samples.
- Coverage expectation: coverlet.collector is referenced; [TODO] no committed coverage threshold or reporting gate was found.

### 6) Evidence

- .editorconfig
- src/Directory.Build.props
- tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj
- tests/CoreEx.Test.Unit/ExceptionTests.cs
- samples/src/Contoso.Products.Api/GlobalUsing.cs
- samples/src/Contoso.Products.Api/Controllers/ProductController.cs
- samples/src/Contoso.Products.Application/ProductService.cs
- samples/src/Contoso.Shopping.Application/BasketService.cs
- samples/src/Contoso.Order.Workflow.Worker/Program.cs
- samples/tests/Contoso.Products.Test.Api/ProductMutateTests.Create.cs
- samples/README.md
