# CoreEx Consumer AI Context

This folder contains AI context files for developers building services that consume the CoreEx NuGet packages. Drop these files into your own project to give GitHub Copilot (and Claude Code) accurate, CoreEx-specific guidance.

## What's Included

| Path | Purpose |
|---|---|
| `.github/copilot-instructions.md` | Global CoreEx context applied to every Copilot interaction |

The per-capability instruction files live in the repo's root [`.github/instructions/`](../.github/instructions/) directory and are shared between framework developers and consumers — no separate consumer copies.

## Setup

### GitHub Copilot

1. Copy `consumer-instructions/.github/copilot-instructions.md` to your project's `.github/copilot-instructions.md`.
2. Copy the instruction files from the repo's `.github/instructions/` that match what you're building into your project's `.github/instructions/` folder.
3. If you want the guided greenfield scaffolding workflow in a non-template repository, copy the canonical files from the repo's [`.github/prompts/`](../.github/prompts/) and [`.github/skills/coreex-solution-scaffolder/`](../.github/skills/coreex-solution-scaffolder/) folders.

Copilot applies the global instructions to every chat interaction and injects the file-scoped instructions automatically based on which file is open. If you copied the greenfield scaffold prompt and skill, run `/coreex-scaffold` to choose the right `CoreEx.Template` commands before generating the solution.

### Claude Code

1. Follow the Copilot setup steps above.
2. Create a `CLAUDE.md` at your project root that imports the copied files:

```markdown
@.github/copilot-instructions.md
@.github/instructions/coreex-api-controllers.instructions.md
@.github/instructions/coreex-application-services.instructions.md
@.github/instructions/coreex-repositories.instructions.md
@.github/instructions/coreex-validators.instructions.md
@.github/instructions/coreex-host-setup.instructions.md
```

Add or remove `@` import lines to match which instruction files you copied. Claude Code reads `CLAUDE.md` on startup and follows the imports — no content duplication needed.

If you copied the greenfield scaffold prompt and skill, you can invoke `/coreex-scaffold` directly in Claude Code as well.

## Which Instruction Files to Copy

Copy only the files that match what your project contains. Add more as you introduce new capabilities.

| File | Copy when you have... |
|---|---|
| `coreex-api-controllers.instructions.md` | An API host with MVC controllers |
| `coreex-contracts.instructions.md` | A Contracts project with DTOs / entity types |
| `coreex-application-services.instructions.md` | An Application layer with services, validators, adapters, policies |
| `coreex-repositories.instructions.md` | An Infrastructure layer with EF Core repositories |
| `coreex-validators.instructions.md` | CoreEx validators (`Validator<T>`, `AbstractValidator<T>`) |
| `coreex-host-setup.instructions.md` | Any `Program.cs` — API, Subscribe, or Outbox Relay host |
| `coreex-event-subscribers.instructions.md` | A Subscribe host consuming Azure Service Bus messages |
| `coreex-domain.instructions.md` | A Domain layer with aggregates, entities, value objects |
| `coreex-tests.instructions.md` | Test projects using `CoreEx.UnitTesting` |
| `coreex-tooling.instructions.md` | `*.CodeGen` or `*.Database` design-time tooling projects |

## Further Reading

- [CoreEx on GitHub](https://github.com/Avanade/CoreEx)
- [Capabilities Guide](https://github.com/Avanade/CoreEx/blob/main/docs/capabilities.md) — deep capability and pattern explanations
- [Application Scaffolding Guide](https://github.com/Avanade/CoreEx/blob/main/docs/application-scaffolding-guide.md) — deciding what to build for a new service
- [Sample Reference Architecture](https://github.com/Avanade/CoreEx/tree/main/samples) — complete Contoso domains (Products / Shopping) demonstrating all patterns

## Why This Folder Exists

The repo's root [`.github/copilot-instructions.md`](../.github/copilot-instructions.md) is written for contributors working inside the CoreEx repository itself. The consumer version in this folder is a separate, intentionally different file for teams consuming CoreEx from NuGet in their own repositories.