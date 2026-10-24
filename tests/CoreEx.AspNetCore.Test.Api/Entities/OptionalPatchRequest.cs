using CoreEx.Entities;
using CoreEx.RefData;

namespace CoreEx.AspNetCore.Test.Api.Entities;

/// <summary>
/// Represents a partial-update request used to verify optional contract and schema behavior.
/// </summary>
[Contract]
public partial class OptionalPatchRequest
{
    /// <summary>
    /// Gets or sets the non-nullable title when supplied.
    /// </summary>
    public Optional<string> Title { get; set; }

    /// <summary>
    /// Gets or sets the nullable notes when supplied.
    /// </summary>
    public Optional<string?> Notes { get; set; }

    /// <summary>
    /// Gets or sets the non-nullable count when supplied.
    /// </summary>
    public Optional<int> Count { get; set; }

    /// <summary>
    /// Gets or sets the nullable count when supplied.
    /// </summary>
    public Optional<int?> NullableCount { get; set; }

    /// <summary>
    /// Gets or sets the tags when supplied.
    /// </summary>
    public Optional<List<string>> Tags { get; set; }

    /// <summary>
    /// Gets or sets the gender reference-data code when supplied.
    /// </summary>
    [ReferenceData<Gender>(Text = false)]
    public partial Optional<string?> GenderCode { get; set; }
}
