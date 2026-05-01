using CoreEx.Generator.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CoreEx.Generator;

/// <summary>
/// Provides the 'ContractAttribute' <see cref="IIncrementalGenerator"/> implementation.
/// </summary>
[Generator]
public class ContractGenerator : IIncrementalGenerator
{
    private const string _contractAttributeResourceName = "CoreEx.Generator.Templates.ContractAttribute.cs.hb";
    private const string _contractIgnoreAttributeResourceName = "CoreEx.Generator.Templates.ContractIgnoreAttribute.cs.hb";
    private const string _refDataAttributeResourceName = "CoreEx.Generator.Templates.ReferenceDataAttribute.cs.hb";
    private const string _refDataTAttributeResourceName = "CoreEx.Generator.Templates.ReferenceDataTAttribute.cs.hb";
    private const string _refDataCodeCollectionTAttributeResourceName = "CoreEx.Generator.Templates.ReferenceDataCodeCollectionTAttribute.cs.hb";
    private const string _stringAttributeResourceName = "CoreEx.Generator.Templates.StringAttribute.cs.hb";
    private const string _dateTimeAttributeResourceName = "CoreEx.Generator.Templates.DateTimeAttribute.cs.hb";
    private const string _cleanAttributeResourceName = "CoreEx.Generator.Templates.CleanAttribute.cs.hb";
    private const string _templateResourceName = "CoreEx.Generator.Templates.Contract.cs.hb";
    private readonly HandlebarsCodeGenerator _codeGenerator = HandlebarsCodeGenerator.Create(_templateResourceName);

    /// <summary>
    /// Gets a <see cref="SymbolDisplayFormat"/> that includes nullability.
    /// </summary>
    internal static SymbolDisplayFormat FullyQualifiedWithNullability =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    /// <inheritdoc/>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Register the requisite '*Attribute' classes.
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddSource("contractattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_contractAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("contractignoreattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_contractIgnoreAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("referencedataattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_refDataAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("referencedatatattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_refDataTAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("referencedatacodecollectiontattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_refDataCodeCollectionTAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("stringattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_stringAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("datetimeattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_dateTimeAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
            ctx.AddSource("cleanattribute.g.cs", SourceText.From(HandlebarsCodeGenerator.Create(_cleanAttributeResourceName).Generate(new CodeGenContext()), Encoding.UTF8));
        });

        // Register the source generator for the above 'ContractAttribute' class usage.
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: "CoreEx.Entities.ContractAttribute",
            predicate: static (syntaxNode, cancellationToken) => syntaxNode is ClassDeclarationSyntax || syntaxNode is RecordDeclarationSyntax,
            transform: static (context, cancellationToken) => ContractModel.Create(context, cancellationToken)
        );

        // Register the source output to generate the resulting contract partial class contents.
        context.RegisterSourceOutput(provider, (context, model) =>
        {
            try
            {
                if (!model.ReportDiagnostics(context))
                    return; // Do not generate as there are errors.

                if (model.IContract == GenApproach.Undetermined)
                    return; // No need to generate if IContract is already declared.

                var sourceText = SourceText.From(_codeGenerator.Generate(model), Encoding.UTF8);
                context.AddSource($"{model.ClassName}.contract.g.cs", sourceText);
            }
            catch (System.Exception ex)
            {
                var descriptor = new DiagnosticDescriptor(
                    id: "CoreEx000",
                    title: "Contract generation error.",
                    messageFormat: "An error occurred while generating an 'ContractAttribute': {0}",
                    category: "CoreEx",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);

                context.ReportDiagnostic(Diagnostic.Create(descriptor, null, ex.Message));
            }
        });
    }
}