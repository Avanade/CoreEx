namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    [TestCase(null, HttpStatusCode.OK)]
    [TestCase("", HttpStatusCode.OK)]
    [TestCase("*/*", HttpStatusCode.OK)]
    [TestCase("application/*", HttpStatusCode.OK)]
    [TestCase("application/json", HttpStatusCode.OK)]
    [TestCase("APPLICATION/JSON", HttpStatusCode.OK)]
    [TestCase("application/xml", HttpStatusCode.NotAcceptable)]
    [TestCase("text/xml", HttpStatusCode.NotAcceptable)]
    [TestCase("application/vnd.example+json", HttpStatusCode.NotAcceptable)]
    [TestCase("application/xml, application/json;q=0.1", HttpStatusCode.OK)]
    [TestCase("application/json;q=0", HttpStatusCode.NotAcceptable)]
    [TestCase("*/*;q=1, application/json;q=0", HttpStatusCode.NotAcceptable)]
    [TestCase("application/*;q=0, application/json;q=0.1", HttpStatusCode.OK)]
    [TestCase("application/*;q=0, */*;q=1", HttpStatusCode.NotAcceptable)]
    [TestCase("application/json;q=0, application/json;q=0.5", HttpStatusCode.OK)]
    [TestCase("application/json; charset=UTF-8", HttpStatusCode.OK)]
    [TestCase("application/json; charset=\"utf-8\"", HttpStatusCode.OK)]
    [TestCase("application/json; charset=utf-16", HttpStatusCode.NotAcceptable)]
    [TestCase("application/json; charset=utf-8;q=0, application/json;q=1", HttpStatusCode.NotAcceptable)]
    [TestCase("application/json; profile=other", HttpStatusCode.NotAcceptable)]
    [TestCase("application/json;q=1;extension=ignored", HttpStatusCode.OK)]
    [TestCase("invalid", HttpStatusCode.BadRequest)]
    [TestCase("application/json, invalid", HttpStatusCode.BadRequest)]
    [TestCase("application/json;q=wrong", HttpStatusCode.BadRequest)]
    [TestCase("application/json;q=1.1", HttpStatusCode.BadRequest)]
    [TestCase("application/json;q=-1", HttpStatusCode.BadRequest)]
    [TestCase("application/json;q=0.1234", HttpStatusCode.BadRequest)]
    [TestCase("application/json;q=0;q=1", HttpStatusCode.BadRequest)]
    public void Accept_ImmediateGuard(string? accept, HttpStatusCode expected)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        if (accept is not null)
            request.Headers.Accept = accept;

        var executed = false;
        Test.Type<TWebApi>().Run(w => w.GetAsync(request, (ro, _) =>
        {
            ro.WithAccept().Should().BeSameAs(ro);
            executed = true;
            return Task.FromResult("response");
        })).ToHttpResponseMessageAssertor(request).Assert(expected)
            .AssertContentType(expected == HttpStatusCode.OK ? MediaTypeNames.Application.Json : MediaTypeNames.Application.ProblemJson);
        executed.Should().Be(expected == HttpStatusCode.OK);
        request.HttpContext.Response.Headers.Vary.ToString().Should().Be("Accept");
    }

    [TestCase("GET")]
    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public void Accept_InstanceDefault_AllVerbs(string method)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method));
        request.Headers.Accept = _specializedMediaType;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            Func<WebApiResponseOptions<string>, CancellationToken, Task<string>> function = (ro, _) =>
            {
                ro.WithAccept();
                return Task.FromResult("response");
            };
            return method switch
            {
                "GET" => w.GetAsync(request, (ro, _) => { ro.WithAccept(); return Task.FromResult("response"); }),
                "POST" => w.PostAsync(request, function),
                "PUT" => w.PutAsync(request, function),
                "PATCH" => w.PatchAsync(request, function),
                _ => w.DeleteAsync(request, function)
            };
        }).ToHttpResponseMessageAssertor(request).AssertContentType(_specializedMediaType);
    }

    [TestCase("application/json", null, false)]
    [TestCase(_specializedMediaType, null, true)]
    [TestCase("application/vnd.operation+json", "application/vnd.operation+json", true)]
    [TestCase(_specializedMediaType, "application/vnd.operation+json", false)]
    [TestCase("application/vnd.operation+json; profile=\"example\"", "application/vnd.operation+json; profile=example", true)]
    [TestCase("application/vnd.operation+json; profile=other", "application/vnd.operation+json; profile=example", false)]
    public void Accept_UsesEffectiveRepresentation(string accept, string? contentType, bool accepted)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.Headers.Accept = accept;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.GetAsync(request, (ro, _) =>
            {
                ro.WithContentType("application/vnd.reset+json").WithContentType(contentType).WithAccept();
                return Task.FromResult("response");
            });
        }).ToHttpResponseMessageAssertor(request).Assert(accepted ? HttpStatusCode.OK : HttpStatusCode.NotAcceptable);
    }

    [TestCase("POST", false)]
    [TestCase("POST", true)]
    [TestCase("PUT", false)]
    [TestCase("PUT", true)]
    [TestCase("PATCH", false)]
    [TestCase("PATCH", true)]
    public void Accept_TypedRequestOptions_PreserveInstance(string method, bool deserialize)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method), "test", "\"request\"", r => r.ContentType = MediaTypeNames.Application.Json);
        request.Headers.Accept = _specializedMediaType;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            Func<WebApiRequestResponseOptions<string, string>, CancellationToken, Task<string>> function = (ro, _) =>
            {
                new WebApiOptionsBase(ro).WithAccept();
                return Task.FromResult(ro.Value);
            };
            return (method, deserialize) switch
            {
                ("POST", true) => w.PostAsync(request, function),
                ("POST", false) => w.PostAsync(request, "request", function),
                ("PUT", true) => w.PutAsync(request, function),
                ("PUT", false) => w.PutAsync(request, "request", function),
                ("PATCH", true) => w.PatchAsync(request, function),
                _ => w.PatchAsync(request, "request", function)
            };
        }).ToHttpResponseMessageAssertor(request).AssertContentType(_specializedMediaType);
    }

    [TestCase("POST")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public void Accept_StatusOnlyOptions_UseInstance(string method)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method));
        request.Headers.Accept = _specializedMediaType;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            Func<WebApiOptions, CancellationToken, Task> function = (ro, _) =>
            {
                ro.WithAccept();
                return Task.CompletedTask;
            };
            return method switch
            {
                "POST" => w.PostAsync(request, function),
                "PATCH" => w.PatchAsync(request, function),
                _ => w.DeleteAsync(request, function)
            };
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.NoContent).GetContent().Should().BeEmpty();
    }

    [TestCase("POST", false)]
    [TestCase("POST", true)]
    [TestCase("PUT", false)]
    [TestCase("PUT", true)]
    [TestCase("PATCH", false)]
    [TestCase("PATCH", true)]
    public void Accept_RequestOnlyOptions_UseInstance(string method, bool deserialize)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method), "test", "\"request\"", r => r.ContentType = MediaTypeNames.Application.Json);
        request.Headers.Accept = _specializedMediaType;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            Func<WebApiRequestOptions<string>, CancellationToken, Task> function = (ro, _) =>
            {
                ro.WithAccept();
                return Task.CompletedTask;
            };
            return (method, deserialize) switch
            {
                ("POST", true) => w.PostAsync(request, function),
                ("POST", false) => w.PostAsync(request, "request", function),
                ("PUT", true) => w.PutAsync(request, function),
                ("PUT", false) => w.PutAsync(request, "request", function),
                ("PATCH", true) => w.PatchAsync(request, function),
                _ => w.PatchAsync(request, "request", function)
            };
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.NoContent);
    }

    [Test]
    public void Accept_MergePatch_CopyPreservesInstance()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Patch, "test", """{"age":40}""", r => r.ContentType = CoreEx.Http.HttpNames.MergePatchJsonMediaTypeName);
        request.Headers.IfMatch = "\"abcdef\"";
        request.Headers.Accept = _specializedMediaType;
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.PatchAsync<Person>(request,
                (ro, _) => { ro.WithAccept(); return Task.FromResult<Person?>(Person.GetPerson("abcdef")); },
                (ro, _) => { ro.WithAccept(); return Task.FromResult(ro.Value); });
        }).ToHttpResponseMessageAssertor(request).AssertOK().AssertContentType(_specializedMediaType);
    }

    [Test]
    public void Accept_OptInOnly()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.Headers.Accept = "application/xml";
        Test.Type<TWebApi>().Run(w => w.GetAsync(request, (_, _) => Task.FromResult("response")))
            .ToHttpResponseMessageAssertor(request).AssertOK().AssertContentType(MediaTypeNames.Application.Json);
        request.HttpContext.Response.Headers.Vary.Should().BeEmpty();
    }

    [Test]
    public void Accept_MultipleHeaderLines()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.Headers.Accept = new Microsoft.Extensions.Primitives.StringValues(["application/xml", "application/json;q=0.5"]);
        new WebApiOptions(request).WithAccept();
    }

    [TestCase("HEAD", HttpStatusCode.OK)]
    [TestCase("GET", HttpStatusCode.NoContent)]
    [TestCase("GET", HttpStatusCode.NotModified)]
    public void Accept_GuardPreservesBodylessResponses(string method, HttpStatusCode statusCode)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method));
        request.Headers.Accept = "application/json";
        var response = Test.Type<TWebApi>().Run(w => w.GetAsync(request, (ro, _) =>
        {
            ro.WithAccept();
            return Task.FromResult("response");
        }, statusCode)).ToHttpResponseMessageAssertor(request).Assert(statusCode);
        response.GetContent().Should().BeEmpty();
        response.Response.Content.Headers.ContentType.Should().BeNull();
    }

    [Test]
    public void Accept_RecheckAfterOverride_IsImmediate()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.Headers.Accept = MediaTypeNames.Application.Json;
        var options = new WebApiOptions(request);
        options.WithAccept().WithContentType(_specializedMediaType);
        var error = Assert.Throws<ValidationException>(() => options.WithAccept());
        error?.StatusCode.Should().Be(HttpStatusCode.NotAcceptable);
    }

    [TestCase("Origin", "Origin,Accept")]
    [TestCase("accept", "accept")]
    [TestCase("*", "*")]
    public void Accept_VaryPreservesExistingValues(string existing, string expected)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.HttpContext.Response.Headers.Vary = existing;
        new WebApiOptions(request).WithAccept().WithAccept();
        request.HttpContext.Response.Headers.Vary.ToString().Replace(" ", "").Should().Be(expected);
    }
}
