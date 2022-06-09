using CoreEx.RefData;

namespace My.Hr.Business.Models;

public class Gender : ReferenceDataBase<Guid, Gender> 
{
    public static implicit operator Gender?(string? code) => ConvertFromCode(code);
}

public class GenderCollection : ReferenceDataCollectionBase<Guid, Gender, GenderCollection> { }