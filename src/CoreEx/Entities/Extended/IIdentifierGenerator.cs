namespace CoreEx.Entities.Extended;

/// <summary>
/// Enables the generation of a new identifier value for <i>any</i> identifier <see cref="Type"/>.
/// </summary>
public interface IIdentifierGenerator
{
    /// <summary>
    /// Generates a new <see cref="Guid"/> (version 7 or other sequential GUID preferred).
    /// </summary>
    /// <returns>The newly generated <see cref="Guid"/>.</returns>
    Guid GenerateGuid();

    /// <summary>
    /// Generate a new identifier value.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
    /// <returns>The newly generated identifier.</returns>
    Task<TId> GenerateIdentifierAsync<TId>();

    /// <summary>
    /// Generate a new identifier value.
    /// </summary>
    /// <typeparam name="TId">The identifier <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
    /// <returns>The newly generated identifier.</returns>
    /// <remarks>The <typeparamref name="TFor"/> allows for the likes of different identity sequences per <see cref="System.Type"/> for example.</remarks>
    Task<TId> GenerateIdentifierAsync<TId, TFor>() where TFor : class;

    /// <summary>
    /// Assigns a generated identifier to the <paramref name="value"/> where the <see cref="IIdentifier.Id"/> has a default value.
    /// </summary>
    /// <typeparam name="TFor">The <see cref="System.Type"/> to generate for.</typeparam>
    /// <param name="value">The value to assign an identifier for.</param>
    Task AssignIdentifierAsync<TFor>(TFor value) where TFor : class;
}