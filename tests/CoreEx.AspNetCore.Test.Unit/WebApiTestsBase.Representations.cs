namespace CoreEx.AspNetCore.Test.Unit;

partial class WebApiTestsBase<TWebApi, TResult>
{
    private const string _specializedMediaType = "application/vnd.coreex.example+json";

    [TestCase("GET")]
    [TestCase("POST")]
    [TestCase("PUT")]
    [TestCase("PATCH")]
    [TestCase("DELETE")]
    public void Representation_InstanceDefault_AllVerbs(string method)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method));
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            Func<WebApiResponseOptions<string>, CancellationToken, Task<string>> function = (_, _) => Task.FromResult("response");
            return method switch
            {
                "GET" => w.GetAsync(request, (_, _) => Task.FromResult("response")),
                "POST" => w.PostAsync(request, function),
                "PUT" => w.PutAsync(request, function),
                "PATCH" => w.PatchAsync(request, function),
                _ => w.DeleteAsync(request, function)
            };
        }).ToHttpResponseMessageAssertor(request).AssertContentType(_specializedMediaType).GetContent().Should().Be("\"response\"");
    }

    [TestCase(null, "application/vnd.coreex.example+json")]
    [TestCase("application/vnd.operation+json", "application/vnd.operation+json")]
    [TestCase("application/vnd.operation+json; charset=utf-8", "application/vnd.operation+json")]
    [TestCase("application/vnd.operation+json; charset=\"utf-8\"", "application/vnd.operation+json")]
    public void Representation_OperationOverride(string? contentType, string expected)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.GetAsync(request, (ro, _) =>
            {
                ro.WithContentType("application/vnd.reset+json").WithContentType(contentType);
                return Task.FromResult("response");
            });
        }).ToHttpResponseMessageAssertor(request).AssertContentType(expected).GetContent().Should().Be("\"response\"");
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("invalid")]
    [TestCase("application/json\r\nX-Injected: true")]
    [TestCase("application/*")]
    [TestCase("application/*+json")]
    [TestCase("application/json, text/plain")]
    [TestCase("application/json; charset=utf-16")]
    [TestCase("application/json; charset=utf-8; charset=utf-16")]
    public void Representation_InvalidMediaTypes(string contentType)
    {
        using var services = new ServiceCollection().BuildServiceProvider();
        var webApi = ActivatorUtilities.CreateInstance<TWebApi>(services);
        var options = new WebApiOptions(Test.CreateHttpRequest(HttpMethod.Get));
        Assert.Throws<ArgumentException>(() => webApi.DefaultJsonMediaType = contentType);
        Assert.Throws<ArgumentException>(() => options.WithContentType(contentType));
        webApi.DefaultJsonMediaType.Should().Be(MediaTypeNames.Application.Json);
        options.ContentType.Should().BeNull();
    }

    [Test]
    public void Representation_OptionsCopies_PreserveOverride()
    {
        var options = new WebApiResponseOptions<string>(Test.CreateHttpRequest(HttpMethod.Get)).WithContentType(_specializedMediaType);
        new WebApiOptionsBase(options).ContentType.Should().Be(_specializedMediaType);
        new WebApiRequestResponseOptions<string, string>(options, "request").ContentType.Should().Be(_specializedMediaType);
    }

    [TestCase(30)]
    [TestCase(40)]
    public void Representation_MergePatch_PreservesOverride(int age)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Patch, "test", $"{{\"age\":{age}}}", r => r.ContentType = CoreEx.Http.HttpNames.MergePatchJsonMediaTypeName);
        request.Headers.IfMatch = "\"abcdef\"";
        Test.Type<TWebApi>().Run(w => w.PatchAsync<Person>(request,
            (ro, _) =>
            {
                ro.WithContentType(_specializedMediaType);
                return Task.FromResult<Person?>(Person.GetPerson("abcdef"));
            },
            (ro, _) =>
            {
                ro.ContentType.Should().Be(_specializedMediaType);
                return Task.FromResult(ro.Value);
            }))
            .ToHttpResponseMessageAssertor(request).AssertOK().AssertContentType(_specializedMediaType);
    }

    [TestCase("HEAD", HttpStatusCode.OK)]
    [TestCase("GET", HttpStatusCode.NoContent)]
    [TestCase("GET", HttpStatusCode.NotModified)]
    public void Representation_BodylessResponses(string method, HttpStatusCode statusCode)
    {
        var request = Test.CreateHttpRequest(new HttpMethod(method));
        var response = Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.GetAsync(request, (ro, _) =>
            {
                ro.WithContentType("application/vnd.operation+json");
                return Task.FromResult("response");
            }, statusCode);
        }).ToHttpResponseMessageAssertor(request).Assert(statusCode);
        response.GetContent().Should().BeEmpty();
        response.Response.Content.Headers.ContentType.Should().BeNull();
    }

    [Test]
    public void Representation_ConditionalGet_RemainsBodyless()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        request.Headers.IfNoneMatch = "\"07cf55095ef8\"";
        var response = Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.GetAsync(request, (ro, _) => Task.FromResult("Hello World"));
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.NotModified);
        response.GetContent().Should().BeEmpty();
    }

    [Test]
    public async Task Representation_ExplicitResult_IsUntouched()
    {
        object explicitResult = typeof(TResult) == typeof(Microsoft.AspNetCore.Mvc.IActionResult)
            ? new Microsoft.AspNetCore.Mvc.ContentResult { Content = "explicit", ContentType = "text/example" }
            : Microsoft.AspNetCore.Http.Results.Content("explicit", "text/example");
        using var services = new ServiceCollection().BuildServiceProvider();
        var webApi = ActivatorUtilities.CreateInstance<TWebApi>(services);
        webApi.DefaultJsonMediaType = _specializedMediaType;
        var result = await webApi.GetAsync(Test.CreateHttpRequest(HttpMethod.Get), (ro, _) =>
        {
            ro.WithContentType("application/vnd.operation+json");
            return Task.FromResult((TResult)explicitResult);
        });
        result.Should().BeSameAs(explicitResult);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Representation_DefaultErrors_IgnoreSuccessMediaType(bool unexpected)
    {
        var request = Test.CreateHttpRequest(HttpMethod.Get);
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            w.ConvertUnhandledExceptionsToProblemDetails = true;
            return w.GetAsync<string>(request, (ro, _) =>
            {
                ro.WithContentType("application/vnd.operation+json");
                throw unexpected ? new InvalidOperationException("private diagnostic") : new NotFoundException();
            });
        }).ToHttpResponseMessageAssertor(request)
            .Assert(unexpected ? HttpStatusCode.InternalServerError : HttpStatusCode.NotFound)
            .AssertContentType(MediaTypeNames.Application.ProblemJson);
    }

    [Test]
    public void Representation_ResponseOverride_DoesNotChangeRequestParsing()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Post, "test", """{"firstName":"Example"}""", r => r.ContentType = MediaTypeNames.Application.Json);
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.PostAsync<Person, Person>(request, (ro, _) =>
            {
                ro.WithContentType("application/vnd.operation+json");
                ro.Value.FirstName.Should().Be("Example");
                return Task.FromResult(ro.Value);
            });
        }).ToHttpResponseMessageAssertor(request).AssertCreated().AssertContentType("application/vnd.operation+json");
        request.ContentType.Should().Be(MediaTypeNames.Application.Json);
    }

    [Test]
    public void Representation_UnsupportedMergePatch_RemainsPlainText()
    {
        var request = Test.CreateHttpRequest(HttpMethod.Patch, "test", "{}", r => r.ContentType = MediaTypeNames.Application.Json);
        Test.Type<TWebApi>().Run(w =>
        {
            w.DefaultJsonMediaType = _specializedMediaType;
            return w.PatchAsync<Person>(request, (_, _) => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException());
        }).ToHttpResponseMessageAssertor(request).Assert(HttpStatusCode.UnsupportedMediaType).AssertContentType(MediaTypeNames.Text.Plain);
    }

    [Test]
    public void Representation_UnexpectedThrownException_DefaultPropagates()
    {
        Test.Type<TWebApi>()
            .Run(w => w.PostAsync(Test.CreateHttpRequest(HttpMethod.Post), (_, _) => throw new InvalidOperationException("private diagnostic")))
            .AssertException<InvalidOperationException>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Representation_UnexpectedReturnedException_DefaultPropagates(bool typed)
    {
        Test.Type<TWebApi>()
            .Run(w => typed
                ? w.GetWithResultAsync<string>(Test.CreateHttpRequest(HttpMethod.Get), (_, _) => Task.FromResult<Result<string>>(new InvalidOperationException("private diagnostic")))
                : w.PostWithResultAsync(Test.CreateHttpRequest(HttpMethod.Post), (_, _) => Task.FromResult(Result.Fail(new InvalidOperationException("private diagnostic")))))
            .AssertException<InvalidOperationException>();
    }
}
