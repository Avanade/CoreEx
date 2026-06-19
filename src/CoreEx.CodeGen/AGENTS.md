# CoreEx.CodeGen — AI Usage Guide

`CoreEx.CodeGen` is a **development-time** code generation tool — it is never deployed at runtime. It reads a `ref-data.yaml` file and generates the complete reference-data layer (contract, controller, service, repository, mapper) as `.g.cs` files.

## Setup

Create a console project (e.g. `MyApp.CodeGen`) and add a `Program.cs` with one line:

```csharp
await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

Place `ref-data.yaml` alongside `Program.cs`.

## ref-data.yaml Structure

```yaml
collectionSortOrder: Code     # default sort for all reference data collections

entities:
  - name: Status
    idType: int               # Id property type; defaults to string
    properties:
      - name: IsExternal
        type: bool
        default: false
  - name: Country
  - name: Currency
```

Run the project (`dotnet run`) to regenerate all `.g.cs` files after changing the YAML.

## Generated Outputs

| Output | Layer | What changes it |
|---|---|---|
| `Contracts/**/*.g.cs` | Contracts | `ref-data.yaml` entity/property config |
| `**/Controllers/**/*.g.cs` | Api | `ref-data.yaml` route/entity config |
| `**/Services/**/*.g.cs` | Application | `ref-data.yaml` entity config |
| `**/Repositories/**/*.g.cs` | Infrastructure | `ref-data.yaml` repository/mapper config |
| `**/Mappers/**/*.g.cs` | Infrastructure | `ref-data.yaml` property config |

## Do Not

- Do not edit `*.g.cs` files — they are overwritten on every generation run. Edit `ref-data.yaml` or the Handlebars templates in the `CoreEx.CodeGen` package instead.
- Do not add `CoreEx.CodeGen` as a runtime dependency — it is a development tool only.

## Further Reading

- [README](./README.md) — full YAML schema, script structure, and template customisation reference.
- [Tooling](../../samples/docs/tooling.md) — how `*.CodeGen` and `*.Database` projects are used together in the sample solution, including run order and generated-file ownership.
- [Contracts layer](../../samples/docs/contracts-layer.md) — shows generated reference-data contracts (`[ReferenceData]`) and how `ref-data.yaml` drives the controller/service/repository layer.
