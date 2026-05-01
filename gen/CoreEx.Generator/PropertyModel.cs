using CoreEx.Generator.Utility;

namespace CoreEx.Generator;

/// <summary>
/// Represents the <i>ContractAttribute</i> class model's property configuration used to drive the underlying partial class source generation.
/// </summary>
internal class PropertyModel
{
    /// <summary>
    /// Gets the owner <see cref="CodeGenContext"/> of the property.
    /// </summary>
    public CodeGenContext? Context { get; set; }

    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Indicates whether the property type is a nullable value type.
    /// </summary>
    public bool IsNullableValueType { get; set; }

    /// <summary>
    /// Indicates whether the property has been declared as partial.
    /// </summary>
    public bool IsPartial { get; set; }

    /// <summary>
    /// Indicates whether the property is read-only (i.e. does not have a setter).
    /// </summary>
    public bool IsReadonly { get; set; }

    /// <summary>
    /// Indicates whether the property is init-only (i.e. has an init setter syntax).
    /// </summary>
    public bool IsInitOnly { get; set; }

    /// <summary>
    /// Indicates whether the property is settable (i.e. has a setter that is not init-only).
    /// </summary>
    public bool IsSettable => !IsReadonly && !IsInitOnly;

    /// <summary>
    /// Indicates whether the property is required (i.e. has required syntax).
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Indicates whether the property has a <see cref="JsonName"/>.
    /// </summary>
    public bool HasJsonName => !string.IsNullOrEmpty(JsonName);

    /// <summary>
    /// Gets or sets the JSON property name where different from the <see cref="Name"/> without the <i>Code</i> suffix for reference data serialization.
    /// </summary>
    public string? JsonName { get; set; }

    /// <summary>
    /// Gets or sets the key and/or text for the property.
    /// </summary>
    public string? KeyAndOrText { get; set; }

    /// <summary>
    /// Gets or sets the fallback text for the property.
    /// </summary>
    public string? FallbackText { get; set; }

    /// <summary>
    /// Indicates whether the property has <see cref="KeyAndOrText"/>.
    /// </summary>
    public bool HasText => !string.IsNullOrEmpty(KeyAndOrText);

    /// <summary>
    /// Indicates whether the property has <see cref="FallbackText"/>.
    /// </summary>
    public bool HasFallbackText => !string.IsNullOrEmpty(FallbackText);

    /// <summary>
    /// Gets or sets the default; being the corresponding c# code.
    /// </summary>
    public string? Default { get; set; }

    /// <summary>
    /// Indicates whether the property has a <see cref="Default"/>.
    /// </summary>
    public bool HasDefault => !string.IsNullOrEmpty(Default);

    /// <summary>
    /// Gets or sets the format string used when formatting the property value as a <see langword="string"/>.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Indicates whether the property has a <see cref="Format"/>.
    /// </summary>
    public bool HasFormat => Format is not null;

    /// <summary>
    /// Indicates whether the property has been marked up with the <i>ReferenceData&lt;TRefData&gt;</i>.
    /// </summary>
    public bool IsRefData { get; set; }

    /// <summary>
    /// Indicates whether the property name ends with the 'Sid' or 'Sids' suffix.
    /// </summary>
    public bool IsSuffixSid => IsRefDataCodeCollection
        ? Name is not null && Name.EndsWith("Sids")
        : Name is not null && Name.EndsWith("Sid");

    /// <summary>
    /// Gets the reference data name.
    /// </summary>
    public string? RefDataName => Name is not null && IsRefData 
        ? (IsSuffixSid ? Name.Substring(0, Name.Length - 3) : Name.Substring(0, Name.Length - 4)) 
        : Name is not null && IsRefDataCodeCollection 
            ? Pluralizer.Instance.Pluralize(IsSuffixSid ? Name.Substring(0, Name.Length - 4) : Name.Substring(0, Name.Length - 5)) : Name;

    /// <summary>
    /// Gets or sets the reference data type where<see cref="IsRefData"/> is <see langword="true"/>.
    /// </summary>
    public string? RefDataType { get; set; }

    /// <summary>
    /// Indicates whether the json serialization attribute is required.
    /// </summary>
    public bool IsRefDataJson { get; set; }

    /// <summary>
    /// Indicates whether an additional <i>Text</i> property is required.
    /// </summary>
    public bool IsRefDataText { get; set; }

    /// <summary>
    /// Gets or sets the JSON property name used for reference data text serialization.
    /// </summary>
    public string? RefDataTextJsonName { get; set; }

    /// <summary>
    /// Indicates whether the property has been marked up with the <i>ReferenceDataCodeCollection&lt;TRefData&gt;</i>.
    /// </summary>
    public bool IsRefDataCodeCollection { get; set; }

