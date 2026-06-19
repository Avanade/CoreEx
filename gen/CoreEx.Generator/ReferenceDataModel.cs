using CoreEx.Generator.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CoreEx.Generator;

/// <summary>
/// Represents the <i>ReferenceDataAttribute</i> class model configuration used to drive the underlying partial class source generation.
/// </summary>
internal class ReferenceDataModel : CodeGenContext
{
    /// <summary>
    /// Gets the namespace of the contract.
    /// </summary>
    public string? Namespace { get; private set; }

    /// <summary>
    /// Gets the class name of the contract.
    /// </summary>
    public string? ClassName { get; private set; }

    /// <summary>
    /// Gets the containing type hierarchy of the contract.
    /// </summary>
    public List<string>? ContainingTypeHierarchy { get; private set; }

    /// <summary>
    /// Indicates whether the contract is a <i>record</i>; otherwise, <see langword="false"/> indicates a <i>class</i>.
    /// </summary>
    public bool IsRecord { get; private set; }

    /// <summary>
    /// Gets the <see cref="GenApproach"/> for the reference data.
    /// </summary>
    public GenApproach IReferenceData { get; private set; }

    /// <summary>
    /// Gets the list of properties for the contract.
    /// </summary>
    public List<PropertyModel> Properties { get; } = [];

    /// <summary>
    /// Gets the list of properties that are to be code-generated as declared as partial.
    /// </summary>
    public IEnumerable<PropertyModel> PartialProperties => Properties.Where(p => p.IsRefData || p.IsSelfCleanedString || p.IsSelfCleanedDateTime);

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="GeneratorAttributeSyntaxContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ContractModel"/>.</returns>
    public static ReferenceDataModel Create(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        try
        {
            var model = context.TargetNode is ClassDeclarationSyntax ? CreateForClass(context, cancellationToken) : CreateForRecord(context, cancellationToken);
            return model;
        }
        catch (System.Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx100",
                title: "Reference data generation error.",
                messageFormat: "An error occurred while generating an 'ReferenceDataAttribute': {0}",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            return new ReferenceDataModel { IReferenceData = GenApproach.Undetermined, Diagnostics = { Diagnostic.Create(descriptor, null, ex.Message) } };
        }
    }

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <see cref="ClassDeclarationSyntax"/> <paramref name="context"/>.
    /// </summary>
    private static ReferenceDataModel CreateForClass(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (ClassDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(syntax)!;

        var model = new ReferenceDataModel
        {
            Namespace = context.TargetSymbol.ContainingType is null
                ? context.TargetSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                : context.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            ContainingTypeHierarchy = context.TargetSymbol.ContainingType is null ? [] : ContractModel.GetContainingTypeHierarchy(context.TargetSymbol.ContainingType),
            ClassName = symbol.Name
        };

        return CreateForStandard(context, symbol, model, cancellationToken);
    }

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <see cref="RecordDeclarationSyntax"/> <paramref name="context"/>.
    /// </summary>
    private static ReferenceDataModel CreateForRecord(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (RecordDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(syntax)!;

        var model = new ReferenceDataModel
        {
            Namespace = context.TargetSymbol.ContainingType is null
                ? context.TargetSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                : context.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            ContainingTypeHierarchy = context.TargetSymbol.ContainingType is null ? [] : ContractModel.GetContainingTypeHierarchy(context.TargetSymbol.ContainingType),
            ClassName = symbol.Name,
            IsRecord = true
        };

        return CreateForStandard(context, symbol, model, cancellationToken);
    }

    /// <summary>
    /// Continues the create for the standardized behaviour.
    /// </summary>
    private static ReferenceDataModel CreateForStandard(GeneratorAttributeSyntaxContext context, INamedTypeSymbol symbol, ReferenceDataModel model, CancellationToken cancellationToken)
    {
        // Check the cancellation token.
        cancellationToken.ThrowIfCancellationRequested();

        // Get the symbol for IReferenceData.
        var iRefDataSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("CoreEx.RefData.Abstractions.IReferenceData");
        if (symbol.AllInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, iRefDataSymbol)) is null)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx101",
                title: "ReferenceDataAttribute is not supported.",
                messageFormat: "The ReferenceDataAttribute is not supported where the class/record does not implement CoreEx.RefData.Abstractions.IReferenceData; alternatively, consider using ContractAttribute.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Diagnostics.Add(Diagnostic.Create(descriptor, symbol.Locations.FirstOrDefault(), symbol.Name));
        }

        model.IReferenceData = GenApproach.Declare;

        // Get the list of get/set properties.
        foreach (var p in symbol.GetMembers().OfType<IPropertySymbol>().Where(p => p.GetMethod is not null))
        {
            if (p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "CoreEx.Entities.ContractIgnoreAttribute") is not null)
                continue; // Ignore properties with ContractIgnoreAttribute.

            var emp = new PropertyModel
            {
                Context = model,
                Name = p.Name,
                IsReadonly = p.SetMethod is null,
                IsInitOnly = p.SetMethod?.IsInitOnly ?? false,
                Type = ContractModel.FormatTypeWithNullability(p.Type.ToDisplayString(ContractGenerator.FullyQualifiedWithNullability), p.NullableAnnotation),
                JsonName = p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute")?.ConstructorArguments.FirstOrDefault().Value as string,
                FallbackText = ContractModel.GetDisplayAttributeName(p),
                Default = p.DeclaringSyntaxReferences.Select(ds => ds.GetSyntax()).OfType<PropertyDeclarationSyntax>().Select(ps => ContractModel.GetDefaultConstant(ps, context.SemanticModel)).FirstOrDefault(),
                Format = ContractModel.GetDisplayFormatAttributeDataFormatString(p)
            };

            if (model.IsRecord && emp.Name == "EqualityContract")
                continue;

            emp.KeyAndOrText = emp.HasFallbackText ? emp.Name : null;

            ContractModel.ManageLocalizationAttribute(p, emp);
            ContractModel.ManageStringAttributeProperty(p, emp);
            ContractModel.ManageDateTimeAttributeProperty(p, emp);
            ContractModel.ManageCleanAttributeProperty(p, emp);
            ContractModel.ManageReferenceDataAttributeProperty(p, emp);
            ContractModel.ManageReferenceDataCodeCollectionAttributeProperty(p, emp);

            model.Properties.Add(emp);
        }

        return model;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not ReferenceDataModel other)
            return false;

        if (Namespace != other.Namespace || ClassName != other.ClassName || IsRecord != other.IsRecord || IReferenceData != other.IReferenceData)
            return false;

        if (Enumerable.SequenceEqual(ContainingTypeHierarchy ?? [], other.ContainingTypeHierarchy ?? []) && Enumerable.SequenceEqual(Properties, other.Properties))
            return true;

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = (Namespace?.GetHashCode() ?? 0) ^ (ClassName?.GetHashCode() ?? 0) ^ IsRecord.GetHashCode() ^ IReferenceData.GetHashCode();
        if (ContainingTypeHierarchy is not null)
            hash ^= ContainingTypeHierarchy.Aggregate(0, (current, item) => current ^ item.GetHashCode());

        if (Properties is not null)
            hash ^= Properties.Aggregate(0, (current, item) => current ^ item.GetHashCode());

        return hash;
    }
}