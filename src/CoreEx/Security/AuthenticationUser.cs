namespace CoreEx.Security;

/// <summary>
/// Represents a user within the system.
/// </summary>
/// <remarks>It is intended that this is extended to enable </remarks>
public record class AuthenticationUser : IReadOnlyIdentifier<string?>
{
    /// <summary>
    /// Represents an unknown user; i.e. a user that has not been authenticated or the authentication type is not known.
    /// </summary>
    public static AuthenticationUser Unknown { get; set; } = new AuthenticationUser { Type = AuthenticationType.Unknown, UserName = nameof(Unknown) };

    /// <summary>
    /// Represents an anonymous user; i.e. a user that has not been authenticated.
    /// </summary>
    public static AuthenticationUser Anonymous { get; set; } = new AuthenticationUser { Type = AuthenticationType.Unauthenticated, UserName = nameof(Anonymous) };

    /// <summary>
    /// Represents the currently authenticated environment user.
    /// </summary>
    public static AuthenticationUser EnvironmentUser { get; set; } = new AuthenticationUser 
    { 
        Type = AuthenticationType.AccountUser,
        Id = Environment.UserDomainName is null ? Environment.UserName : Environment.UserDomainName + "\\" + Environment.UserName,
        UserName = Environment.UserDomainName is null ? Environment.UserName : Environment.UserDomainName + "\\" + Environment.UserName 
    };

    /// <summary>
    /// Gets the type of authentication used for the user.
    /// </summary>
    public required AuthenticationType Type { get; init; }

    /// <summary>
    /// Gets the unique identifier of the user principle; such as email, service account, etc.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the user name that is used for the likes of auditing etc.
    /// </summary>
    public required string UserName { get; init => field = value.ThrowIfNullOrEmpty(); }

    /// <inheritdoc/>
    /// <remarks>Returns the <see cref="UserName"/>.</remarks>
    public override string ToString() => UserName;
}