namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class PersonApi_MvcQueryTests : PersonApi_QueryTestsBase
{
    public override string Route => "api/people";
}