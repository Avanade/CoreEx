namespace CoreEx.Validation;

/// <summary>
/// Provides the base <see cref="IReferenceData"/> validator.
/// </summary>
/// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
public partial class ReferenceDataValidator<TRef> : Validator<TRef> where TRef : class, IReferenceData
{
    /// <summary>
    /// Gets or sets the maximum length of the <see cref="IReferenceData.Code"/> property.
    /// </summary>
    public static int CodeMaximumLength { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum length of the <see cref="IReferenceData.Text"/> property.
    /// </summary>
    public static int TextMaximumLength { get; set; } = 250;

    /// <summary>
    /// Gets or sets the maximum length of the <see cref="IReferenceData.Description"/> property.
    /// </summary>
    public static int DescriptionMaximumLength { get; set; } = 1000;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceDataValidator{TRef}"/> class.
    /// </summary>
    public ReferenceDataValidator()
    {
        Property(p => p.Code).Mandatory().MaximumLength(CodeMaximumLength);
        Property(p => p.Text).Mandatory().MaximumLength(TextMaximumLength);
        Property(p => p.Description).MaximumLength(DescriptionMaximumLength);
        Property(p => p.EndsOn).CompareProperty(CompareOperator.GreaterThanOrEqualTo, p => p.StartsOn);
    }
}
