namespace CoreEx.IO;

/// <summary>Defines continuation after expected content errors, not operational faults.</summary>
public enum FileErrorMode
{
    /// <summary>Reports the applicable failing record or rule's diagnostics and stops validation.</summary>
    FailFast,

    /// <summary>Continues validation where structurally safe, without retaining diagnostic history.</summary>
    CollectErrors
}
