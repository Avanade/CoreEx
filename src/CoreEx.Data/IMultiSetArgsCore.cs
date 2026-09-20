namespace CoreEx.Data;

/// <summary>
/// Enables the base multi-set arguments used to read multiple result sets (or equivalent) from a single data source round-trip.
/// </summary>
/// <remarks>This is the minimal, storage-agnostic contract shared by provider-specific multi-set capabilities (see <c>CoreEx.Database.Extended.IMultiSetArgs</c> for the
/// positional/ordered result-set variant, and <c>CoreEx.Cosmos.Extended.IMultiSetArgs</c> for the discriminator-keyed variant). Each provider adds its own record/item
/// callback shaped for its underlying data access mechanism.</remarks>
/// <remarks>Named <see cref="IMultiSetArgsCore"/> (not <c>IMultiSetArgs</c>) so each provider's own <c>IMultiSetArgs</c> - which necessarily shares this simple name for its own callers' convenience - can
/// extend it unqualified (<c>: IMultiSetArgsCore</c>) from within a project that also has a <c>global using IMultiSetArgs = ...</c> alias pointing at its own provider-specific type; without the distinct
/// name, every reference to this base interface from such a project would require an awkward fully-qualified <c>CoreEx.Data.IMultiSetArgs</c>.</remarks>
public interface IMultiSetArgsCore
{
    /// <summary>
    /// Gets the minimum number of rows allowed.
    /// </summary>
    int MinimumRows { get; }

    /// <summary>
    /// Gets the maximum number of rows allowed.
    /// </summary>
    int? MaximumRows { get; }

    /// <summary>
    /// Indicates whether to stop further result set processing where the current set has resulted in a <see langword="null"/> (i.e. no records).
    /// </summary>
    bool StopOnNull { get; }

    /// <summary>
    /// Invokes the corresponding result function.
    /// </summary>
    void InvokeResult();
}
