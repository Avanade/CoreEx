using CoreEx.RefData;
using CoreEx.RefData.Models;

namespace My.Hr.Business.Models;

public class Gender : ReferenceDataBase<Guid> { }

public class GenderCollection : ReferenceDataCollection<Guid, Gender, GenderCollection> { }