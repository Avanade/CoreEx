using CoreEx.AspNetCore.Test.Api.Entities;
using CoreEx.AspNetCore.Test.Api.Services;
using CoreEx.Http;

namespace CoreEx.AspNetCore.Test.Unit;

public abstract class PersonApi_QueryTestsBase : WithApiTester<Api.Program>
{
    public abstract string Route { get; }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        PersonService.Reset();
        PersonService2.Reset();
    }

    [Test]
    public void Get_Found()
    {
        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .AssertValue(new Person
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                Birthday = new DateOnly(1980, 1, 1),
                GenderSid = "M",
            }, "etag")
            .AssertETagHeader();
    }

    [Test]
    public void Get_Found_IncludeText()
    {
        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1?$text")
            .AssertOK()
            .AssertValue(new Person
            {
                Id = "1",
                FirstName = "John",
                LastName = "Doe",
                Birthday = new DateOnly(1980, 1, 1),
                GenderSid = "M",
                GenderText = "Male"
            }, "etag")
            .AssertETagHeader();
    }

    [Test]
    public void Get_NotFound()
    {
        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/0")
            .AssertNotFound();
    }

    [Test]
    public void Get_NotModified()
    {
        var v = Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1")
            .AssertOK()
            .Value!;

        Test.Http<Person?>()
            .Run(HttpMethod.Get, $"{Route}/1", r => r.WithIfNoneMatch(v.ETag))
            .AssertNotModified();
    }

    [Test]
    public void GetByQuery_Default()
    {
        Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route)
            .AssertOK()
            .AssertJsonFromResource("Person_GetByQuery_Default.json", "etag")
            .AssertNamedHeader(HttpNames.PagingSkipHeaderName, "0")
            .AssertNamedHeader(HttpNames.PagingTakeHeaderName, "25")
            .AssertETagHeader();
    }

    [Test]
    public void GetByQuery_Paging()
    {
        Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithPaging(1, 2, true))
            .AssertOK()
            .AssertJsonFromResource("Person_GetByQuery_Paging.json", "etag")
            .AssertNamedHeader(HttpNames.PagingSkipHeaderName, "1")
            .AssertNamedHeader(HttpNames.PagingTakeHeaderName, "2")
            .AssertNamedHeader(HttpNames.PagingTotalCountHeaderName, "4")
            .AssertNamedHeader("Link", $"</{Route}?$skip=0&$take=1&$count=true>; rel=\"prev\", </{Route}?$skip=3&$take=2&$count=true>; rel=\"next\"")
            .AssertETagHeader();
    }

    [Test]
    public void GetByQuery_WithFields()
    {
        Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithFields("firstname", "lastname"))
            .AssertOK()
            .AssertJsonFromResource("Person_GetByQuery_IncludeFields.json");
    }

    [Test]
    public void GetByQuery_WithoutFields()
    {
        Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithoutFields("id", "birthday", "gender", "etag"))
            .AssertOK()
            .AssertJsonFromResource("Person_GetByQuery_IncludeFields.json");
    }

    [Test]
    public void GetByQuery_Filter_Eq()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery("firstname eq 'John'"))
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(1);
        v[0].Id.Should().Be("1");
    }

    [Test]
    public void GetByQuery_Filter_EndsWith()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery("endswith(firstname, 'e')"))
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(2);
        v[0].Id.Should().Be("2");
        v[1].Id.Should().Be("3");
    }

    [Test]
    public void GetByQuery_Filter_Gender()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery("gender in ('m')"))
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(2);
        v[0].Id.Should().Be("1");
        v[1].Id.Should().Be("4");
    }

    [Test]
    public void GetByQuery_OrderBy_FirstName()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery(filter: "gender in ('m')", orderBy: "firstname"))
            .AssertOK()
            .Value;

        v.Should().NotBeNull();
        v.Should().HaveCount(2);
        v[0].Id.Should().Be("4");
        v[1].Id.Should().Be("1");
    }

    [Test]
    public void GetByQuery_Filter_Error()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery("gender gt 'm'"))
            .AssertBadRequest()
            .AssertContentType("application/problem+json")
            .AssertJsonFromResource("Person_GetByQuery_FilterError.json", "traceid");
    }

    [Test]
    public void GetByQuery_OrderBy_Error()
    {
        var v = Test.Http<Person[]>()
            .Run(HttpMethod.Get, Route, r => r.WithQuery(orderBy: "eyecolor"))
            .AssertBadRequest()
            .AssertContentType("application/problem+json")
            .AssertJsonFromResource("Person_GetByQuery_OrderByError.json", "traceid");
    }
}