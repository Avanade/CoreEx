namespace CoreEx.Entities;

/// <summary>
/// Represents the option for cleaning an <see cref="IContract"/> property value.
/// </summary>
public enum CleanOption
{
    /// <summary>
    /// Indicates that the <see cref="Cleaner.DefaultCleanOption"/> value should be used.
    /// </summary>
    UseDefault,

    /// <summary>
    /// No cleaning required, the value will remain as-is.
    /// </summary>
    None,

    /// <summary>
    /// The value will be cleaned.
    /// </summary>
    /// <remarks>Where the property is an <see cref="IContract"/> all sub-properties will also be cleaned (where applicable).
    /// <para>Where the property is <i>not</i> an <see cref="IContract"/> then the <see cref="Clean"/> and <see cref="CleanAndDefault"/> will have the same outcome.</para></remarks>
    Clean,

    /// <summary>
    /// The value will be cleaned and defaulted.
    /// </summary>
    /// <remarks>Where the property is an <see cref="IContract"/> all sub-properties will also be cleaned (where applicable). Also, where the property is <see cref="Extended.IDefault.IsDefault"/> the value will be <see langword="default">defaulted</see>.
    /// <para>Where the property is <i>not</i> an <see cref="IContract"/> then the <see cref="Clean"/> and <see cref="CleanAndDefault"/> will have the same outcome.</para></remarks>
    CleanAndDefault
}