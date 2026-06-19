namespace CoreEx.Entities;

/// <summary>
/// Provides a <see cref="string"/> and <see cref="Guid"/> <see cref="IIdentifierGenerator"/> where each is created using a <see cref="Guid"/>.
/// </summary>
/// <remarks>A custom <see cref="IIdentifierGenerator"/> can be used to support different identifier generation strategies and identifier types as necessary.</remarks>
public class IdentifierGenerator : IIdentifierGenerator
{
    private static readonly IIdentifierGenerator _default = new IdentifierGenerator();

    /// <summary>
    /// Gets the current <see cref="IIdentifierGenerator"/> from the <see cref="ExecutionContext"/>, or the default <see cref="IdentifierGenerator"/> where not available.
    /// </summary>
    public static IIdentifierGenerator Current => ExecutionContext.GetService<IIdentifierGenerator>() ?? _default;

    /// <inheritdoc/>
    public Guid GenerateGuid()
#if NET9_0_OR_GREATER
        => Guid.CreateVersion7();
#else
        => Guid.NewGuid();
#endif

    /// <inheritdoc/>
    public Task<TId> GenerateIdentifierAsync<TId>() => Task.FromResult(typeof(TId) switch
    {
        Type _ when typeof(TId) == typeof(string) => Internal.Cast<string, TId>(GenerateGuid().ToString()),
        Type _ when typeof(TId) == typeof(Guid) => Internal.Cast<Guid, TId>(GenerateGuid()),
        _ => throw new NotSupportedException($"Identifier Type '{typeof(TId).Name}' is not supported; only String or Guid.")
    });

    /// <inheritdoc/>
    public async Task<TId> GenerateIdentifierAsync<TId, TFor>() where TFor : class => await GenerateIdentifierAsync<TId>().ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task AssignIdentifierAsync<TFor>(TFor value) where TFor : class
    {
        if (value is not IReadOnlyIdentifier ii)
            return;

        if (value is IIdentifier<string> iis)
            iis.Id ??= await GenerateIdentifierAsync<string, TFor>().ConfigureAwait(false);
        else if (value is IIdentifier<Guid> iig)
        {
            if (iig.Id == Guid.Empty)
                iig.Id = await GenerateIdentifierAsync<Guid, TFor>().ConfigureAwait(false);
        }
        else
            throw new NotSupportedException($"Identifier Type '{ii.IdType.Name}' is not supported; only String or Guid.");
    }
}