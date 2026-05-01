namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains an <see cref="IReferenceData"/> (<see cref="ReferenceDataRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowInactive">Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ReferenceData<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool allowInactive = false) where TEntity : class where TProperty : IReferenceData
        => Chain(rule, new ReferenceDataRule<TEntity, TProperty>(allowInactive));

    /// <summary>
    /// Chains an <see cref="IReferenceData"/> (<see cref="ReferenceDataRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowInactive">Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.</param>
    public static IPropertyRule<TEntity, TProperty> IsValid<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool allowInactive = false) where TEntity : class where TProperty : IReferenceData
        => ReferenceData(rule, allowInactive);

    /* ReferenceData-string */

    /// <summary>
    /// Chains a <see langword="string"/>-based <see cref="IReferenceData.Code"/> (<see cref="ReferenceDataCodeRule{TEntity}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="with">The <see cref="ReferenceDataCodeRule{TEntity}.ReferenceDataWith"/> function.</param>
    public static IPropertyRule<TEntity, string> ReferenceData<TEntity>(this IPropertyRule<TEntity, string> rule, Func<ReferenceDataCodeRule<TEntity>.ReferenceDataWith, ReferenceDataCodeRule<TEntity>.ReferenceDataWith> with) where TEntity : class
        => Chain(rule, new ReferenceDataCodeRule<TEntity>(with));

    /* IReferenceDataCodeCollection */

    /// <summary>
    /// Chains an <see cref="IReferenceDataCodeCollection"/> (<see cref="ReferenceDataRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowInactive">Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ReferenceDataCodes<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool allowInactive = false) where TEntity : class where TProperty : IReferenceDataCodeCollection
        => Chain(rule, new ReferenceDataCodeCollectionRule<TEntity, TProperty>(allowInactive));

    /// <summary>
    /// Chains an <see cref="IReferenceDataCodeCollection"/> (<see cref="ReferenceDataRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowInactive">Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.</param>
    public static IPropertyRule<TEntity, TProperty> AreValid<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool allowInactive = false) where TEntity : class where TProperty : IReferenceDataCodeCollection
        => ReferenceDataCodes(rule, allowInactive);
}