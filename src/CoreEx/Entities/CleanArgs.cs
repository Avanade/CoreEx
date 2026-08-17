namespace CoreEx.Entities;

/// <summary>
/// Provides the arguments that control the behavior of <see cref="Cleaner.Clean{T}(T, CleanArgs)"/> (and related runtime metadata cleaning).
/// </summary>
public readonly struct CleanArgs
{
    /// <summary>
    /// Gets the default <see cref="CleanArgs"/> instance (i.e. all options set to their default value of <see langword="false"/>).
    /// </summary>
    public static readonly CleanArgs Default = default;

    /// <summary>
    /// Gets or sets a value indicating whether nested (child) values are also cleaned and defaulted (i.e. collapsed to <see langword="default"/>) where fully default and their <see cref="CleanOption"/> is <see cref="CleanOption.CleanAndDefault"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>.</remarks>
    public bool CleanAndDefaultNested { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the root value itself is also cleaned and defaulted (i.e. collapsed to <see langword="default"/>) where fully default and its <see cref="CleanOption"/> is <see cref="CleanOption.CleanAndDefault"/>.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/>. The root value is otherwise never defaulted, regardless of <see cref="CleanAndDefaultNested"/>.</remarks>
    public bool CleanAndDefaultRoot { get; init; }
}
