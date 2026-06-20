using OnRamp.Utility;
using CoreEx.CodeGen.RefData.Config;

if (args.Length == 1)
{
    switch (args[0].ToUpperInvariant())
    {
        case "--GENERATE-JSON-SCHEMA":
            JsonSchemaGenerator.Generate<CodeGenConfig>("../../schema/coreex-refdata.json", "JSON Schema for CoreEx code-generation (https://github.com/avanade/coreex).");
            break;

        case "--GENERATE-DOC-MARKDOWN":
            MarkdownDocumentationGenerator.Generate<CodeGenConfig>(null, "../../src/CoreEx.CodeGen/docs");
            break;
    }
}