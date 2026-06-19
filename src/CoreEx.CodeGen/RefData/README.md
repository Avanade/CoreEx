# CoreEx.CodeGen.RefData

> Provides the reference-data code-generation pipeline: typed configuration models, generator classes, and the embedded Handlebars templates that together produce the full reference-data layer from a `ref-data.yaml` input file.

## Overview

`CoreEx.CodeGen.RefData` contains all the moving parts of the reference-data generation workflow. The `Config` sub-namespace holds the three typed configuration models (`CodeGenConfig`, `EntityConfig`, `PropertyConfig`) that OnRamp deserialises from `ref-data.yaml` and enriches with defaults before passing to the generators. The `Generators` sub-namespace holds the three `CodeGeneratorBase` implementations that select the appropriate configuration nodes for each template pass. The `Templates` folder contains the embedded Handlebars (`.hbs`) templates, one per generated artefact type.

OnRamp drives the overall orchestration: `ref-data-script.yaml` (also embedded in the package) lists the generation steps in order — contract, controller, service, repository interface, repository, mapper — binding each step to its generator class, template, output file name pattern, and output directory. The `RootGenerator` runs once against the whole `CodeGenConfig` for the single-file artefacts (controller, service, repository interface, repository). `ContractGenerator` and `MapperGenerator` each iterate over the entity collection to produce one file per entity.

## Key capabilities

- 🗂️ **Typed configuration hierarchy**: `CodeGenConfig` → `EntityConfig` → `PropertyConfig` mirrors the YAML structure exactly; all defaults are resolved during the `PrepareAsync` phase before any template is evaluated.
- ⚙️ **Three-generator pattern**: `RootGenerator` (single-file artefacts), `ContractGenerator` (per-entity contracts), and `MapperGenerator` (per-entity mappers, with opt-out support) cleanly separate the generation concerns.
- 📐 **Embedded templates**: `Contract_cs.hbs`, `Controller_cs.hbs`, `Service_cs.hbs`, `IRepository_cs.hbs`, `Repository_cs.hbs`, `Mapper_cs.hbs` — all embedded in the package so consuming projects carry no template files.
- 🔀 **Property-level control**: `PropertyConfig` supports `^`-prefixed reference-data types, `?`-suffixed nullable types, per-property model name overrides, and per-property exclusion from contract or mapping generation.

## Key types

| Type | Description |
|------|-------------|
| **[`CodeGenConfig`](./Config/CodeGenConfig.cs)** | Root configuration model; deserialised from `ref-data.yaml` and enriched with defaults for domain name, project paths, namespace strings, `idType`, `collectionSortOrder`, `route`, `routeConvention`, and the entity collection. |
| **[`EntityConfig`](./Config/EntityConfig.cs)** | Per-entity configuration: `name`, `plural`, `text`, `idType`, `collectionSortOrder`, `route`, repository mode and parameter name, EF model name, mapper name, `excludeMapper`, and the `properties` collection. |
| **[`PropertyConfig`](./Config/PropertyConfig.cs)** | Per-property configuration: `name`, `type` (with `^`/`?` conventions), `text`, data model property name, `excludeContract`, and `excludeMapping`. |
| **[`RootGenerator`](./Generators/RootGenerator.cs)** | Selects `CodeGenConfig` itself as the generation target; used by the controller, service, repository interface, and repository templates to produce one file for the entire entity set. |
| **[`ContractGenerator`](./Generators/ContractGenerator.cs)** | Selects each `EntityConfig` in turn; produces one contract `.g.cs` file per reference-data entity. |
| **[`MapperGenerator`](./Generators/MapperGenerator.cs)** | Selects each `EntityConfig` where `excludeMapper` is not `true`; produces one mapper `.g.cs` file per entity. |

## Related namespaces

- **[`CoreEx.CodeGen`](../README.md)** - Root package: `CodeGenConsole` entry point, `CommandType` enum, and the embedded `ref-data-script.yaml` that drives this pipeline.
- **[`CoreEx.CodeGen.Counting`](../Counting/README.md)** - Companion utility for reporting generated vs. hand-authored file and line counts.