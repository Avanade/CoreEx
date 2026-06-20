---
applyTo: "**/*.instructions.md"
description: "Standards for authoring and maintaining Copilot instruction files in this repository"
tags: ["authoring", "standards", "instructions", "documentation"]
---

# Instruction File Authoring Standards

Instruction files (`.instructions.md`) are **context-window injections for a code model**. When Copilot generates or edits a file that matches the `applyTo` glob, the instruction file is automatically prepended to the context. This has two practical consequences that drive every rule below:

- **Every token costs.** Instruction file content displaces actual code context. Brevity and precision matter.
- **Code examples outperform prose rules.** Copilot is a code model. Showing it the exact pattern to follow produces better results than listing `MUST`/`MUST NOT` directives.

Write instruction files as you would a concise internal coding guide for a capable new team member, not as a policy document.

---

## Principles

- **Show, don't tell.** A real, working code example is worth more than five imperative rules.
- **Be specific.** Use actual type names, package names, and method names from this codebase.
- **Be brief.** If content cannot fit on one screen, split into a separate file or move detail to `Further Reading`.
- **Do not restate global rules.** If `.github/copilot-instructions.md` already covers it, do not repeat it here.
- **Explicit negation matters.** Copilot's training data contains many common patterns that are wrong for this repo. State anti-patterns explicitly with a `## Do Not` section.
- **Scope tightly.** An instruction that is always injected regardless of context wastes tokens. Use the narrowest `applyTo` glob that is still correct.
- **Never direct Copilot toward generated files.** Instruction files must never include guidance, examples, or `applyTo` globs that would cause Copilot to create or modify `*.g.cs`, `*.g.sql`, `*.g.pgsql`, or any other generated-output file. All generated files are owned exclusively by their corresponding tooling (Roslyn source generator, `*.Database` project, `*.CodeGen` project). Changes must be made to the source templates or generation configuration, not to the output. See [Generated Code](#generated-code) below.

---

## Required Format

```markdown
---
applyTo: "<narrowest correct glob>"
description: "<one concrete sentence describing what this file governs>"
tags: ["tag1", "tag2"]
---

# <Layer/Area> Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.X` | `TypeA`, `TypeB`, `TypeC` |

## <Pattern or topic>

<One or two sentences stating the rule, followed immediately by a real code example.>

```csharp
// real example matching what the codebase actually looks like
```

## <Next pattern or topic>

...

## Do Not

- Do not use `SomeWrongType` — use `CorrectType` instead.
- Do not call `SomeAntiPattern()` directly; delegate to the application service.

## Further Reading

- [`samples/docs/<layer>.md`](../../samples/docs/<layer>.md) — layer-level walkthrough with sample code references.
- [`src/CoreEx.X/README.md`](../../src/CoreEx.X/README.md) — full API reference for the primary package.
```

---

## Frontmatter Rules

- Field order: `applyTo` → `description` → `tags`.
- `applyTo` must be the narrowest glob that correctly captures all relevant files. Examples:
  - `**/Contracts/**/*.cs` — contract DTOs
  - `**/Application/**/*.cs` — application services
  - `**/Infrastructure/**/*.cs` — repositories and adapters
  - `**/Controllers/**/*.cs` — API controllers
  - `**/Subscribe/**/*.cs` — event subscriber classes
  - `**/Program.cs` — host entry points
  - `**/*Validator*.cs` — validator classes
  - `**/*.Test*/**/*.cs` — test projects
  - `**/*.Database/**/*.sql` — database project SQL scripts
- `description` must be one sentence, concrete, and specific to the area governed.
- `tags` must reflect the instruction's domain (e.g., `["validation", "application-layer", "unit-of-work"]`).
- Do not use `applyTo: "**"` unless the file is genuinely repository-wide.

---

## Section Guidance

### `## NuGet / Project References` — required, always first

List every package the generated code will need, with the key types Copilot should use from each. This is the highest-value, lowest-token section: it anchors imports and prevents Copilot from inventing types.

```markdown
| Package | Key types provided |
|---|---|
| `CoreEx` | `[ScopedService<T>]`, `IUnitOfWork`, `NotFoundException`, `.ThrowIfNull()` |
| `CoreEx.Validation` | `Validator<T, TSelf>`, `.ValidateAndThrowAsync()` |
```

### Pattern sections — one section per distinct concept

Name sections after the actual coding concept, not a rule number. Each section should contain one or two orienting sentences followed by a real code example. Avoid long prose; if a concept needs a paragraph of explanation, it belongs in `Further Reading`.

### `## Do Not` — required when anti-patterns exist

List things Copilot must not generate for this area. Be specific: name the wrong type, wrong library, or wrong pattern, and say what to use instead. This section counteracts Copilot's training data for common-but-wrong patterns.

```markdown
## Do Not

- Do not use `AutoMapper` — use explicit mapping helpers or classes.
- Do not inject `IUnitOfWork` into controllers — delegate to the application service.
- Do not inherit from `Controller` — inherit from `ControllerBase`.
```

### `## Further Reading` — required

Link to the layer-level `samples/docs/` walkthrough and any directly relevant `src/*/README.md` files. Copilot will fetch these when it needs deeper context, keeping the instruction file itself lean.

```markdown
## Further Reading

- [`samples/docs/application-layer.md`](../../samples/docs/application-layer.md) — application service patterns with sample code.
- [`src/CoreEx/Results/README.md`](../../src/CoreEx/Results/README.md) — `Result<T>` pipeline API reference.
```

---

## Conflict Resolution

- `.github/copilot-instructions.md` defines repository-wide defaults. Never restate them in a scoped file.
- When two instruction files could apply to the same file, the narrower `applyTo` glob wins.
- If a new file would duplicate policy from an existing one, extend the existing file instead.

## Non-Instruction Files in `.github/instructions/`

Not every file in this folder is an instruction file. `namespace_readme_template.md` is a copy/paste template used by skills and prompts — it has no `applyTo` frontmatter and is never auto-injected by Copilot. Do not add `applyTo` to it or restructure it to follow the instruction file format.

---

## Generated Code

Generated files are owned exclusively by their tooling and must never be touched by Copilot or by hand. There are three distinct generators in this repo, each with its own input and output:

### Roslyn source generator (`CoreEx.Generator`)

Triggered at compile time. Reads classes decorated with `[Contract]` or `[ReferenceData]` and emits companion `.g.cs` files in the same project.

| Output pattern | What to change instead |
|---|---|
| `*.g.cs` alongside a `[Contract]` class | The decorated partial class itself (add/remove properties, change attributes) |
| `*.g.cs` alongside a `[ReferenceData]` class | The decorated partial class itself |

### CoreEx.CodeGen (`*.CodeGen` project)

A development-time console tool. Reads a single `ref-data.yaml` file (validated against `schema/coreex-refdata.json`) and generates a complete reference-data implementation — contract, controller, service, repository interface, repository, and mapper — across four project directories in one run.

| Output pattern | Target layer | What to change instead |
|---|---|---|
| `Contracts/**/*.g.cs` | Contracts | `ref-data.yaml` entity/property config |
| `**/Controllers/**/*.g.cs` | Api | `ref-data.yaml` route/entity config |
| `**/Services/**/*.g.cs` | Application | `ref-data.yaml` entity config |
| `**/Repositories/**/*.g.cs` | Infrastructure | `ref-data.yaml` repository/mapper config |
| `**/Mappers/**/*.g.cs` | Infrastructure | `ref-data.yaml` property config or `excludeMapper: true` |

The entry point is a one-line `Program.cs` in the `*.CodeGen` project. Templates live in `CoreEx.CodeGen/RefData/Templates/` (embedded in the NuGet package) and are the only place structural changes to the generated shape belong.

### DbEx (`*.Database` project)

A development-time migration and generation tool. Reads YAML configuration and SQL migration scripts to produce database schema, outbox infrastructure, and EF Core scaffolding.

| Output pattern | What to change instead |
|---|---|
| `*.g.sql`, `*.g.pgsql` | The YAML configuration or SQL migration scripts in the `*.Database` project |
| `*DbContext.g.cs`, `Persistence/*.g.cs` | The DbEx YAML config in the `*.Database` project |

### Rules for instruction file authors

- Do not write guidance that instructs Copilot to create or modify any `*.g.*` file.
- Do not include `*.g.*` files in `applyTo` globs — they must never match a generated output.
- When showing examples that reference generated types (e.g. a persistence model or a ref-data controller), show the YAML or source class that drives generation, not the generated output.
- If a user asks Copilot to edit a generated file, identify which generator owns it from the table above and redirect to the correct input source.

---

## Worked Example

A well-formed instruction file for API controllers:

```markdown
---
applyTo: "**/Controllers/**/*.cs"
description: "API controller conventions for CoreEx: inheritance, routing, WebApi helper usage, and CQRS separation"
tags: ["controllers", "api", "routing", "dependency-injection"]
---

# API Controller Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.AspNetCore` | `WebApi`, `[IdempotencyKey]`, `[ProducesNotFoundProblem]`, `[Query]`, `[Paging]` |
| `CoreEx.AspNetCore.NSwag` | `[OpenApiTag]` |

## Structure

Inherit from `ControllerBase`. Decorate with `[ApiController]`, `[Route]`, and `[OpenApiTag]`. Inject `WebApi`
and the relevant service interface via primary constructor, guarded with `.ThrowIfNull()`. Split read and
write operations into separate controller classes following CQRS conventions.

```csharp
[ApiController, Route("/api/products"), OpenApiTag("Products")]
public class ProductController(WebApi webApi, IProductService service) : ControllerBase
{
    private readonly WebApi _webApi = webApi.ThrowIfNull();
    private readonly IProductService _service = service.ThrowIfNull();
}
```

## Action Methods

Return `Task<IActionResult>` via the `WebApi` helper. Do not return typed `ActionResult<T>` directly.

```csharp
[HttpPost, IdempotencyKey]
public Task<IActionResult> CreateAsync([FromBody] Product product) =>
    _webApi.PostAsync(Request, () => _service.CreateAsync(product), statusCode: HttpStatusCode.Created);
```

## Do Not

- Do not inherit from `Controller` — that pulls in View support; use `ControllerBase`.
- Do not return `ActionResult<T>` directly — use the `WebApi` helper for consistent error translation.
- Do not inject `IUnitOfWork` into controllers — it belongs in the application service.
- Do not put business logic in controllers — delegate immediately to the application service.

## Further Reading

- [`samples/docs/hosts-layer.md`](../../samples/docs/hosts-layer.md) — API host composition and controller patterns.
- [`src/CoreEx.AspNetCore/README.md`](../../src/CoreEx.AspNetCore/README.md) — `WebApi` helper API reference.
```
