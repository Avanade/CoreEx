# app-name.CodeGen

This is a non-packable, design-time CoreEx.CodeGen console. It does not reference or deploy with a runtime host. Package versions follow the repository's effective central/inline policy.

Use `/coreex-file` for checked-in top-level `*-file.yaml` definitions. Their `runtimeProjectPath` targets an existing project referencing CoreEx.IO and CoreEx.Validation; it does not require a database, reference data or new business-layer projects. Generation owns `.g.cs` output; handwritten handler starters are create-only and must be preserved.

Run commands from this project directory, because `dotnet run --project` does not change the working directory used for definition discovery:

```powershell
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
```

The console's unchanged default command is RefData and requires an authored `ref-data.yaml`. No dummy definition is supplied. Do not run bare `dotnet run` expecting File generation, and do not claim an empty console proves a file integration.
