namespace CoreEx.Data;

/// <summary>
/// Represents position-based paging arguments; specifically <see cref="Skip"/> and <see cref="Take"/>.
/// </summary>
public record class PagingArgs
{
    private static int? _defaultTake;
    private static int? _maximumTake;

    /// <summary>
    /// Creates a new default <see cref="PagingArgs"/> with <i>no</i> <see cref="IsCountRequested"/>.
    /// </summary>
    /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
    /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
    /// <param name="count">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query.</param>
    public static PagingArgs Create(int skip = 0, int? take = null, bool count = false) => new(skip, take, count);

    /// <summary>
    /// Creates a new default <see cref="PagingArgs"/> <i>with</i> <see cref="IsCountRequested"/>.
    /// </summary>
    /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
    /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
    public static PagingArgs CreateWithCount(int skip = 0, int? take = null) => new(skip, take, count: true);

    /// <summary>
    /// Gets or sets the default <see cref="Take"/>.
    /// </summary>
    /// <remarks>Defaults to settings '<c>CoreEx:Data:PagingArgs:DefaultTake</c>'; otherwise, <c>25</c>.</remarks>
    public static int DefaultTake
    {
        get => _defaultTake ?? Internal.GetConfigurationValue("CoreEx:Data:PagingArgs:DefaultTake", 25);
        set => _defaultTake = value < 1 ? null : value;
    }

    /// <summary>
    /// Gets or sets the maximum <see cref="Take"/>.
    /// </summary>
    /// <remarks>Defaults to settings '<c>CoreEx:Data:PagingArgs:MaximumTake</c>'; otherwise, <c>1000</c>.</remarks>
    public static int MaximumTake
    {
        get => _maximumTake ?? Internal.GetConfigurationValue("CoreEx:Data:PagingArgs:MaximumTake", 1000);
        set => _maximumTake = value < 1 ? null : value;
    }

    /// <summary>
    /// Represents a <see cref="PagingArgs"/> that will explicitly <b>not</b> be applied.
    /// </summary>
    /// <remarks>This instance is immutable.</remarks>
    public static PagingArgs None { get; } = new() { IsNone = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="PagingArgs"/> class.
    /// </summary>
    /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
    /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
    /// <param name="count">Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query.</param>
    public PagingArgs(int skip = 0, int? take = null, bool count = false)
    {
        Skip = skip;
        Take = take ?? DefaultTake;
        IsCountRequested = count;
    }

    /// <summary>
    /// Gets the specified number of elements in a sequence to bypass.
    /// </summary>
    public int Skip { get => field; init => CheckImmutable(field = value < 0 ? 0 : value); }

    /// <summary>
    /// Gets the specified number of contiguous elements from the start of a sequence.
    /// </summary>
    public int Take { get => field; init => field = CheckImmutable(value <= 0 ? DefaultTake : Math.Min(MaximumTake, value)); }

    /// <summary>
    /// Indicates whether to get the total count (see <see cref="PagingResult.TotalCount"/>) when performing the underlying query (defaults to <see langword="false"/>).
    /// </summary>
    /// <remarks>This may result in a secondary query and therefore impact overall performance; this should be used judiciously.
    /// <para>There are no guarantees that a count will be performed, as this will depend on the underlying implementation; hence, it is simply a request.</para></remarks>
    public bool IsCountRequested { get; init => field = CheckImmutable(value); }

    /// <summary>
    /// Immutability value check for <see cref="Skip"/> and <see cref="Take"/> properties where <see cref="IsNone"/>.
    /// </summary>
    private T CheckImmutable<T>(T value) => IsNone ? throw new InvalidOperationException($"The {nameof(PagingArgs)} cannot be mutated when {nameof(IsNone)} is true.") : value;

    /// <summary>
    /// Indicates whether the <see cref="PagingArgs"/> is the <see cref="None"/> instance.
    /// </summary>
    public bool IsNone { get; private init; }
}