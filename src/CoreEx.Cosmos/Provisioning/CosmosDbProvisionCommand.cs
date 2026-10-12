namespace CoreEx.Cosmos.Provisioning;

/// <summary>
/// Defines the provisioning commands (<see cref="CosmosDbProvisioner.RunAsync(CosmosDbProvisionCommand, CancellationToken)"/>); the <b>Cosmos DB</b> equivalent of the <c>DbEx</c> <c>MigrationCommand</c>.
/// </summary>
[Flags]
public enum CosmosDbProvisionCommand
{
    /// <summary>
    /// No command.
    /// </summary>
    None = 0,

    /// <summary>
    /// Deletes the database (and therefore all containers and data); a non-existent database is not an error.
    /// </summary>
    Drop = 1,

    /// <summary>
    /// Creates the database and any declared containers that do not already exist; existing containers (and their data) are left untouched.
    /// </summary>
    Create = 2,

    /// <summary>
    /// Replaces (deletes and recreates, so is therefore empty) every declared container. The database must already exist (it is not implicitly created; as per <c>DbEx</c> the creation must be specifically requested, e.g. <c>Create | Reset</c>); runs after <see cref="Create"/>.
    /// </summary>
    Reset = 4,

    /// <summary>
    /// Imports the seed data; a $-prefixed key is merged (upserted, re-runnable), otherwise inserted (a re-run conflicts).
    /// </summary>
    Data = 8,

    /// <summary>
    /// <see cref="Create"/> and <see cref="Data"/>.
    /// </summary>
    All = Create | Data,

    /// <summary>
    /// <see cref="Reset"/> and <see cref="Data"/>.
    /// </summary>
    ResetAndData = Reset | Data,

    /// <summary>
    /// <see cref="Drop"/> and <see cref="All"/>.
    /// </summary>
    DropAndAll = Drop | All
}
