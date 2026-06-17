using CoreEx.RefData;

namespace CoreEx.AspNetCore.Test.Api.Entities;

[ReferenceData]
public partial class Gender : ReferenceData<Gender> { }

public class GenderCollection : ReferenceDataCollection<Gender> { }
