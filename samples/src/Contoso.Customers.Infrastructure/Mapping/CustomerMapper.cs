namespace Contoso.Customers.Infrastructure.Mapping;

public class CustomerMapper : BiDirectionMapper<Contracts.Customer, Persistence.Customer, CustomerMapper>
{
    // Unlike the EF-based sample domains, CosmosDbMappedContainer does not auto-copy Id/ETag/ChangeLog between the contract and persistence model itself - MapStandardFrom does that explicitly here
    // (deliberately excludes PartitionKey; see below).
    protected override Persistence.Customer OnMap(Contracts.Customer source)
    {
        var destination = new Persistence.Customer
        {
            FirstName = source.FirstName!,
            LastName = source.LastName!,
            Email = source.Email!,
            Phone = source.Phone,
            ShippingAddress = AddressMapper.To.Map(source.ShippingAddress),
            CustomerTypeCode = source.CustomerType?.Code,
            ContactMethodCode = source.ContactMethod?.Code,
            HasShopped = source.HasShopped
        };

        destination.MapStandardFrom(source);
        return destination;
    }

    protected override Contracts.Customer OnMap(Persistence.Customer source)
    {
        var destination = new Contracts.Customer
        {
            FirstName = source.FirstName,
            LastName = source.LastName,
            Email = source.Email,
            Phone = source.Phone,
            ShippingAddress = AddressMapper.From.Map(source.ShippingAddress),
            CustomerTypeCode = source.CustomerTypeCode,
            ContactMethodCode = source.ContactMethodCode,
            HasShopped = source.HasShopped
        };

        destination.MapStandardFrom(source);
        return destination;
    }
}
