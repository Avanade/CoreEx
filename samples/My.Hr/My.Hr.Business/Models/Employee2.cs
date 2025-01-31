using System.Diagnostics;

namespace My.Hr.Business.Models;

/// <summary>
/// Represents the Entity Framework (EF) model for database object 'Hr.Employee2'.
/// </summary>
public class Employee2 : IIdentifier<Guid>, IETag, ILogicallyDeleted
{
    /// <summary>
    /// Gets or sets the 'EmployeeId' column value.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the 'Email' column value.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Gets or sets the 'FirstName' column value.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the 'LastName' column value.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the 'GenderCode' column value.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    public Gender? Gender { get; set; }

    /// <summary>
    /// Gets or sets the 'Birthday' column value.
    /// </summary>
    public DateTime? Birthday { get; set; }

    /// <summary>
    /// Gets or sets the 'StartDate' column value.
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Gets or sets the 'TerminationDate' column value.
    /// </summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>
    /// Gets or sets the 'TerminationReasonCode' column value.
    /// </summary>
    public string? TerminationReasonCode { get; set; }

    /// <summary>
    /// Gets or sets the 'PhoneNo' column value.
    /// </summary>
    public string? PhoneNo { get; set; }

    /// <summary>
    /// Gets or sets the 'RowVersion' column value.
    /// </summary>
    public string? ETag { get; set; }

    /// <summary>
    ///  Gets or sets the 'IsDeleted' column value.
    /// </summary>
    public bool? IsDeleted { get; set; }
}

public class Employee2Collection : List<Employee2> { }

public class Employee2CollectionResult : CollectionResult<Employee2Collection, Employee2> { }