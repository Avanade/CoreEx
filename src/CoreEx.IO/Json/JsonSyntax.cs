namespace CoreEx.IO.Json;

#if !NET10_0_OR_GREATER
/// <summary>Checks decoded member uniqueness on targets without built-in duplicate rejection.</summary>
internal static class JsonSyntax
{
    /// <summary>Checks object names without decoding string values ignored by the mapping.</summary>
    /// <param name="element">The value parsed by System.Text.Json.</param>
    /// <returns>An invalid-line diagnostic code, or null when valid.</returns>
    public static string? Check(JsonElement element)
    {
        try { return CheckValue(element); }
        catch (InvalidOperationException) { return "jsonl.record.invalid"; }
    }

    /// <summary>Visits parsed values without reimplementing JSON tokenization.</summary>
    /// <param name="element">The parsed value to inspect.</param>
    /// <returns>An invalid-line diagnostic code, or null when valid.</returns>
    private static string? CheckValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var member in element.EnumerateObject())
            {
                if (!names.Add(member.Name))
                    return "jsonl.record.invalid";

                if (CheckValue(member.Value) is { } code)
                    return code;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray())
                if (CheckValue(item) is { } code)
                    return code;

        return null;
    }
}
#endif
