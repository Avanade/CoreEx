namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares a scalar or nested wire occurrence, source member, and pure validation.</summary>
[CodeGenClass("Property", Title = "File record property.")]
[CodeGenCategory("Primary", Title = "Source field and generated property identity.")]
[CodeGenCategory("Csv", Title = "CSV selector and conversion.")]
[CodeGenCategory("Mapping", Title = "Typed value conversion.")]
[CodeGenCategory("Validation", Title = "Pure typed validation.")]
public class PropertyConfig : ConfigBase<CodeGenConfig, RecordConfig>
{
    /// <summary>Gets or sets the exact source field name.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Source field name.", IsMandatory = true, Description = "Exact JSON direct member or CSV column name; logical label for positional CSV fields.")]
    public string? Name { get; set; }

    /// <summary>Gets or sets the exact generated C# property name override.</summary>
    [JsonPropertyName("property")]
    [CodeGenProperty("Primary", Title = "Generated C# property override.", Description = "Defaults to DbEx-style PascalCase derivation from name.")]
    public string? Property { get; set; }

    /// <summary>Gets the resolved generated C# property name.</summary>
    public string PropertyName { get; private set; } = null!;

    /// <summary>Gets or sets the supported logical type.</summary>
    [JsonPropertyName("type")]
    [CodeGenProperty("Primary", Title = "Logical type.", Options = ["String", "Integer", "Int64", "Decimal", "Double", "Boolean", "Guid", "DateOnly", "DateTime", "DateTimeOffset"], Description = "Defaults to String only without record/collection. Native JSON token types are required; all wire properties are nullable.")]
    public string? Type { get; set; }

    /// <summary>Gets or sets the nested object record reference.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Primary", Title = "Nested object record.", Description = "JSONL only; mutually exclusive with type/collection. Required rejects missing/null; present objects always validate.")]
    public string? Record { get; set; }

    /// <summary>Gets or sets the nested collection occurrence.</summary>
    [JsonPropertyName("collection")]
    [CodeGenProperty("Primary", Title = "Nested collection occurrence.", Description = "JSONL only; mutually exclusive with type/record. Null elements are not supported.")]
    public CollectionConfig? Collection { get; set; }

    /// <summary>Gets or sets CSV-specific positional selection.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Csv", Title = "CSV property settings.")]
    public CsvPropertyConfig? Csv { get; set; }

    /// <summary>Gets or sets an exact invariant date format.</summary>
    [JsonPropertyName("format")]
    [CodeGenProperty("Mapping", Title = "Exact invariant date format.", Description = "Optional for CSV and JSON dates; when omitted, uses standard invariant parsing. DateTimeOffset always requires an explicit Z or numeric offset; JSON DateTime rejects offsets. Also applies to scalar collection elements and date bounds/options.")]
    public string? Format { get; set; }

    /// <summary>Gets or sets whether null is rejected, preserving valid zero/false values.</summary>
    [JsonPropertyName("required")]
    [CodeGenProperty("Validation", Title = "Reject null.", Description = "Defaults to false. Uses NotNull, not Mandatory, for value types.")]
    public bool? Required { get; set; }

    /// <summary>Gets or sets a string length bound.</summary>
    [JsonPropertyName("maxLength")]
    [CodeGenProperty("Validation", Title = "Maximum string length.", Description = "Nonnegative; String properties only. Zero permits only empty strings or null.")]
    public int? MaxLength { get; set; }

    /// <summary>Gets or sets whether null and default values are rejected.</summary>
    [JsonPropertyName("mandatory")]
    [CodeGenProperty("Validation", Title = "Reject null and default values.", Description = "Defaults to false. Unlike required, rejects zero and false.")]
    public bool? Mandatory { get; set; }

    /// <summary>Gets or sets the minimum string length.</summary>
    [JsonPropertyName("minLength")]
    [CodeGenProperty("Validation", Title = "Minimum string length.", Description = "Nonnegative; String only.")]
    public int? MinLength { get; set; }

    /// <summary>Gets or sets the .NET regular expression.</summary>
    [JsonPropertyName("regex")]
    [CodeGenProperty("Validation", Title = "Regular expression.", Description = "String only; generated cached regex has a one-second timeout.")]
    public string? Regex { get; set; }

    /// <summary>Gets or sets whether email validation is enabled.</summary>
    [JsonPropertyName("email")]
    [CodeGenProperty("Validation", Title = "Email validation.", Description = "String only.")]
    public bool? Email { get; set; }

