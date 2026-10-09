using CoreEx.Entities;
using CoreEx.RefData;

namespace CoreEx.AspNetCore.Test.Api.Entities;

[Contract]
public partial class Person : IIdentifier<string?>, IETag, IChangeLog
{
    public string? Id { get; set; }

    [NonNullable]
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    [NonNullable]
    public DateOnly? Birthday { get; set; }

    [ReferenceData<Gender>]
    public partial string? GenderSid { get; set; }

    public string? ETag { get; set; }

    public ChangeLog? ChangeLog { get; set; }
}