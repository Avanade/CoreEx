using CoreEx.RefData;
using CoreEx.RefData.Models;

namespace My.Hr.Business.Models;

public class USState : ReferenceDataBase<Guid> { }

public class USStateCollection : ReferenceDataCollection<Guid, USState, USStateCollection> { }