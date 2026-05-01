namespace CoreEx.Generator;

/// <summary>
/// Defines the approach to code generation for a given scenario.
/// </summary>
internal enum GenApproach
{
    /// <summary>
    /// No approach has been determined; i.e. initial state.
    /// </summary>
    Undetermined,

    /// <summary>
    /// Declares new code (as virtual), but does not override any existing code.
    /// </summary>
    Declare,

    /// <summary>
    /// Overrides existing code, also calling into the base implementation as appropriate.
    /// </summary>
    Override,

    /// <summary>
    /// Bypasses code generation as determined as not required (i.e. manually implemented) or unable (i.e. sealed).
    /// </summary>
    Bypass
}