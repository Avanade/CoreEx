namespace CoreEx.Security;

/// <summary>
/// Represents the type of authentication used.
/// </summary>
/// <remarks>See <see href="https://github.com/cloudevents/spec/blob/main/cloudevents/extensions/authcontext.md"/> for inspiration.</remarks>
public enum AuthenticationType
{
    /// <summary>
    /// Indicates that the authentication type is not known or not specified.
    /// </summary>
    Unknown,

    /// <summary>
    /// Indicates that the authentication type is unauthenticated; i.e. no authentication has been performed (anonymous).
    /// </summary>
    Unauthenticated,

    /// <summary>
    /// Indicates that the authentication type is application based; i.e. is the end user of an application (e.g. Facebook, Google, etc.).
    /// </summary>
    ApplicationUser,

    /// <summary>
    /// Indicates that the authentication type is identity-provider based; i.e. is a specific user (e.g. username/password, SSO, etc.).
    /// </summary>
    AccountUser,

    /// <summary>
    /// Indicates that the authentication type is system based; i.e. is a service account or system identity (e.g. background service, database, daemon, etc.).
    /// </summary>
    SystemUser
}