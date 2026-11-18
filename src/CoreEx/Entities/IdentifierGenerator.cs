namespace CoreEx.Entities;

/// <summary>
/// Allocates new <see cref="string"/> and <see cref="Guid"/> identifiers using GUIDs and provides a static helper for deriving deterministic identifiers.
/// </summary>
/// <remarks>A custom <see cref="IIdentifierGenerator"/> can be used to support different identifier generation strategies and identifier types as necessary.</remarks>
public class IdentifierGenerator : IIdentifierGenerator
{
    private static readonly IIdentifierGenerator _default = new IdentifierGenerator();
    private static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Gets the current <see cref="IIdentifierGenerator"/> from the <see cref="ExecutionContext"/>, or the default <see cref="IdentifierGenerator"/> where not available.
    /// </summary>
    public static IIdentifierGenerator Current => ExecutionContext.GetService<IIdentifierGenerator>() ?? _default;

    /// <summary>
    /// Derives a deterministic identifier from an ordered sequence of string parts.
    /// </summary>
    /// <param name="parts">One or more parts. Order, casing, and whitespace are significant; <see langword="null"/> is distinct from an empty string.</param>
    /// <returns>The full SHA-256 digest as 64 lowercase hexadecimal characters.</returns>
    /// <remarks>
    /// Each part is framed by its signed 32-bit big-endian byte length followed by its UTF-8 bytes without a BOM; <see langword="null"/> is encoded as length -1 without bytes.
    /// This format is independent of <see cref="Current"/> and the current culture. Format non-string inputs invariantly before calling and consider a leading purpose/version part for durable identifiers.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="parts"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="parts"/> is empty.</exception>
    /// <exception cref="EncoderFallbackException">A part contains invalid UTF-16.</exception>
    public static string FromParts(params string?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        if (parts.Length == 0)
            throw new ArgumentException("At least one identifier part is required.", nameof(parts));

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var part in parts)
        {
            var bytes = part is null ? null : _strictUtf8.GetBytes(part);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes?.Length ?? -1);
            hash.AppendData(length);
            if (bytes is not null)
                hash.AppendData(bytes);
        }

        return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }

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
    public async Task AssignIdentifierAsync<TFor>(TFor value) where TFor : class, IIdentifier
    {
        if (value is IIdentifier<string> iis)
            iis.Id ??= await GenerateIdentifierAsync<string, TFor>().ConfigureAwait(false);
        else if (value is IIdentifier<Guid> iig)
        {
            if (iig.Id == Guid.Empty)
                iig.Id = await GenerateIdentifierAsync<Guid, TFor>().ConfigureAwait(false);
        }
        else
            throw new NotSupportedException($"Identifier Type '{value.IdType.Name}' is not supported; only String or Guid.");
    }
}
