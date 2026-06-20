# CoreEx.CodeGen

> Provides the CoreEx development-time code-generation tooling: a deterministic, schema-driven pipeline that scaffolds the full reference-data implementation — contract, controller, service, repository interface, repository, and mapper — from a single YAML configuration file.

## Overview

`CoreEx.CodeGen` is a console-executable tooling package used **during development**, not at runtime. It reads a developer-authored `ref-data.yaml` file (validated against [`schema/coreex-refdata.json`](../../schema/coreex-refdata.json)) as its sole configuration source, then produces the `.g.cs` generated files that implement the complete reference-data layer across the Contracts, Api, Application, and Infrastructure projects of a CoreEx solution.

Generation is orchestrated by [OnRamp](https://github.com/Avanade/OnRamp), which loads `ref-data-script.yaml` (embedded in the package) to discover the ordered sequence of generation steps. Each step binds a generator class to a Handlebars template and an output path; OnRamp resolves the template, evaluates it against the typed configuration model, and writes the result. Templates reside in `RefData/Templates/` and are also embedded so the package is entirely self-contained.

The package exposes `CodeGenConsole`, a thin wrapper around `OnRamp.Console.CodeGenConsole`, as its entry point. A consuming project needs only a single `Program.cs` line to wire up the generator. A secondary `Count` command is available to report generated vs. hand-authored file and line counts across the solution directories.

## Motivation

- Reference data (lookup tables) follows a well-understood, deterministic pattern: each entity needs a contract class, a controller route, a service method, a repository interface, a repository implementation, and a mapper. Writing these by hand is repetitive and error-prone.
- Centralising the pattern in code-generation ensures every entity is consistent — identical structure, naming conventions, route patterns, and EF mapping — with no room for per-entity drift.
- The YAML input and JSON schema act as a concise, reviewable declaration of the domain's reference data catalogue; the generated output is an artefact, not something developers need to maintain.
- Embedding the script and templates inside the NuGet package means consuming projects carry no additional files — only a `ref-data.yaml` and a one-line `Program.cs`.

## Key capabilities

- 📄 **Schema-validated YAML input**: `ref-data.yaml` is validated against `schema/coreex-refdata.json`; the schema covers root settings (`domain`, `idType`, `collectionSortOrder`, `route`, `routeConvention`, `repository`) and per-entity / per-property overrides.
- 🔧 **Script-driven generation**: `ref-data-script.yaml` (embedded) defines the ordered generation steps — contract, controller, service, repository interface, repository, and mapper — each bound to a generator class and a Handlebars template.
- 📐 **Handlebars templates**: six `.hbs` templates (embedded in `RefData/Templates/`) produce idiomatic CoreEx C# across all target layers; output files carry a `.g.cs` suffix to clearly distinguish generated from hand-authored code.
- 🏗️ **Multi-layer output**: a single run generates artefacts across four project directories — Contracts, Api, Application, and Infrastructure — all resolved automatically from the CodeGen project's location by convention.
- 🗺️ **EF Core mapper generation**: the `MapperGenerator` emits a typed mapper per entity, with per-property mapping directives derived from the `PropertyConfig`; entities can opt out via `excludeMapper: true`.
- 📊 **Code-count reporting**: the `Count` command walks the solution output directories and reports total vs. generated file and line counts per directory, helping track the proportion of the codebase that is generated.

## Usage

A CodeGen project is a minimal console executable that references this package. Create a project at e.g. `My.App.Sales.CodeGen`, add the project reference, then write:

**`Program.cs`**
```csharp
await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

**`ref-data.yaml`** — place alongside `Program.cs`:
```yaml
collectionSortOrder: Code
repository: EntityFramework
entities:
- name: Status
- name: Region
  properties:
  - name: CountryCode
    type: ^Country
- name: Currency
  plural: Currencies
  idType: Guid
```

Run from the project directory:
```
dotnet run
```

The generator resolves the Contracts, Api, Application, and Infrastructure project directories relative to the CodeGen project's location by convention (e.g. `../My.App.Sales.Contracts`, `../My.App.Sales.Api`, etc.), then writes `.g.cs` files into the appropriate sub-folders.

To report code counts instead of generating:
```
dotnet run -- count
```

## Schema

The [`schema/coreex-refdata.json`](../../schema/coreex-refdata.json) JSON Schema defines the structure and validation rules for `ref-data.yaml`. 
The hierarchy is as follows:

```
CodeGeneration
└── Entity(s)
  └── Property(s)
```

Configuration details for each of the above are as follows:

- [`CodeGeneration`](./docs/CodeGeneration.md)
- [`Entity`](./docs/Entity.md)
- [`Property`](./docs/Property.md)

## Key types

| Type | Description |
|------|-------------|
| **[`CodeGenConsole`](./CodeGenConsole.cs)** | Entry point for the code-generation tool; wraps `OnRamp.Console.CodeGenConsole`, loads `ref-data.yaml` and `ref-data-script.yaml`, and exposes the `RefData` and `Count` commands. |
| **[`CommandType`](./CommandType.cs)** | Enum selecting the command to execute: `RefData` (default — runs code generation) or `Count` (reports file and line statistics). |
| **[`CodeGenConfig`](./RefData/Config/CodeGenConfig.cs)** | Root configuration model for reference-data generation; maps directly to the top-level YAML and resolves all project directory paths, namespace defaults, and entity collection preparation. |
| **[`EntityConfig`](./RefData/Config/EntityConfig.cs)** | Per-entity configuration: name, plural, text, `idType`, `collectionSortOrder`, route, repository mode, model name, mapper name, `excludeMapper`, and the property collection. |
| **[`PropertyConfig`](./RefData/Config/PropertyConfig.cs)** | Per-property configuration: name, type (with `^`-prefix for reference-data and `?`-suffix for nullable), text, data model property name, and exclude flags for contract and mapping generation. |
| **[`RootGenerator`](./RefData/Generators/RootGenerator.cs)** | `CodeGeneratorBase` implementation that selects `CodeGenConfig` as its single generation target; used for the controller, service, repository interface, and repository templates. |
| **[`ContractGenerator`](./RefData/Generators/ContractGenerator.cs)** | `CodeGeneratorBase` implementation that iterates all `EntityConfig` entries to produce one contract file per entity. |
| **[`MapperGenerator`](./RefData/Generators/MapperGenerator.cs)** | `CodeGeneratorBase` implementation that iterates `EntityConfig` entries where `excludeMapper` is not `true`, producing one mapper file per entity. |
| **[`CodeGenCounter`](./Counting/CodeGenCounter.cs)** | Walks the solution output directories and counts total vs. generated files and lines; drives the `Count` command output. |
| **[`DirectoryCountStatistics`](./Counting/DirectoryCountStatistics.cs)** | Aggregates file and line counts (total and generated) for a single directory and its children; renders a formatted hierarchy table to the logger. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.CodeGen.RefData`** | Reference-data generation pipeline: typed configuration models (`Config`), generator classes (`Generators`), and the embedded Handlebars templates (`Templates`). | [📖 README](./RefData/README.md) |
| **`CoreEx.CodeGen.Counting`** | File and line counting utilities for reporting generated vs. hand-authored code proportions across solution directories. | [📖 README](./Counting/README.md) |

## Additional Resources

- [OnRamp](https://github.com/Avanade/OnRamp) — the underlying code-generation orchestration framework used to load the script, resolve templates, and manage file output.
- [CoreEx ref-data schema](../../schema/coreex-refdata.json) — JSON Schema for `ref-data.yaml`; use it with IDE YAML language-server support for validation and auto-complete while authoring configuration.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.