using CoreEx.RefData.Models;

namespace My.Hr.Business.Models;

public class USState: ReferenceDataBase<Guid>
{
    /// <summary>
    /// Gets or sets the 'RowVersion' column value.
    /// </summary>
    public byte[]? RowVersion { get; set; }

    /// <summary>
    /// Gets or sets the 'CreatedBy' column value.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the 'CreatedDate' column value.
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the 'UpdatedBy' column value.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the 'UpdatedDate' column value.
    /// </summary>
    public DateTime? UpdatedDate { get; set; }
}