    /// <summary>
    /// Gets the corresponding backing field name for the <see cref="IsRefDataCodeCollection"/> property.
    /// </summary>
    public string RefDataCodeCollectionFieldName => $"_{char.ToLowerInvariant(Name![0])}{Name.Substring(1)}";

    /// <summary>
    /// Gets the JSON property name used to represent the reference data code collection.
    /// </summary>
    public string RefDataCodeCollectionJsonName => $"{char.ToLowerInvariant(RefDataName![0])}{RefDataName.Substring(1)}";

    /// <summary>
    /// Indicates whether the property is self-cleaned (i.e. has a StringAttribute or DataTimeAttribute declared).
    /// </summary>
    public bool IsSelfCleaned => IsSelfCleanedString || IsSelfCleanedDateTime || IsCleanOption;

    /// <summary>
    /// Indicates whether the property is self-cleaned as a <see cref="string"/> value (i.e. has a StringAttribute declared).
    /// </summary>
    public bool IsSelfCleanedString { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="string"/> trim.
    /// </summary>
    public string? StringTrim { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="string"/> transform.
    /// </summary>
    public string? StringTransform { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="string"/> casing.
    /// </summary>
    public string? StringCase { get; set; }

    /// <summary>
    /// Indicates whether the property is self-cleaned as a <see cref="DateTime"/> value (i.e. has a DateTimeAttribute declared).
    /// </summary>
    public bool IsSelfCleanedDateTime { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="System.DateTime"/> transform.
    /// </summary>
    public string? DateTimeTransform { get; set; }

    /// <summary>
    /// Indicates whether the property has a clean option specified.
    /// </summary>
    public bool IsCleanOption { get; set; }

    /// <summary>
    /// Gets or sets the clean option.
    /// </summary>
    public string? CleanOption { get; set; } = "UseDefault";

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not PropertyModel other)
            return false;

        if (Name != other.Name || Type != other.Type || IsNullableValueType != other.IsNullableValueType || IsPartial != other.IsPartial || IsReadonly != other.IsReadonly || IsInitOnly != other.IsInitOnly || IsRequired != other.IsRequired
            || KeyAndOrText != other.KeyAndOrText || FallbackText != other.FallbackText || JsonName != other.JsonName || Default != other.Default || Format != other.Format
            || IsRefData != other.IsRefData || RefDataType != other.RefDataType
            || IsRefDataJson != other.IsRefDataJson || IsRefDataText != other.IsRefDataText || RefDataTextJsonName != other.RefDataTextJsonName
            || IsRefDataCodeCollection != other.IsRefDataCodeCollection
            || IsSelfCleanedString != other.IsSelfCleanedString || StringTrim != other.StringTrim || StringTransform != other.StringTransform || StringCase != other.StringCase
            || IsSelfCleanedDateTime != other.IsSelfCleanedDateTime || DateTimeTransform != other.DateTimeTransform
            || IsCleanOption != other.IsCleanOption || CleanOption != other.CleanOption)
            return false;

        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
        => Name?.GetHashCode() ?? 0
            ^ (Type?.GetHashCode() ?? 0)
            ^ IsNullableValueType.GetHashCode()
            ^ IsPartial.GetHashCode()
            ^ IsReadonly.GetHashCode()
            ^ IsInitOnly.GetHashCode()
            ^ IsRequired.GetHashCode()
            ^ (KeyAndOrText?.GetHashCode() ?? 0)
            ^ (FallbackText?.GetHashCode() ?? 0)
            ^ (JsonName?.GetHashCode() ?? 0)
            ^ (Default?.GetHashCode() ?? 0)
            ^ (Format?.GetHashCode() ?? 0)
            ^ IsRefData.GetHashCode()
            ^ (RefDataType?.GetHashCode() ?? 0)
            ^ IsRefDataJson.GetHashCode()
            ^ IsRefDataText.GetHashCode()
            ^ (RefDataTextJsonName?.GetHashCode() ?? 0)
            ^ IsRefDataCodeCollection.GetHashCode()
            ^ IsSelfCleanedString.GetHashCode()
            ^ (StringTrim?.GetHashCode() ?? 0)
            ^ (StringTransform?.GetHashCode() ?? 0)
            ^ (StringCase?.GetHashCode() ?? 0)
            ^ IsSelfCleanedDateTime.GetHashCode()
            ^ (DateTimeTransform?.GetHashCode() ?? 0)
            ^ IsCleanOption.GetHashCode()
            ^ (CleanOption?.GetHashCode() ?? 0);
}