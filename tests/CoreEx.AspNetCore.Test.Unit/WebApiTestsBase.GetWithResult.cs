using CoreEx.Data;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Results;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [Test]
    public void GetWithResult_Null_As_Not_Found()
    {
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(Test.CreateHttpRequest(HttpMethod.Get), (ro, ct) => Task.FromResult(Result<string?>.Ok(null))))
            .ToHttpResponseMessageAssertor()
            .AssertNotFound();
    }

    [Test]
    public void GetWithResult_OK()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok("Hello World"))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }

    [Test]
    public void GetWithResult_Collection_With_Items()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(_helloWorld))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"");
    }

    [Test]
    public void GetWithResult_Collection_Empty()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Array.Empty<string>()))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("[]")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"4f53cda18c2b\"");
    }

    [Test]
    public void GetWithResult_ItemsResult_With_Items()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(_helloWorld)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue(_helloWorld)
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"d24bf63378b2\"");
    }

    [Test]
    public void GetWithResult_ItemsResult_Empty()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>()))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("[]")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"4f53cda18c2b\"");
    }

    [Test]
    public void GetWithResult_ItemsResult_With_Total_Count_NoPaging()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(_helloWorld).WithTotalCount(50)))))
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
    public void GetWithResult_ItemsResult_With_Total_Count_WithPaging()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(_helloWorld, PagingArgs.CreateWithCount()).WithTotalCount(50)))))
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
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links1()
    {
        // GetWithResult a mid page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=2&$take=5&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(["a", "b", "c", "d", "e"], ro.PagingArgs).WithTotalCount(50)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=0&$take=2&$count&abc=def>; rel=\"prev\"", "</test?xyz=uvw&$skip=7&$take=5&$count&abc=def>; rel=\"next\""]);
    }

    [Test]
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links2()
    {
        // GetWithResult the first page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=0&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(["a", "b"], ro.PagingArgs).WithTotalCount(50)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=2&$take=2&$count&abc=def>; rel=\"next\""]);
    }

    [Test]
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links3()
    {
        // GetWithResult the last page.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=50&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>(["a"], ro.PagingArgs).WithTotalCount(51)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=48&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links4()
    {
        // GetWithResult a page with way too much skip - total count will help.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=250&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>([], ro.PagingArgs).WithTotalCount(50)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=48&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links5()
    {
        // GetWithResult a page with way too much skip - can only guess the previous.
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$skip=250&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>([], ro.PagingArgs)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.GetValues("Link").Should().Contain(["</test?xyz=uvw&$skip=248&$take=2&$count&abc=def>; rel=\"prev\""]);
    }

    [Test]
    public void GetWithResult_ItemsResult_Prev_Next_Paging_Links6()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get, "test?xyz=uvw&$take=2&$count&abc=def");
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ItemsResult<string>([], ro.PagingArgs)))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .Response.Headers.Should().NotContainKeys("Link");
    }

    [Test]
    public void GetWithResult_Person_Default_ETag()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Person.GetPerson()))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe","age":30,"etag":"1f5d2ae86bc2"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"1f5d2ae86bc2\"");
    }

    [Test]
    public void GetWithResult_Person_Specified_ETag()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Person.GetPerson("abcdefg")))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe","age":30,"etag":"abcdefg"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void GetWithResult_Person_Not_Modified()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.Headers.IfNoneMatch = new EntityTagHeaderValue("\"abcdefg\"", true).ToString();

        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Person.GetPerson("abcdefg")))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertNotModified()
            .AssertETagHeader("\"abcdefg\"");
    }

    [Test]
    public void GetWithResult_Person_Fields_Include()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.QueryString = hr.QueryString.Add(HttpNames.IncludeFieldsQueryStringName, "firstname,LastName");

        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Person.GetPerson()))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"firstName":"John","lastName":"Doe"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"ddbd7f44c126\"");
    }

    [Test]
    public void GetWithResult_Person_Fields_Exclude()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        hr.QueryString = hr.QueryString.Add(HttpNames.ExcludeFieldsQueryStringName, "firstname,LastName");

        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(Person.GetPerson()))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertJson("""{"age":30,"etag":"1f5d2ae86bc2"}""")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"483e8079ddab\"");
    }

    [Test]
    public void GetWithResult_ValueResult_Default_StatusCode()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ValueResult<string>("Hello World")))))
            .ToHttpResponseMessageAssertor(hr)
            .AssertOK()
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }

    [Test]
    public void GetWithResult_ValueResult_Specified_StatusCode()
    {
        var hr = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>()
            .Run(async w => await w.GetWithResultAsync(hr, (ro, ct) => Task.FromResult(Result.Ok(new ValueResult<string>("Hello World", System.Net.HttpStatusCode.Accepted)))))
            .ToHttpResponseMessageAssertor(hr)
            .Assert(System.Net.HttpStatusCode.Accepted)
            .AssertValue("Hello World")
            .AssertContentType(MediaTypeNames.Application.Json)
            .AssertETagHeader("\"07cf55095ef8\"");
    }
}