    /// <summary>Gets or sets the inclusive minimum value.</summary>
    [JsonPropertyName("min")]
    [CodeGenProperty("Validation", Title = "Inclusive minimum.", JsonTypes = [CodeGenJsonType.String, CodeGenJsonType.Number], Description = "String, numeric or date value; dates use the property format. String ordering is culture-sensitive.")]
    public JsonElement Min { get; set; }

    /// <summary>Gets or sets the inclusive maximum value.</summary>
    [JsonPropertyName("max")]
    [CodeGenProperty("Validation", Title = "Inclusive maximum.", JsonTypes = [CodeGenJsonType.String, CodeGenJsonType.Number], Description = "String, numeric or date value; dates use the property format.")]
    public JsonElement Max { get; set; }

    /// <summary>Gets or sets the maximum decimal digit count.</summary>
    [JsonPropertyName("precision")]
    [CodeGenProperty("Validation", Title = "Decimal precision.", Description = "Decimal only; from 1 through 29. No rounding.")]
    public int? Precision { get; set; }

    /// <summary>Gets or sets the maximum decimal places.</summary>
    [JsonPropertyName("scale")]
    [CodeGenProperty("Validation", Title = "Decimal scale.", Description = "Decimal only; from 0 through 28, not greater than precision.")]
    public int? Scale { get; set; }

    /// <summary>Gets or sets allowed typed values.</summary>
    [JsonPropertyName("options")]
    [CodeGenPropertyCollection("Validation", Title = "Allowed values.", ItemJsonTypes = [CodeGenJsonType.String, CodeGenJsonType.Number, CodeGenJsonType.Boolean], Description = "Nonempty typed scalars; dates use the property format. Values are not canonicalized.")]
    public List<JsonElement>? Options { get; set; }

    /// <summary>Gets or sets typed reference-data validation for a string code.</summary>
    [JsonPropertyName("referenceData")]
    [CodeGenProperty("Validation", Title = "Reference-data code validation.", Description = "Uses the existing CoreEx ReferenceData rule without converting the code to an object. The caller must initialize the ambient reference-data orchestrator and load this collection before processing.")]
    public ReferenceDataConfig? ReferenceData { get; set; }

    /// <summary>Gets whether this property has any generated validation rules.</summary>
    public bool HasValidation => RuleExpressions.Any();

    /// <summary>Gets the safely escaped regular expression literal, including an empty pattern.</summary>
    public string? RegexLiteral => Regex is null ? null : OnRamp.Utility.CSharpSource.StringLiteral(Regex);

