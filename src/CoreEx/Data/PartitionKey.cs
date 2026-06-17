namespace CoreEx.Data;

/// <summary>
/// Provides the <see cref="IReadOnlyPartitionKey.PartitionKey"/> capabilities.
/// </summary>
public class PartitionKey
{
    /// <summary>
    /// Gets the default partition size.
    /// </summary>
    public const int DefaultPartitionSize = 4;

    /// <summary>
    /// Gets the maximum byte count for stack allocation when converting strings to UTF-8.
    /// </summary>
    private const int MaxStackAllocByteCount = 256;

    /// <summary>
    /// Gets the hash-based partition identifier/number for the specified <paramref name="partitionKey"/> based on the planned <paramref name="partitionSize"/>.
    /// </summary>
    /// <param name="partitionKey">The deterministic partition key.</param>
    /// <param name="partitionSize">The partition size (i.e the number of possible partitions).</param>
    /// <param name="ignoreCase">Indicates whether to ignore case.</param>
    /// <returns>The resulting partition identifier/number.</returns>
    /// <remarks>The <paramref name="partitionKey"/> <b>must</b> be a universal, deterministic, and culture-independent <see cref="string"/>; where in doubt use <see cref="CompositeKey.ToString"/> which will enable.</remarks>
    public static int GetPartitionId(string partitionKey, int partitionSize = DefaultPartitionSize, bool ignoreCase = true)
    {
        partitionKey = partitionKey.ThrowIfNull().Trim().ThrowIfNullOrEmpty().ThrowWhen(pk => pk.Length > 256, nameof(partitionKey));
        partitionSize.ThrowWhen(ps => ps <= 0 || ps > 256, nameof(partitionSize));

        static int GenerateIdFromHash(ReadOnlySpan<byte> bytes, int partitionSize)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(bytes, hash);
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(hash);

            return (partitionSize & (partitionSize - 1)) == 0
                ? (int)(value & (uint)(partitionSize - 1))
                : (int)(value % (uint)partitionSize);
        }

        // Faster path for GUIDs: hash the 16-byte representation directly.
        if (Guid.TryParse(partitionKey, out var guid))
        {
            Span<byte> guidBytes = stackalloc byte[16];
            guid.TryWriteBytes(guidBytes);
            return GenerateIdFromHash(guidBytes, partitionSize);
        }

        // Slower path for strings as casing and encoding is an additional step.
        var normalized = ignoreCase ? partitionKey.ToUpperInvariant() : partitionKey;
        var byteCount = Encoding.UTF8.GetByteCount(normalized);
        Span<byte> utf8 = byteCount <= MaxStackAllocByteCount ? stackalloc byte[byteCount] : new byte[byteCount];
        Encoding.UTF8.GetBytes(normalized.AsSpan(), utf8);
        return GenerateIdFromHash(utf8, partitionSize);
    }

    /// <summary>
    /// Gets the hash-based partition identifier/number for the specified <paramref name="partitionKey"/> based on the planned <paramref name="partitionSize"/> formatted as a string with leading zeros where appropriate.
    /// </summary>
    /// <param name="partitionKey">The deterministic partition key.</param>
    /// <param name="partitionSize">The partition size (i.e the number of possible partitions).</param>
    /// <param name="ignoreCase">Indicates whether to ignore case.</param>
    /// <returns>The resulting partition identifier/number.</returns>
    /// <remarks>The <paramref name="partitionKey"/> <b>must</b> be a universal, deterministic, and culture-independent <see cref="string"/>; where in doubt use <see cref="CompositeKey.ToString"/> which will enable.</remarks>
    public static string GetPartitionIdAsString(string partitionKey, int partitionSize = DefaultPartitionSize, bool ignoreCase = true)
    {
        var id = GetPartitionId(partitionKey, partitionSize, ignoreCase);
        var maxId = partitionSize - 1;
        var digits = maxId switch
        {
            < 10 => 1,
            < 100 => 2,
            _ => 3
        };

        return id.ToString($"D{digits}", CultureInfo.InvariantCulture);
    }
}