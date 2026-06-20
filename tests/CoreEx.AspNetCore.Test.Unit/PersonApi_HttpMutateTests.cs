namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class PersonApi_HttpMutateTests : PersonApi_MutateTestsBase
{
    public override string Route => "api/persons";
}