    /// <summary>Gets the fluent validation expressions in execution order.</summary>
    public IEnumerable<string> RuleExpressions
    {
        get
        {
            if (Mandatory == true) yield return "Mandatory()";
            else if (Required == true || Collection?.Cardinality is "One" or "OneOrMore") yield return "NotNull()";
            if (MinLength.HasValue) yield return $"MinimumLength({MinLength.Value})";
            if (MaxLength == 0) yield return "CompareValues(new string[] { \"\" })";
            else if (MaxLength.HasValue) yield return $"MaximumLength({MaxLength.Value})";
            if (Regex is not null) yield return $"Matches({PropertyName}Regex())";
            if (Email == true) yield return "Email()";
            if (HasMin && HasMax) yield return $"InclusiveBetween({ScalarLiteral(ParseScalar(Min, nameof(Min)))}, {ScalarLiteral(ParseScalar(Max, nameof(Max)))})";
            else if (HasMin) yield return $"GreaterThanOrEqualTo({ScalarLiteral(ParseScalar(Min, nameof(Min)))})";
            else if (HasMax) yield return $"LessThanOrEqualTo({ScalarLiteral(ParseScalar(Max, nameof(Max)))})";
            if (Precision.HasValue || Scale.HasValue) yield return $"Decimal({Precision?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}, {Scale?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"})";
            if (Options is not null) yield return $"CompareValues(new {DotNetType.TrimEnd('?')}[] {{ {string.Join(", ", Options.Select(x => ScalarLiteral(ParseScalar(x, nameof(Options)))))} }})";
            if (ReferenceData is not null) yield return $"ReferenceData(r => r.With<global::{ReferenceData.Type}>(){(ReferenceData.AllowInactive == true ? ".AllowInactive()" : string.Empty)})";
            if (Record is not null) yield return $"Entity(v => v.WithValidator(new {Root!.Name}{Record}Validator()))";
            if (Collection is not null)
            {
                var minimum = Collection.Cardinality is "One" or "OneOrMore" ? 1 : 0;
                var maximum = Collection.Cardinality is "One" or "Optional" ? Math.Min(Collection.MaxItems ?? 1, 1) : Collection.MaxItems;
                var with = Collection.Record is null ? string.Empty : $", with: v => v.WithItemValidator(new {Root!.Name}{Collection.Record}Validator())";
                yield return $"Collection(minCount: {minimum}, maxCount: {maximum?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null"}{with})";
            }
        }
    }

    /// <summary>Gets whether a minimum was supplied.</summary>
    private bool HasMin => Min.ValueKind != JsonValueKind.Undefined;

    /// <summary>Gets whether a maximum was supplied.</summary>
    private bool HasMax => Max.ValueKind != JsonValueKind.Undefined;

    /// <summary>Parses a configuration scalar without losing integer or decimal precision.</summary>
    private object ParseScalar(JsonElement value, string property)
    {
        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var culture = System.Globalization.CultureInfo.InvariantCulture;
        return Type switch
        {
            "String" when text is not null => text,
            "Integer" when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var n) => n,
            "Int64" when value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var n) => n,
            "Decimal" when value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var n) => n,
            "Decimal" when text is not null && decimal.TryParse(text, System.Globalization.NumberStyles.Float, culture, out var n) => n,
            "Double" when value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var n) && double.IsFinite(n) => n,
            "Double" when text is not null && double.TryParse(text, System.Globalization.NumberStyles.Float, culture, out var n) && double.IsFinite(n) => n,
            "Boolean" when value.ValueKind is JsonValueKind.True or JsonValueKind.False => value.GetBoolean(),
            "Guid" when text is not null && Guid.TryParse(text, out var n) => n,
            "DateOnly" when text is not null && (Format is null
                ? DateOnly.TryParse(text, culture, System.Globalization.DateTimeStyles.None, out var n)
                : DateOnly.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.None, out n)) => n,
            "DateTime" when text is not null && (!Root!.IsJson || !HasOffset(text)) && (Format is null
                ? DateTime.TryParse(text, culture, System.Globalization.DateTimeStyles.None, out var n)
                : DateTime.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.None, out n))
                && (!Root.IsJson || n.Kind == DateTimeKind.Unspecified) => n,
            "DateTimeOffset" when text is not null && HasOffset(text) && (Format is null
                ? DateTimeOffset.TryParse(text, culture, System.Globalization.DateTimeStyles.None, out var n)
                : DateTimeOffset.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.AssumeUniversal, out n)) => n,
            _ => throw new CodeGenException(this, property, $"Expected a non-null {Type} scalar matching the property's type and format.")
        };
    }

    /// <summary>Renders a parsed scalar as a culture-independent C# expression.</summary>
    private static string ScalarLiteral(object value) => value switch
    {
        string text => OnRamp.Utility.CSharpSource.StringLiteral(text),
        int n => n.ToString(System.Globalization.CultureInfo.InvariantCulture),
        long n => n.ToString(System.Globalization.CultureInfo.InvariantCulture) + "L",
        decimal n => n.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m",
        double n => n.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "d",
        bool n => n ? "true" : "false",
        Guid n => $"new Guid({OnRamp.Utility.CSharpSource.StringLiteral(n.ToString("D"))})",
        DateOnly n => FormattableString.Invariant($"new DateOnly({n.Year}, {n.Month}, {n.Day})"),
        DateTime n => FormattableString.Invariant($"new DateTime({n.Ticks}L, DateTimeKind.{n.Kind})"),
        DateTimeOffset n => FormattableString.Invariant($"new DateTimeOffset({n.Ticks}L, new TimeSpan({n.Offset.Ticks}L))"),
        _ => throw new InvalidOperationException("Unsupported validation scalar.")
    };

    /// <summary>Gets the nullable C# type.</summary>
    public string DotNetType => Record is not null ? Root!.Name + Record + "?" : Collection is not null ? $"List<{ElementType}>?" : ScalarDotNetType(Type!) + "?";

    /// <summary>Gets a collection's non-null element type.</summary>
    public string ElementType => Collection!.Record is not null ? Root!.Name + Collection.Record : ScalarDotNetType(Collection.Type!);

    /// <summary>Resolves the logical scalar to its exact C# type.</summary>
    private static string ScalarDotNetType(string type) => type switch
    {
        "String" => "string", "Integer" => "int", "Int64" => "long", "Decimal" => "decimal", "Double" => "double", "Boolean" => "bool",
        "Guid" => "Guid", "DateOnly" => "DateOnly", "DateTime" => "DateTime", "DateTimeOffset" => "DateTimeOffset", _ => throw new InvalidOperationException("Unprepared property type.")
    };

    /// <summary>Gets the complete JSON mapping occurrence call.</summary>
    public string JsonMappingExpression
    {
        get
        {
            var args = $"{SelectorLiteral}, x => x.{PropertyName}";
            var scalar = Collection?.Type ?? Type;
            var converter = $"JsonFieldConverters.{scalar}" + (scalar is "DateOnly" or "DateTime" or "DateTimeOffset" ? $"({(Format is null ? "null" : FormatLiteral)})" : string.Empty);
            if (Record is not null) return $"MapObject({args}, {Root!.Name}{Record}Mapping.Create())";
            if (Collection is not null)
                return Collection.Record is not null
                    ? $"MapObjects({args}, {Root!.Name}{Collection.Record}Mapping.Create())"
                    : $"MapValues<{ElementType}>({args}, {converter})";

            return Type == "String" ? $"MapString({args})" : $"MapValue<{ScalarDotNetType(Type!)}>({args}, {converter})";
        }
    }

    /// <summary>Gets a safe mapping selector literal.</summary>
    public string SelectorLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets the positional selector expression.</summary>
    public string IndexExpression => Csv?.Index?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null";

    /// <summary>Gets the non-throwing converter expression.</summary>
    public string Converter => Type switch
    {
        "String" => "CsvFieldConverters.String",
        "DateOnly" => $"CsvFieldConverters.DateOnly({FormatLiteral})",
        "DateTime" => $"CsvFieldConverters.DateTime({FormatLiteral})",
        "DateTimeOffset" => $"CsvFieldConverters.DateTimeOffset({FormatLiteral})",
        _ => $"CsvFieldConverters.Nullable<{DotNetType.TrimEnd('?')}>()"
    };

    /// <summary>Gets a safe date-format literal.</summary>
    public string FormatLiteral => Format is null ? "null" : OnRamp.Utility.CSharpSource.StringLiteral(Format);

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        Name = RequireText(nameof(Name), Name);
        PropertyName = Property is null ? DerivePropertyName(Name) : ConfigValidation.Identifier(this, nameof(Property), Property);
        PropertyName = ConfigValidation.Identifier(this, nameof(Property), PropertyName);
        if ((Type is not null ? 1 : 0) + (Record is not null ? 1 : 0) + (Collection is not null ? 1 : 0) > 1)
            throw new CodeGenException(this, nameof(Type), "Use exactly one of type, record, or collection.");

        if (Root!.IsJson ? Csv is not null : Record is not null || Collection is not null)
            throw new CodeGenException(this, nameof(Csv), "Nested records/collections are supported only by JSON formats; CSV selectors are inapplicable to JSON.");

        if (Collection is not null)
            await Collection.PrepareAsync(Root!, this).ConfigureAwait(false);

        if (Record is not null || Collection is not null)
        {
            if (Mandatory.HasValue || MinLength.HasValue || MaxLength.HasValue || Regex is not null || Email.HasValue || HasMin || HasMax || Precision.HasValue || Scale.HasValue || Options is not null || ReferenceData is not null)
                throw new CodeGenException(this, nameof(Record), "Nested occurrences support required and collection bounds, not scalar validation rules.");

            if (Record is not null)
                ConfigValidation.Identifier(this, nameof(Record), Record);

            if (Format is not null && Collection?.Type is not ("DateOnly" or "DateTime" or "DateTimeOffset"))
                throw new CodeGenException(this, nameof(Format), "Only date scalars support format.");

            if (Collection?.Type is "DateOnly" or "DateTime" or "DateTimeOffset")
                ValidateDateFormat(Collection.Type);

            return;
        }

        Type = DefaultWhereNull(Type, () => "String");
        if (Type is not ("String" or "Integer" or "Int64" or "Decimal" or "Double" or "Boolean" or "Guid" or "DateOnly" or "DateTime" or "DateTimeOffset"))
            throw new CodeGenException(this, nameof(Type), "Unsupported scalar type.");

        Required = DefaultWhereNull(Required, () => false);
        if (Csv is not null)
            await Csv.PrepareAsync(Root!, this).ConfigureAwait(false);
        else if (Root!.IsRouted)
            throw new CodeGenException(this, nameof(Csv), "Routed CSV properties require a positional csv.index.");

        if (Type != "String" && (MinLength.HasValue || MaxLength.HasValue || Regex is not null || Email.HasValue))
            throw new CodeGenException(this, nameof(MinLength), "Length, regex and email rules require String.");

        if (MinLength < 0 || MaxLength < 0 || MinLength > MaxLength)
            throw new CodeGenException(this, nameof(MinLength), "Lengths must be nonnegative and minLength must not exceed maxLength.");

        if (Regex is not null)
        {
            try { _ = new System.Text.RegularExpressions.Regex(Regex, RegexOptions.None, TimeSpan.FromSeconds(1)); }
            catch (ArgumentException ex) { throw new CodeGenException(this, nameof(Regex), ex.Message); }
        }

        if (Type != "Decimal" && (Precision.HasValue || Scale.HasValue))
            throw new CodeGenException(this, nameof(Precision), "Precision and scale require Decimal.");

        if (Precision is < 1 or > 29 || Scale is < 0 or > 28 || Scale > Precision)
            throw new CodeGenException(this, nameof(Precision), "Require precision 1..29, scale 0..28 and scale <= precision.");

        if (Type is "DateOnly" or "DateTime" or "DateTimeOffset")
            ValidateDateFormat(Type);
        else if (Format is not null)
            throw new CodeGenException(this, nameof(Format), "Only date properties support format.");

        if (HasMin || HasMax)
        {
            if (Type is "Boolean" or "Guid")
                throw new CodeGenException(this, nameof(Min), "Bounds are not supported for Boolean or Guid.");

            var min = HasMin ? ParseScalar(Min, nameof(Min)) : null;
            var max = HasMax ? ParseScalar(Max, nameof(Max)) : null;
            if (min is IComparable comparable && max is not null && comparable.CompareTo(max) > 0)
                throw new CodeGenException(this, nameof(Min), "Minimum must not exceed maximum.");
        }

        if (Options is not null)
        {
            if (Options.Count == 0)
                throw new CodeGenException(this, nameof(Options), "At least one allowed value is required.");

            foreach (var option in Options)
                ParseScalar(option, nameof(Options));
        }

        if (ReferenceData is not null)
            await ReferenceData.PrepareAsync(Root!, this).ConfigureAwait(false);
    }

    /// <summary>Checks scalar and scalar-collection date formats without weakening offset semantics.</summary>
    private void ValidateDateFormat(string type)
    {
        if (Format is null)
            return;

        RequireText(nameof(Format), Format);

        try
        {
            var date = new DateTimeOffset(2024, 6, 15, 13, 14, 15, TimeSpan.FromHours(10));
            var culture = System.Globalization.CultureInfo.InvariantCulture;
            var text = type switch
            {
                "DateOnly" => DateOnly.FromDateTime(date.DateTime).ToString(Format, culture),
                "DateTimeOffset" => date.ToString(Format, culture),
                _ => date.DateTime.ToString(Format, culture)
            };
            if (type switch
            {
                "DateOnly" => !DateOnly.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.None, out _),
                "DateTimeOffset" => !HasOffset(text) || !DateTimeOffset.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed) || parsed.Offset != date.Offset,
                _ => Root!.IsJson && HasOffset(text) || !DateTime.TryParseExact(text, Format, culture, System.Globalization.DateTimeStyles.None, out _)
            })
                throw new FormatException("The date format cannot round-trip its required offset semantics.");
        }
        catch (FormatException ex)
        {
            throw new CodeGenException(this, nameof(Format), ex.Message);
        }
    }

    /// <summary>Requires an explicit source offset rather than assuming a local time zone.</summary>
    private static bool HasOffset(string text) => System.Text.RegularExpressions.Regex.IsMatch(text.TrimEnd(), @"(?:[zZ]|[+-]\d{2}:\d{2})$", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>Derives a public property identifier from underscore- and hyphen-separated source names.</summary>
    private static string DerivePropertyName(string sourceName)
    {
        var parts = sourceName.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(x => char.ToUpperInvariant(x[0]) + x[1..]));
    }
}
