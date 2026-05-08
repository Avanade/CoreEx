using HandlebarsDotNet;
using System;
using System.IO;

namespace CoreEx.Generator.Utility;

/// <summary>
/// The core code generator that manages the <b>Handlebars</b> compilation (cached for performance) and enables the corresponding <see cref="Generate"/> (one or more invocations).
/// </summary>
public class HandlebarsCodeGenerator
{
    private readonly HandlebarsTemplate<object?, object?> _template;

    /// <summary>
    /// Static constructor.
    /// </summary>
    static HandlebarsCodeGenerator()
    {
        HandlebarsHelpers.RegisterHelpers();
        Handlebars.Configuration.TextEncoder = null;
    }

    /// <summary>
    /// Creates a new instance of the <see cref="HandlebarsCodeGenerator"/> from the specified <paramref name="resourceName"/>.
    /// </summary>
    /// <param name="resourceName">The fully qualified embedded resource name for the code template.</param>
    /// <returns>The <see cref="HandlebarsCodeGenerator"/>.</returns>
    public static HandlebarsCodeGenerator Create(string resourceName)
    {
        using var s = typeof(HandlebarsCodeGenerator).Assembly.GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException($"Resource '{resourceName}' not found.");
        using var sr = new StreamReader(s);
        return new HandlebarsCodeGenerator(sr);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="HandlebarsCodeGenerator"/> from the specified <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The template <see cref="Stream"/>.</param>
    /// <returns></returns>
    public static HandlebarsCodeGenerator Create(Stream stream)
    {
        using var sr = new StreamReader(stream);
        return new HandlebarsCodeGenerator(sr);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HandlebarsCodeGenerator"/> from the <paramref name="sr"/>.
    /// </summary>
    /// <param name="sr">The template <see cref="StreamReader"/>.</param>
    public HandlebarsCodeGenerator(StreamReader sr)
    {
        if (sr is null)
            throw new ArgumentNullException(nameof(sr));

        _template = Handlebars.Compile(sr.ReadToEnd());
    }

    /// <summary>
    /// Generate content from the template using the <paramref name="context"/> and optional secondary <paramref name="data"/>.
    /// </summary>
    /// <param name="context">The primary context value referenced within the template.</param>
    /// <param name="data">The optional secondary data.</param>
    /// <returns>The resulting generated output.</returns>
    public string Generate(CodeGenContext context, object? data = null) => _template(context, data);
}