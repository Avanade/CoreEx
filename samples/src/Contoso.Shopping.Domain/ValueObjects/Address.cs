namespace Contoso.Shopping.Domain.ValueObjects;

public record class Address
{
    public required string? Street1 { get; init => field = value.ThrowIfNullOrEmpty(); }
    public string? Street2 { get; init => field = value.ThrowIfEmpty(); }
    public required string City { get; init => field = value.ThrowIfNullOrEmpty(); }
    public required string PostCode { get; init => field = value.ThrowIfNullOrEmpty(); }
    public required string State { get; init => field = value.ThrowIfNullOrEmpty(); }
}
