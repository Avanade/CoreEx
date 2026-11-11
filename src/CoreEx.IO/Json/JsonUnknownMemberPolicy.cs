namespace CoreEx.IO.Json;

/// <summary>Specifies treatment of undeclared direct members in mapped JSON objects.</summary>
public enum JsonUnknownMemberPolicy
{
    /// <summary>Rejects unknown members without echoing their names or values.</summary>
    Reject,

    /// <summary>Ignores unknown members after syntax, duplicate, UTF-8, and parser-depth checks.</summary>
    Ignore
}
