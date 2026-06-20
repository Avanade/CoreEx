using CoreEx.Generator.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CoreEx.Generator;

/// <summary>
/// Provides the 'ReferenceDataAttribute' <see cref="IIncrementalGenerator"/> implementation.
/// </summary>
[Generator]
public class ReferenceDataGenerator : IIncrementalGenerator
{
    private const string _templateResourceName = "CoreEx.Generator.Templates.ReferenceData.cs.hb";
    private readonly HandlebarsCodeGenerator _codeGenerator = HandlebarsCodeGenerator.Create(_templateResourceName);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // No RegisterPostInitializationOutput needed as handled by ContractGenerator (i.e. centralized singleton).

        // Register the source generator for the above 'ReferenceDataAttribute' class usage.
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "CoreEx.RefData.ReferenceDataAttribute",
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax || syntaxNode is RecordDeclarationSyntax,
            transform: static (context, cancellationToken) => ReferenceDataModel.Create(context, cancellationToken)
        );

        // Register the source output to generate the resulting reference data partial class contents.
        context.RegisterSourceOutput(provider, (context, model) =>
        {
            try
            {
                if (!model.ReportDiagnostics(context))
                    return; // Do not generate as there are errors.

                if (model.IReferenceData == GenApproach.Undetermined)
                    return; // No need to generate if IReferenceData is already declared.

                var sourceText = SourceText.From(_codeGenerator.Generate(model), Encoding.UTF8);
                context.AddSource($"{model.ClassName}.refdata.g.cs", sourceText);
            }
            catch (System.Exception ex)
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "CoreEx000",
                    title: "Reference data generation error.",
                    messageFormat: "An error occurred while generating a 'ReferenceDataAttribute': {0}",
                    category: "CoreEx",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, null, ex.Message));
            }
        });
    }
}