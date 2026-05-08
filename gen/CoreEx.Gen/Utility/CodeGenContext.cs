using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Gen.Utility;

/// <summary>
/// Provides context for code generation, allowing for customization of the generated code.
/// </summary>
public class CodeGenContext
{
    /// <summary>
    /// Gets or sets the indent level to be used for the generated code.
    /// </summary>
    public int Indent { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of spaces used for each indentation level.
    /// </summary>
    public int IndentSize { get; set; } = 4;

    /// <summary>
    /// Increments the indent level by one.
    /// </summary>
    public void IncrementIndent() => Indent++;

    /// <summary>
    /// Decreases the current indentation level by one.
    /// </summary>
    public void DecrementIndent() => Indent--;

    /// <summary>
    /// Gets the string used for indentation, consisting of spaces.
    /// </summary>
    public string GetIndentString() => new(' ', Indent * IndentSize);

    /// <summary>
    /// Gets the <see cref="Diagnostic"/> list to be reported.
    /// </summary>
    public List<Diagnostic> Diagnostics { get; } = [];

    /// <summary>
    /// Reports the accumulated <see cref="Diagnostics"/> to the provided <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="SourceProductionContext"/>.</param>
    /// <returns><see langword="true"/> indicates there are no <see cref="Diagnostics"/> in <see cref="DiagnosticSeverity.Error"/> and source production should occur; otherwise, <see langword="false"/> indicates that no source production should occur.</returns>
    public bool ReportDiagnostics(SourceProductionContext context)
    {
        foreach (var d in Diagnostics)
            context.ReportDiagnostic(d);

        return !Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
    }
}