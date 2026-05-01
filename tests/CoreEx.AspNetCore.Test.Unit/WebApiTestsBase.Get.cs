using CoreEx.Data;
using CoreEx.Entities;
using CoreEx.Http;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    private readonly string[] _helloWorld = ["Hello", "World"];

    [Test]
    public void Get_Null_As_Not_Found()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(Test.CreateHttpRequest(HttpMethod.Get), (ro, ct) => Task.FromResult<string?>(null)))
            .ToHttpResponseMessageAssertor()
            .AssertNotFound();
    }

    [Test]
    public void Get_OK()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult("Hello World")))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }

    [Test]
    public void Get_Collection_With_Items()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(_helloWorld)))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"");
    }

    [Test]
    public void Get_Collection_Empty()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Array.Empty<string>())))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("[]")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"4f53cda18c2b\"");
    }

    [Test]
    public void Get_ItemsResult_With_Items()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(_helloWorld))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"");
    }

    [Test]
    public void Get_ItemsResult_Empty()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>())))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("[]")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"4f53cda18c2b\"");
    }

    [Test]
    public void Get_ItemsResult_With_Total_Count_NoPaging()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(_helloWorld).WithTotalCount(50))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"")
            .AssertNoNamedHeader(HttpNames.PagingSkipHeaderName)
            .AssertNoNamedHeader(HttpNames.PagingTakeHeaderName)
            .AssertNoNamedHeader(HttpNames.PagingTotalCountHeaderName);
    }

    [Test]
    public void Get_ItemsResult_With_Total_Count_WithPaging()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(_helloWorld, PagingArgs.CreateWithCount()).WithTotalCount(50))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"")
            .AssertNamedHeader(HttpNames.PagingSkipHeaderName, "0")
            .AssertNamedHeader(HttpNames.PagingTakeHeaderName, PagingArgs.DefaultTake.ToString())
            .AssertNamedHeader(HttpNames.PagingTotalCountHeaderName, "50");
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links1()
    {
        // Get a mid page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=2&$take=5&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(["a", "b", "c", "d", "e"], ro.PagingArgs).WithTotalCount(50))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=0&$take=2&$count&abc=def>; rel=\"prev\"", "</test?xyz=uvw&$skip=7&$take=5&$count&abc=def>; rel=\"next\""]);
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links2()
    {
        // Get the first page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=0&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(["a", "b"], ro.PagingArgs).WithTotalCount(50))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=2&$take=2&$count&abc=def>; rel=\"next\""]);
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links3()
    {
        // Get the last page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=50&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>(["a"], ro.PagingArgs).WithTotalCount(51))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=48&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links4()
    {
        // Get a page with way too much skip - total count will help.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=250&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>([], ro.PagingArgs).WithTotalCount(50))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=48&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links5()
    {
        // Get a page with way too much skip - can only guess the previous.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=250&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>([], ro.PagingArgs))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=248&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void Get_ItemsResult_Prev_Next_Paging_Links6()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ItemsResult<string>([], ro.PagingArgs))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.Should().NotContainKeys("Link");
    }

    [Test]
    public void Get_Person_Default_ETag()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Person.GetPerson())))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe","age":30,"etag":"1f5d2ae86bc2"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"1f5d2ae86bc2\"");
    }

    [Test]
    public void Get_Person_Specified_ETag()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Person.GetPerson("abcdefg"))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe","age":30,"etag":"abcdefg"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void Get_Person_Not_Modified()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.Headers.IfNoneMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Person.GetPerson("abcdefg"))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertNotModified()
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void Get_Person_Fields_Include()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.QueryString = hr.QueryString.Add(HttpNames.IncludeFieldsQueryStringName, "firstname,LastName");

        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Person.GetPerson())))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"ddbd7f44c126\"");
    }

    [Test]
    public void Get_Person_Fields_Exclude()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.QueryString = hr.QueryString.Add(HttpNames.ExcludeFieldsQueryStringName, "firstname,LastName");

        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(Person.GetPerson())))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"age":30,"etag":"1f5d2ae86bc2"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"483e8079ddab\"");
    }

    [Test]
    public void Get_ValueResult_Default_StatusCode()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ValueResult<string>("Hello World"))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }

    [Test]
    public void Get_ValueResult_Specified_StatusCode()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetAsync(hr, (ro, ct) => Task.FromResult(new ValueResult<string>("Hello World", System.Net.HttpStatusCode.Accepted))))
            .ToHttpResponseMessageAssertor(hr)
            .Assert(System.Net.HttpStatusCode.Accepted)
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }
}