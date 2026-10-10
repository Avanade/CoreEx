namespace Contoso.Products.Contracts;

[Contract]
public partial class Movement : IIdentifier<string?>, IETag, IChangeLog
{
    [ReadOnly(true)]
    public string? Id { get; set; }

    [NonNullable]
    public string? ReferenceId { get; set; }

    [ReferenceData<MovementKind>]
    [NonNullable]
    public partial string? KindCode { get; set; }

    [ReferenceData<MovementStatus>]
    [NonNullable]
    public partial string? StatusCode { get; set; }

    [NonNullable]
    public string? ProductId { get; set; }

    public decimal Quantity { get; set; }

    [ReferenceData<UnitOfMeasure>]
    [NonNullable]
    public partial string? UnitOfMeasureCode { get; set; }

    [ReadOnly(true)]
    public ChangeLog? ChangeLog { get; set; }

    [ReadOnly(true)]
    public string? ETag { get; set; }

    [JsonIgnore]
    public bool IsQuantityValidForKind => KindCode switch
    {
        MovementKind.Issue => Quantity < 0,
        MovementKind.Receive or MovementKind.Adjust => Quantity > 0,
        _ => false
    };
}