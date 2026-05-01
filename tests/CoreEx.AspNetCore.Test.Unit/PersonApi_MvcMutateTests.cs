namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class PersonApi_MvcMutateTests : PersonApi_MutateTestsBase
{
    public override string Route => "api/people";
}