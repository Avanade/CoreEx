namespace CoreEx.Operations;

/// <summary>
/// Provides the maximum lengths enforced by every <see cref="IOperationStore"/>, matching the relational column sizes.
/// </summary>
public static class OperationLimits
{
    /// <summary>
    /// Gets the maximum initiator identifier length.
    /// </summary>
    public const int MaxInitiatorIdLength = 256;

    /// <summary>
    /// Gets the maximum claim owner token length.
    /// </summary>
    public const int MaxClaimOwnerLength = 128;

    /// <summary>
    /// Gets the maximum user-safe progress text length.
    /// </summary>
    public const int MaxProgressLength = 1000;

    /// <summary>
    /// Gets the maximum error code length.
    /// </summary>
    public const int MaxErrorCodeLength = 100;

    /// <summary>
    /// Gets the maximum user-safe error message length.
    /// </summary>
    public const int MaxErrorMessageLength = 1000;

    /// <summary>
    /// Gets the maximum result location length.
    /// </summary>
    public const int MaxResultLocationLength = 2000;
}
