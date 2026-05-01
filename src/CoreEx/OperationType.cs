namespace CoreEx;

/// <summary>
/// Represents the <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> operation types (Create, Read, Update and Delete).
/// </summary>
public enum OperationType
{
    /// <summary>
    /// An <i>Unspecified</i> operation.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// A <i>Get</i> (keyed) operation.
    /// </summary>
    Get = 1,

    /// <summary>
    /// A <i>Create</i> operation.
    /// </summary>
    Create = 2,

    /// <summary>
    /// An <i>Update</i> operation.
    /// </summary>
    Update = 4,

    /// <summary>
    /// A <i>Delete</i> operation.
    /// </summary>
    Delete = 8,

    /// <summary>
    /// A <i>Query</i> operation (as distinct from a <see cref="Get"/>).
    /// </summary>
    Query = 16,
}