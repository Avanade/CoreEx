if (args.Length == 1)
{
    switch (args[0].ToUpperInvariant())
    {
        case "--GENERATE-JSON-SCHEMA":
            JsonSchemaGenerator.Generate<CoreEx.CodeGen.RefData.Config.CodeGenConfig>("../../schema/coreex-refdata.json", "JSON Schema for CoreEx reference-data code-generation (https://github.com/avanade/coreex).");
            JsonSchemaGenerator.Generate<CoreEx.CodeGen.Files.Config.CodeGenConfig>("../../schema/coreex-files.json", "JSON Schema for CoreEx File code-generation (https://github.com/avanade/coreex).");
            break;

        case "--GENERATE-DOC-MARKDOWN":
            MarkdownDocumentationGenerator.Generate<CoreEx.CodeGen.RefData.Config.CodeGenConfig>(null, Directory.CreateDirectory("../../src/CoreEx.CodeGen/RefData/docs").FullName);
            MarkdownDocumentationGenerator.Generate<CoreEx.CodeGen.Files.Config.CodeGenConfig>(null, Directory.CreateDirectory("../../src/CoreEx.CodeGen/Files/docs").FullName);
            break;
    }
}
