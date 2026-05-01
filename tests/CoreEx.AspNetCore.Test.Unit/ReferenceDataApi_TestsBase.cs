using CoreEx.AspNetCore.Test.Api.Entities;

namespace CoreEx.AspNetCore.Test.Unit;

[Parallelizable]
public abstract class ReferenceDataApi_TestsBase : WithApiTester<Api.Program>
{
    public abstract string Route { get; }

    [Test]
    public void Gender_Get_All()
    {
        Test.Http<Gender[]>()
            .Run(HttpMethod.Get, $"{Route}/genders")
            .AssertOK()
            .AssertJsonFromResource("Gender_Get_All.json");
    }

    [Test]
    public void Gender_Get_Codes()
    {
        var v = Test.Http<Gender[]>()
            .Run(HttpMethod.Get, $"{Route}/genders?codes=F&codes=X")
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(1);
        v[0].Should().NotBeNull();
        v[0].Code.Should().Be("F");
    }

    [Test]
    public void Gender_Get_Text()
    {
        var v = Test.Http<Gender[]>()
            .Run(HttpMethod.Get, $"{Route}/genders?text=F*")
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(1);
        v[0].Should().NotBeNull();
        v[0].Code.Should().Be("F");
    }

    [Test]
    public void Gender_Get_Codes_IncludeInactive()
    {
        var v = Test.Http<Gender[]>()
            .Run(HttpMethod.Get, $"{Route}/genders?codes=F&codes=X&$inactive")
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(2);
        v[0].Should().NotBeNull();
        v[0].Code.Should().Be("F");
        v[1].Should().NotBeNull();
        v[1].Code.Should().Be("X");
    }
}