namespace CoreEx.CodeGen.Files.Config;

/// <summary>Applies the File generator's ASCII-only C# identifier and namespace policy.</summary>
internal static partial class ConfigValidation
{
    private static readonly HashSet<string> _keywords = new(("abstract as base bool break byte case catch char checked class const continue decimal default delegate do double else enum event " +
        "explicit extern false finally fixed float for foreach goto if implicit in int interface internal is lock long namespace new null object operator out override params private protected " +
        "public readonly ref return sbyte sealed short sizeof stackalloc static string struct switch this throw true try typeof uint ulong unchecked unsafe ushort using virtual void volatile while " +
        "__arglist __makeref __reftype __refvalue").Split(' '), StringComparer.Ordinal);

    /// <summary>Requires an ASCII C# identifier without rewriting names or accepting invisible aliases.</summary>
    internal static string Identifier(ConfigBase config, string property, string? value)
    {
        var text = config.RequireText(property, value);
        if (!NameRegex().IsMatch(text) || _keywords.Contains(text))
            throw new CodeGenException(config, property, "Specify an unescaped ASCII C# identifier that is not a reserved keyword.");

        return text;
    }

    /// <summary>Validates an explicit or inferred namespace.</summary>
    internal static string Namespace(ConfigBase config, string property, string? value)
    {
        var text = config.RequireText(property, value);
        foreach (var part in text.Split('.'))
            Identifier(config, property, part);

        return text;
    }

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex NameRegex();
}
