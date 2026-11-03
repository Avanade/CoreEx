namespace CoreEx.AspNetCore;

public static partial class AspNetCoreExtensions
{
    private static readonly LText _acceptInvalid = new("CoreEx.AspNetCore.WebApi.AcceptInvalid", "The Accept header is invalid.");
    private static readonly LText _acceptNotSupported = new("CoreEx.AspNetCore.WebApi.AcceptNotSupported", "The requested response media type is not acceptable.");

    /// <summary>
    /// Immediately asserts that the request accepts the operation's generated JSON response media type.
    /// </summary>
    /// <typeparam name="TRequestOptions">The <see cref="WebApiOptionsBase"/> type.</typeparam>
    /// <param name="requestOptions">The operation options.</param>
    /// <returns>The options for fluent-style method-chaining.</returns>
    /// <remarks>Set any <see cref="WithContentType{TRequestOptions}"/> override before calling this guard, and call it before business execution. A missing or empty Accept header accepts any representation.
    /// Uses the owning WebApi's default, or application/json for independently constructed options. This is validation, not negotiation: it does not select a representation, inspect explicit results,
    /// or change error rendering. Changing the content type afterward requires another check. Adds Vary: Accept without replacing existing values.</remarks>
    /// <exception cref="ValidationException">Thrown with status 406 where no acceptable range matches, or status 400 for a malformed header.</exception>
    public static TRequestOptions WithAccept<TRequestOptions>(this TRequestOptions requestOptions) where TRequestOptions : WebApiOptionsBase
    {
        requestOptions.ThrowIfNull();
        var response = requestOptions.Request.HttpContext.Response;
        var vary = response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
        if (!vary.Any(v => v == "*" || StringComparer.OrdinalIgnoreCase.Equals(v, HeaderNames.Accept)))
            response.Headers.AppendCommaSeparatedValues(HeaderNames.Vary, HeaderNames.Accept);

        var accept = requestOptions.Request.Headers.Accept;
        if (accept.Count == 0 || accept.All(string.IsNullOrWhiteSpace))
            return requestOptions;

        if (!MediaTypeHeaderValue.TryParseStrictList(accept.Select(value => value ?? string.Empty).ToArray(), out var ranges))
            throw new ValidationException(_acceptInvalid);

        var candidate = MediaTypeHeaderValue.Parse(requestOptions.ContentType ?? requestOptions.WebApi?.DefaultJsonMediaType ?? MediaTypeNames.Application.Json);
        if (!candidate.Charset.HasValue)
            candidate.Charset = "utf-8";

        var bestSpecificity = (Type: -1, Parameters: -1);
        var bestQuality = 0d;
        foreach (var range in ranges)
        {
            var qualityParameters = range.Parameters.Where(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name.Value, "q")).ToArray();
            if (qualityParameters.Length > 1 || (qualityParameters.Length == 1
                && !ParameterRegex().IsMatch(qualityParameters[0].Value.ToString())))
                throw new ValidationException(_acceptInvalid);

            var specificity = GetAcceptSpecificity(candidate, range);
            if (specificity.CompareTo(bestSpecificity) > 0)
            {
                bestSpecificity = specificity;
                bestQuality = range.Quality ?? 1;
            }
            else if (specificity == bestSpecificity)
                bestQuality = Math.Max(bestQuality, range.Quality ?? 1);
        }

        if (bestSpecificity.Type < 0 || bestQuality == 0)
            throw new ValidationException(_acceptNotSupported).WithStatusCode(HttpStatusCode.NotAcceptable);

        return requestOptions;
    }

    /// <summary>
    /// Matches exact media types or standard wildcards and media parameters, without treating structured suffixes as equivalent media types.
    /// </summary>
    private static (int Type, int Parameters) GetAcceptSpecificity(MediaTypeHeaderValue candidate, MediaTypeHeaderValue range)
    {
        var candidateType = candidate.MediaType.ToString();
        var rangeType = range.MediaType.ToString();
        var type = StringComparer.OrdinalIgnoreCase.Equals(candidateType, rangeType) ? 2
            : StringComparer.OrdinalIgnoreCase.Equals(candidateType[..candidateType.IndexOf('/')] + "/*", rangeType) ? 1
            : rangeType == "*/*" ? 0 : -1;

        if (type < 0)
            return (-1, -1);

        var count = 0;
        foreach (var parameter in range.Parameters)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(parameter.Name.Value, "q"))
                break;

            var comparison = StringComparer.OrdinalIgnoreCase.Equals(parameter.Name.Value, "charset") ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            if (!candidate.Parameters.Any(p => StringComparer.OrdinalIgnoreCase.Equals(p.Name.Value, parameter.Name.Value)
                && comparison.Equals(p.Value.ToString().Trim('"'), parameter.Value.ToString().Trim('"'))))
                return (-1, -1);

            count++;
        }

        return (type, count);
    }

    [GeneratedRegex(@"\A(?:0(?:\.[0-9]{0,3})?|1(?:\.0{0,3})?)\z")]
    private static partial Regex ParameterRegex();
}
