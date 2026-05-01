namespace CoreEx.Text;

/// <summary>
/// Provides common sentence case capabilities.
/// </summary>
public static partial class SentenceCase
{
    /// <summary>
    /// The <see cref="Regex"/> pattern for splitting strings into a sentence of words.
    /// </summary>
    public const string WordSplitPattern = @"([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z])|[\s_-])";

    /// <summary>
    /// Gets the compiled <see cref="Regex"/> for splitting strings into a sentence of words (see <see cref="WordSplitPattern"/>).
    /// </summary>
    public static Regex WordSplitRegex { get; } = _wordSplitRegex();

    /// <summary>
    /// Provides the generated <see cref="Regex"/> for splitting strings into a sentence of words.
    /// </summary>
    [GeneratedRegex(WordSplitPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex _wordSplitRegex();

    /// <summary>
    /// Performs a sentence case word split on the specified <paramref name="text"/>.
    /// </summary>
    /// <param name="text">The text to sentence case word split.</param>
    /// <returns>An array of words.</returns>
    public static string[] SplitIntoWords(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        var matches = WordSplitRegex.EnumerateMatches(text);
        var buffer = ArrayPool<string>.Shared.Rent(16);
        int count = 0;
        int lastIndex = 0;
        
        try
        {
            foreach (var match in matches)
            {
                if (count >= buffer.Length)
                {
                    var newBuffer = ArrayPool<string>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, newBuffer, count);
                    ArrayPool<string>.Shared.Return(buffer);
                    buffer = newBuffer;
                }

                char matchedChar = text[match.Index];
                
                // If matched char is a delimiter, extract word before it and skip the delimiter
                if (matchedChar == '_' || matchedChar == '-' || char.IsWhiteSpace(matchedChar))
                {
                    if (match.Index > lastIndex)
                        buffer[count++] = text[lastIndex..match.Index];

                    lastIndex = match.Index + 1; // Skip the delimiter
                }
                else
                {
                    // Case-change split: include the matched character
                    buffer[count++] = text[lastIndex..(match.Index + 1)];
                    lastIndex = match.Index + 1;
                }
            }

            if (lastIndex < text.Length)
            {
                if (count >= buffer.Length)
                {
                    var newBuffer = ArrayPool<string>.Shared.Rent(buffer.Length * 2);
                    Array.Copy(buffer, newBuffer, count);
                    ArrayPool<string>.Shared.Return(buffer);
                    buffer = newBuffer;
                }
                buffer[count++] = text[lastIndex..];
            }
            
            var result = new string[count];
            Array.Copy(buffer, result, count);
            return result;
        }
        finally
        {
            ArrayPool<string>.Shared.Return(buffer);
        }
    }

    /// <summary>
    /// Converts a <see cref="string"/> into sentence case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> as sentence case.</returns>
    /// <remarks>For example a value of '<c>VarNameDB</c>' would return '<c>Var name DB</c>'.
    /// <para>Uses the <see cref="SentenceCaseConverter"/> function to perform the conversion.</para></remarks>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToSentenceCase(string? text) => SentenceCaseConverter is null ? text : SentenceCaseConverter(text);

    /// <summary>
    /// Gets or sets the underlying logic to perform the sentence case conversion.
    /// </summary>
    /// <remarks>Defaults to the <see cref="SentenceCaseConversion(string?)"/> logic.</remarks>
    public static Func<string?, string?>? SentenceCaseConverter { get; set; } = SentenceCaseConversion;

    /// <summary>
    /// Performs the out-of-the-box sentence case conversion.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <remarks>Defaults to the following: Initial word splitting is performed using the <see cref="WordSplitPattern"/> <see cref="Regex"/>. First letter is always capitalized, initial full text is tested (and replaced where matched) 
    /// against <see cref="Substitutions"/>, then each word is tested (and replaced where matched) against <see cref="Substitutions"/> and first letter lowercased where remainder of word is lowercase. Finally, the last word in the
    /// initial text is tested against the <see cref="LastWordRemovals"/> and where matched the final word will be removed.</remarks>
    public static string? SentenceCaseConversion(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Make sure the first character is always upper case.
        if (char.IsLower(text[0]))
            text = string.Create(text.Length, text, static (span, t) =>
            {
                span[0] = char.ToUpper(t[0], CultureInfo.InvariantCulture);
                t.AsSpan(1).CopyTo(span[1..]);
            });

        // Check if there is a one-to-one substitution.
        ref var value = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrNullRef(Substitutions, text);
        if (!Unsafe.IsNullRef(ref value))
            return value;

        // Determine whether last word should be removed, then go through each word and substitute.
        var parts = SplitIntoWords(text);
        var removeLastWord = parts.Length > 0 && LastWordRemovals.Contains(parts[^1]);

        for (int i = 0; i < parts.Length; i++)
        {
            if (Substitutions.TryGetValue(parts[i], out var iscs))
                parts[i] = iscs;

            if (i > 0 && parts[i].Length >= 2)
                parts[i] = LowercaseFirstWhereRestIsLower(parts[i]);
        }

        // Rejoin the words back into the final sentence.
        return string.Join(" ", parts, 0, parts.Length - (removeLastWord ? 1 : 0));
    }

    /// <summary>
    /// Lowercase the first character of the specified <paramref name="text"/> where the remainder of the text is all lowercase; otherwise returns the original value.
    /// </summary>
    private static string LowercaseFirstWhereRestIsLower(string text)
    {
        if (string.IsNullOrEmpty(text) || text.Length == 1)
            return text;

        ReadOnlySpan<char> span = text.AsSpan();

        // Check remainder is all lowercase.
        for (int i = 1; i < span.Length; i++)
        {
            if (!char.IsLower(span[i]))
                return text; // no allocation
        }

        char firstLower = char.ToLowerInvariant(span[0]);

        // If already lowercase, don't allocate.
        if (firstLower == span[0])
            return text;

        // Allocate only when we actually need to change.
        return string.Create(span.Length, (text, firstLower), static (dest, state) =>
        {
            dest[0] = state.firstLower;
            state.text.AsSpan()[1..].CopyTo(dest[1..]);
        });
    }

    /// <summary>
    /// Converts a <see cref="string"/> into PascalCase.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> in PascalCase.</returns>
    /// <remarks>For example '<c>employee_id</c>' or '<c>EmployeeId</c>' would return '<c>EmployeeId</c>'.</remarks>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToPascalCase(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var parts = SplitIntoWords(text);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;

            // Uppercase first letter, lowercase rest.
            if (parts[i].Length == 1)
                parts[i] = char.ToUpperInvariant(parts[i][0]).ToString();
            else
            {
                parts[i] = string.Create(parts[i].Length, parts[i], static (span, word) =>
                {
                    span[0] = char.ToUpperInvariant(word[0]);
                    for (int j = 1; j < word.Length; j++)
                    {
                        span[j] = char.ToLowerInvariant(word[j]);
                    }
                });
            }
        }

