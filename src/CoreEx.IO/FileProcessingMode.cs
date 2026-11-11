namespace CoreEx.IO;

/// <summary>Defines the validation and business-execution strategy, independent of diagnostic or unit reporting hooks.</summary>
public enum FileProcessingMode
{
    /// <summary>Validates input without executing business units.</summary>
    ValidateOnly,

    /// <summary>Validates the entire input before replaying its immutable contents for execution.</summary>
    ValidateThenExecute,

    /// <summary>Validates and executes each complete valid unit in one pass, allowing partial writes on later content failure.</summary>
    ValidateAndExecute
}
