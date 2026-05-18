using CoreEx.Generator.Utility;
using HandlebarsDotNet;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Globalization;

namespace CoreEx.Generator;

/// <summary>
/// Represents the <i>ContractAttribute</i> class model configuration used to drive the underlying partial class source generation.
/// </summary>
internal class ContractModel : CodeGenContext
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
    /// Indicates whether the contract has a base type.
    /// </summary>
    public bool HasBaseType => BaseType is not null && !BaseType.Equals("object", System.StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the base type of the contract.
    /// </summary>
    public string? BaseType { get; private set; }

    /// <summary>
    /// Gets the <see cref="GenApproach"/> for the contract.
    /// </summary>
    public GenApproach IContract { get; private set; }

    /// <summary>
    /// Gets the list of properties for the contract.
    /// </summary>
    public List<PropertyModel> Properties { get; } = [];

    /// <summary>
    /// Gets the list of properties that are to be code-generated as declared as partial.
    /// </summary>
    public IEnumerable<PropertyModel> PartialProperties => Properties.Where(p => p.IsPartial);

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="GeneratorAttributeSyntaxContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ContractModel"/>.</returns>
    public static ContractModel Create(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        try
        {
            return context.TargetNode is ClassDeclarationSyntax ? CreateForClass(context, cancellationToken) : CreateForRecord(context, cancellationToken);
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

            return new ContractModel { IContract = GenApproach.Undetermined, Diagnostics = { Diagnostic.Create(descriptor, null, ex.Message) } };
        }
    }

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <see cref="ClassDeclarationSyntax"/> <paramref name="context"/>.
    /// </summary>
    private static ContractModel CreateForClass(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (ClassDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(syntax)!;

        var model = new ContractModel
        {
            Namespace = context.TargetSymbol.ContainingType is null
                ? context.TargetSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                : context.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            ContainingTypeHierarchy = context.TargetSymbol.ContainingType is null ? [] : GetContainingTypeHierarchy(context.TargetSymbol.ContainingType),
            ClassName = symbol.Name
        };

        return CreateForStandard(context, symbol, model, cancellationToken);
    }

    /// <summary>
    /// Create the <see cref="ContractModel"/> from the <see cref="RecordDeclarationSyntax"/> <paramref name="context"/>.
    /// </summary>
    private static ContractModel CreateForRecord(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        var syntax = (RecordDeclarationSyntax)context.TargetNode;
        var symbol = (INamedTypeSymbol)context.SemanticModel.GetDeclaredSymbol(syntax)!;

        var model = new ContractModel
        {
            Namespace = context.TargetSymbol.ContainingType is null
                ? context.TargetSymbol.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))
                : context.TargetSymbol.ContainingType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)),
            ContainingTypeHierarchy = context.TargetSymbol.ContainingType is null ? [] : GetContainingTypeHierarchy(context.TargetSymbol.ContainingType),
            ClassName = symbol.Name,
            IsRecord = true
        };

        return CreateForStandard(context, symbol, model, cancellationToken);
    }

    /// <summary>
    /// Continues the create for the standardized behaviour.
    /// </summary>
    private static ContractModel CreateForStandard(GeneratorAttributeSyntaxContext context, INamedTypeSymbol symbol, ContractModel model, CancellationToken cancellationToken)
    {
        // Determine whether already implements IContract<T> where T is itself.
        var iContractSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("CoreEx.Entities.IContract`1");
        if (AlreadyImplementsIContractGenericSelf(symbol, iContractSymbol))
            return model;

        // Determine whether IContract<T> is the base/interface implementation.
        model.IContract = IsBaseInterfaceImplementation(symbol, iContractSymbol!) ? GenApproach.Declare : GenApproach.Override;
        model.BaseType = symbol.BaseType?.ToDisplayString();

        // Check the cancellation token.
        cancellationToken.ThrowIfCancellationRequested();

        // Get the symbol for IReferenceData.
        var iRefDataSymbol = context.SemanticModel.Compilation.GetTypeByMetadataName("CoreEx.RefData.Abstractions.IReferenceData");
        if (symbol.AllInterfaces.FirstOrDefault(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, iRefDataSymbol)) is not null)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx010",
                title: "ContractAttribute is not supported.",
                messageFormat: "The ContractAttribute is not supported where the class/record implements CoreEx.RefData.Abstractions.IReferenceData; alternatively, consider using the ReferenceDataAttribute.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Diagnostics.Add(Diagnostic.Create(descriptor, symbol.Locations.FirstOrDefault(), symbol.Name));
        }

        // Get the list of properties which as a minimum do a get.
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
                IsRequired = p.IsRequired,
                Type = FormatTypeWithNullability(p.Type.ToDisplayString(ContractGenerator.FullyQualifiedWithNullability), p.NullableAnnotation),
                JsonName = p.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "System.Text.Json.Serialization.JsonPropertyNameAttribute")?.ConstructorArguments.FirstOrDefault().Value as string,
                FallbackText = GetDisplayAttributeName(p),
                Default = p.DeclaringSyntaxReferences.Select(ds => ds.GetSyntax()).OfType<PropertyDeclarationSyntax>().Select(ps => GetDefaultConstant(ps, context.SemanticModel)).FirstOrDefault(),
                Format = GetDisplayFormatAttributeDataFormatString(p)
            };

            if (model.IsRecord && emp.Name == "EqualityContract")
                continue;

            emp.KeyAndOrText = emp.HasFallbackText ? emp.Name : null;

            ManageLocalizationAttribute(p, emp);
            ManageStringAttributeProperty(p, emp);
            ManageDateTimeAttributeProperty(p, emp);
            ManageCleanAttributeProperty(p, emp);
            ManageReferenceDataAttributeProperty(p, emp);
            ManageReferenceDataCodeCollectionAttributeProperty(p, emp);

            model.Properties.Add(emp);
        }

        return model;
    }

    /// <summary>
    /// Formats the type with nullability.
    /// </summary>
    /// <param name="type">The type name.</param>
    /// <param name="nullableAnnotation">The <see cref="NullableAnnotation"/>.</param>
    /// <returns>The type name with <paramref name="nullableAnnotation"/> added as required.</returns>
    internal static string FormatTypeWithNullability(string type, NullableAnnotation nullableAnnotation) => nullableAnnotation == NullableAnnotation.Annotated && !type.EndsWith("?") ? type + "?" : type;

    /// <summary>
    /// Gets the name from the <i>DisplayAttribute</i> where defined.
    /// </summary>
    internal static string? GetDisplayAttributeName(IPropertySymbol propertySymbol)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "System.ComponentModel.DataAnnotations.DisplayAttribute");
        if (att is null)
            return null;

        var na = att.NamedArguments.FirstOrDefault(na => na.Key == "Name");
        if (na.Key is not null && na.Value.Value is string name)
            return string.IsNullOrEmpty(name) ? null : name;

        return null;
    }

    /// <summary>
    /// Gets the format from the <i>DisplayFormatAttribute</i> where defined.
    /// </summary>
    internal static string? GetDisplayFormatAttributeDataFormatString(IPropertySymbol propertySymbol)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "System.ComponentModel.DataAnnotations.DisplayFormatAttribute");
        if (att is null)
            return null;

        var na = att.NamedArguments.FirstOrDefault(na => na.Key == "DataFormatString");
        if (na.Key is not null && na.Value.Value is string format)
            return string.IsNullOrEmpty(format) ? null : format;

        return null;
    }

    /// <summary>
    /// Gets the default constant value from the property syntax.
    /// </summary>
    internal static string? GetDefaultConstant(PropertyDeclarationSyntax propertySyntax, SemanticModel semanticModel)
    {
        // Check if there's an initializer
        var initializer = propertySyntax.Initializer?.Value;
        if (initializer is null)
            return null;

        // Try to get the constant value
        var constantValue = semanticModel.GetConstantValue(initializer);
        if (constantValue.HasValue)
            return initializer.ToString();

        // Where not constant then we can not reliably determine the value, so default for you!
        return initializer.ToString();
    }

    /// <summary>
    /// Determine type declaration hierarchy.
    /// </summary>
    /// <param name="type">The <see cref="INamedTypeSymbol"/>.</param>
    /// <returns>The resulting hierarchy list.</returns>
    internal static List<string> GetContainingTypeHierarchy(INamedTypeSymbol? type)
    {
        static void AddContainingTypeHierarchy(INamedTypeSymbol? type, List<string> list)
        {
            if (type is null)
                return;

            list.Insert(0, type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            AddContainingTypeHierarchy(type.ContainingType, list);
        }

        var list = new List<string>();
        AddContainingTypeHierarchy(type, list);
        return list;
    }

    /// <summary>
    /// Determines whether the <paramref name="symbol"/> already implements <c>CoreEx.Entities.IContract{T}</c> somewhere in its parent hierarchy.
    /// </summary>
    private static bool AlreadyImplementsIContractGenericSelf(INamedTypeSymbol symbol, INamedTypeSymbol? interfaceSymbol)
    {
        foreach (var iface in symbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, interfaceSymbol) && iface.TypeArguments.Length == 1 && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], symbol))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check whether this is considered the base interface implementation.
    /// </summary>
    private static bool IsBaseInterfaceImplementation(INamedTypeSymbol symbol, INamedTypeSymbol interfaceSymbol)
    {
        if (symbol.BaseType is null || symbol.SpecialType == SpecialType.System_Object)
            return true;

        if (!symbol.BaseType.Locations.Any(loc => loc.IsInSource))
            return !symbol.BaseType.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x.OriginalDefinition, interfaceSymbol));

        if (symbol.BaseType.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "CoreEx.Entities.ContractAttribute"))
            return false;

        return IsBaseInterfaceImplementation(symbol.BaseType, interfaceSymbol);
    }

    /// <summary>
    /// Determines and manages the LocalizationAttribute property configuration.
    /// </summary>
    internal static void ManageLocalizationAttribute(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.Localization.LocalizationAttribute");
        if (att is null)
            return;

        var kt = att.ConstructorArguments.Length < 1 ? null : att.ConstructorArguments[0].Value as string;
        var ft = att.ConstructorArguments.Length < 2 ? null : att.ConstructorArguments[1].Value as string;

        if (!string.IsNullOrEmpty(kt))
        {
            model.KeyAndOrText = kt;
            model.FallbackText = ft;
        }
    }

    /// <summary>
    /// Determines and manages the <see cref="string"/> property configuration.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
    /// <param name="model">The <see cref="PropertyModel"/>.</param>
    internal static void ManageStringAttributeProperty(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.Entities.StringAttribute");
        if (att is null)
            return;

        if (propertySymbol.Type.SpecialType != SpecialType.System_String)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx006",
                title: "Property type invalid.",
                messageFormat: "Property '{0}' must be declared with a type of 'string' to enable 'StringAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            return;
        }

        if (!propertySymbol.IsPartialDefinition)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx007",
                title: "Property must be partial.",
                messageFormat: "Property '{0}' must be declared as 'partial' to enable 'StringAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            return;
        }

        if (model.IsReadonly)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx008",
                title: "Property must support get and set.",
                messageFormat: "Property '{0}' must be declared with a 'get' and 'set' to enable 'StringAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
        }

        model.IsPartial = true;
        model.IsSelfCleanedString = true;
        model.StringTrim = (att.ConstructorArguments.Length < 1 ? null : GetEnumFriendlyName(att.ConstructorArguments[0])) ?? "UseDefault";
        model.StringTransform = (att.ConstructorArguments.Length < 2 ? null : GetEnumFriendlyName(att.ConstructorArguments[1])) ?? "UseDefault";
        model.StringCase = (att.ConstructorArguments.Length < 3 ? null : GetEnumFriendlyName(att.ConstructorArguments[2])) ?? "UseDefault";
    }

    /// <summary>
    /// Determines and manages the <see cref="System.DateTime"/> property configuration.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
    /// <param name="model">The <see cref="PropertyModel"/>.</param>
    internal static void ManageDateTimeAttributeProperty(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.Entities.DateTimeAttribute");
        if (att is null)
            return;

        var ds = propertySymbol.Type.ToDisplayString();
        if (ds != "System.DateTime" && ds != "System.DateTime?")
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx009",
                title: "Property type invalid.",
                messageFormat: "Property '{0}' must be declared with a type of 'DateTime' to enable 'DateTimeAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            return;
        }

        if (!propertySymbol.IsPartialDefinition)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx011",
                title: "Property must be partial.",
                messageFormat: "Property '{0}' must be declared as 'partial' to enable 'DateTimeAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            return;
        }

        if (model.IsReadonly)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx012",
                title: "Property must support get and set.",
                messageFormat: "Property '{0}' must be declared with a 'get' and 'set' to enable 'DateTimeAttribute' capabilities.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
        }

        model.IsPartial = true;
        model.IsSelfCleanedDateTime = true;
        model.DateTimeTransform = (att.ConstructorArguments.Length < 1 ? null : GetEnumFriendlyName(att.ConstructorArguments[0])) ?? "UseDefault";
    }

    /// <summary>
    /// Determines and manages the clean property configuration.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
    /// <param name="model">The <see cref="PropertyModel"/>.</param>
    internal static void ManageCleanAttributeProperty(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.Entities.CleanAttribute");
        if (att is null)
            return;

        model.IsCleanOption = true;
        model.CleanOption = GetEnumFriendlyName(att.ConstructorArguments[0]) ?? "UseDefault";
    }

    /// <summary>
    /// Gets the <see langword="enum"/> friendly name from the <see cref="TypedConstant"/>.
    /// </summary>
    private static string? GetEnumFriendlyName(TypedConstant arg)
    {
        if (arg.Kind != TypedConstantKind.Enum || arg.Value is not int ev)
            return null;

        var enumType = (INamedTypeSymbol)arg.Type!;
        var member = enumType
            .GetMembers()
            .OfType<IFieldSymbol>()
            .FirstOrDefault(f => f.HasConstantValue && (int)f.ConstantValue! == ev);

        return member?.Name;
    }

    /// <summary>
    /// Determines and manages the reference data property configuration.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
    /// <param name="model">The <see cref="PropertyModel"/>.</param>
    internal static void ManageReferenceDataAttributeProperty(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.RefData.ReferenceDataAttribute<TReferenceData>");
        if (att is null || att.AttributeClass is null || !att.AttributeClass!.IsGenericType)
            return;

        model.IsRefData = true;
        model.RefDataType = FormatTypeWithNullability(att.AttributeClass.TypeArguments.FirstOrDefault()?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!, propertySymbol.NullableAnnotation);

        if (propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "string" && propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "string?")
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx001",
                title: "Reference Data property type invalid.",
                messageFormat: "Reference Data property '{0}' must be declared with a type of 'string' to enable.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefData = false;
        }

        if (!propertySymbol.IsPartialDefinition)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx002",
                title: "Reference Data property must be partial.",
                messageFormat: "Reference Data property '{0}' must be declared as 'partial' to enable.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefData = false;
        }

        if (!(propertySymbol.Name.Length >= 4 && propertySymbol.Name.EndsWith("Sid", System.StringComparison.OrdinalIgnoreCase))
        && !(propertySymbol.Name.Length >= 5 && propertySymbol.Name.EndsWith("Code", System.StringComparison.OrdinalIgnoreCase)))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx004",
                title: "Reference Data property name invalid.",
                messageFormat: "Reference Data property '{0}' must be declared by convention with a name that ends with 'Sid' (Serializer Identifier) or 'Code'.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefData = false;
        }

        if (propertySymbol.DeclaredAccessibility != Accessibility.Public || (propertySymbol.GetMethod?.DeclaredAccessibility ?? Accessibility.Public) != Accessibility.Public || (propertySymbol.SetMethod?.DeclaredAccessibility ?? Accessibility.Public) != Accessibility.Public)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx005",
                title: "Reference Data property accessibility invalid.",
                messageFormat: "Reference data property '{0}' must be declared with 'public' accessibility only.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefData = false;
        }

        if (!model.IsRefData)
            return;

        model.IsPartial = true;
        model.IsRefDataJson = !propertySymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString()?.StartsWith("System.Text.Json.Serialization.Json") ?? false);

        // Camelcase the property name for JSON serialization.
        model.JsonName ??= model.RefDataName!.Length == 1 ? model.RefDataName.ToLowerInvariant() : char.ToLower(model.RefDataName[0], CultureInfo.InvariantCulture) + model.RefDataName.Substring(1);
    }

    /// <summary>
    /// Determines and manages the reference data code collection property configuration.
    /// </summary>
    /// <param name="propertySymbol">The <see cref="IPropertySymbol"/>.</param>
    /// <param name="model">The <see cref="PropertyModel"/>.</param>
    internal static void ManageReferenceDataCodeCollectionAttributeProperty(IPropertySymbol propertySymbol, PropertyModel model)
    {
        var att = propertySymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.OriginalDefinition.ToDisplayString() == "CoreEx.RefData.ReferenceDataCodeCollectionAttribute<TReferenceData>");
        if (att is null || att.AttributeClass is null || !att.AttributeClass!.IsGenericType)
            return;

        model.IsPartial = true;
        model.IsRefDataCodeCollection = true;
        model.RefDataType = att.AttributeClass.TypeArguments.FirstOrDefault()?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)!;

        if (propertySymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != "global::System.Collections.Generic.List<string>")
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx002",
                title: "Reference Data property type invalid.",
                messageFormat: "Reference Data code collection property '{0}' must be declared with a type of 'List<string>'/'List<string?>' to enable.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefDataCodeCollection = false;
        }

        if (!propertySymbol.IsPartialDefinition)
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx002",
                title: "Reference Data property must be partial.",
                messageFormat: "Reference Data code collection property '{0}' must be declared as 'partial' to enable.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefDataCodeCollection = false;
        }

        if (!(propertySymbol.Name.Length >= 5 && propertySymbol.Name.EndsWith("Sids", System.StringComparison.OrdinalIgnoreCase))
        && !(propertySymbol.Name.Length >= 6 && propertySymbol.Name.EndsWith("Codes", System.StringComparison.OrdinalIgnoreCase)))
        {
            var descriptor = new DiagnosticDescriptor(
                id: "CoreEx004",
                title: "Reference Data property name invalid.",
                messageFormat: "Reference Data code collection property '{0}' must be declared by convention with a name that ends with 'Sids' (Serializer Identifiers) or 'Codes'.",
                category: "CoreEx",
                defaultSeverity: DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            model.Context!.Diagnostics.Add(Diagnostic.Create(descriptor, propertySymbol.Locations.FirstOrDefault(), propertySymbol.Name));
            model.IsRefDataCodeCollection = false;
        }
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not ContractModel other)
            return false;

        if (Namespace != other.Namespace 
            || ClassName != other.ClassName 
            || IsRecord != other.IsRecord 
            || IContract != other.IContract 
            || BaseType != other.BaseType)
            return false;

        if (Enumerable.SequenceEqual(ContainingTypeHierarchy ?? [], other.ContainingTypeHierarchy ?? []) && Enumerable.SequenceEqual(Properties, other.Properties))
            return true;

        return false;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = (Namespace?.GetHashCode() ?? 0) 
            ^ (ClassName?.GetHashCode() ?? 0) 
            ^ IsRecord.GetHashCode() 
            ^ IContract.GetHashCode() 
            ^ (BaseType?.GetHashCode() ?? 0);

        if (ContainingTypeHierarchy is not null)
            hash ^= ContainingTypeHierarchy.Aggregate(0, (current, item) => current ^ item.GetHashCode());

        if (Properties is not null)
            hash ^= Properties.Aggregate(0, (current, item) => current ^ item.GetHashCode());

        return hash;
    }
}