        return string.Concat(parts);
    }

    /// <summary>
    /// Converts a <see cref="string"/> into camelCase.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> in camelCase.</returns>
    /// <remarks>For example '<c>EmployeeId</c>' would return '<c>employeeId</c>'.</remarks>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToCamelCase(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var parts = SplitIntoWords(text);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;

            if (i == 0)
                parts[i] = parts[i].ToLowerInvariant(); // First word: all lowercase.
            else
            {
                // Other words: uppercase first letter, lowercase rest.
                if (parts[i].Length == 1)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]).ToString();
                }
                else
                {
                    parts[i] = string.Create(parts[i].Length, parts[i], static (span, word) =>
                    {
                        span[0] = char.ToUpperInvariant(word[0]);
                        for (int j = 1; j < word.Length; j++)
                        {
                            span[j] = char.ToLowerInvariant(word[j]);
                        }
                    });
                }
            }
        }

        return string.Concat(parts);
    }

    /// <summary>
    /// Converts a <see cref="string"/> into kebab-case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> in kebab-case.</returns>
    /// <remarks>For example '<c>EmployeeId</c>' would return '<c>employee-id</c>'.</remarks>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToKebabCase(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var parts = SplitIntoWords(text);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            parts[i] = parts[i].ToLowerInvariant();
        }

        return string.Join("-", parts);
    }

    /// <summary>
    /// Converts a <see cref="string"/> into snake_case.
    /// </summary>
    /// <param name="text">The text to convert.</param>
    /// <returns>The <see cref="string"/> in snake_case.</returns>
    /// <remarks>For example '<c>EmployeeId</c>' would return '<c>employee_id</c>'.</remarks>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? ToSnakeCase(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var parts = SplitIntoWords(text);
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].Length == 0) continue;
            parts[i] = parts[i].ToLowerInvariant();
        }

        return string.Join("_", parts);
    }

    /// <summary>
    /// Gets or sets the sentence case substitutions <see cref="Dictionary{TKey, TValue}"/> where the key is the originating (input) text and the value the corresponding substitution sentence case text.
    /// </summary>
    /// <remarks>Defaults with the following entry: key '<c>Id</c>' and value '<c>Identifier</c>'.
    /// <para>This substitution applies to all words in the text with the exception of the last where it matches the <see cref="LastWordRemovals"/>.</para></remarks>
    public static Dictionary<string, string> Substitutions { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Id", "Identifier" }, { "Etag", "ETag" } };

    /// <summary>
    /// Gets or sets the sentence case last word removal list; i.e. where there is more than one word, and there is a match, the word will be removed.
    /// </summary>
    /// <remarks>Defaults with the following entry: '<c>Id</c>'.
    /// <para>For example a value of '<c>EmployeeId</c>' would return just '<c>Employee</c>'.</para></remarks>
    public static HashSet<string> LastWordRemovals { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Id" };
}