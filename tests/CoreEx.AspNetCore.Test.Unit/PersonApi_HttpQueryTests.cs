namespace CoreEx.AspNetCore.Test.Unit;

[TestFixture]
public class PersonApi_HttpQueryTests : PersonApi_QueryTestsBase
{
    public override string Route => "api/persons";
}