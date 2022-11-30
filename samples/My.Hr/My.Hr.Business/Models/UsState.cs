using CoreEx.RefData.Extended;

namespace My.Hr.Business.Models;

public class USState : ReferenceDataBaseEx<Guid, USState>
{
    public static implicit operator USState?(string? code) => ConvertFromCode(code);
}

public class USStateCollection : ReferenceDataCollectionBase<Guid, USState, USStateCollection> { }