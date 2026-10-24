namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class HttpRequestBuilderTests
{
    private sealed class Widget
    {
        public string? Name { get; set; }
    }

    private sealed class DisposableResponse : HttpResponseMessage
    {
        public DisposableResponse(HttpStatusCode statusCode, HttpContent? content = null) : base(statusCode)
        {
            if (content is not null)
                Content = content;
        }

        public bool IsDisposed { get; private set; }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class InspectHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> inspect) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => inspect(request, cancellationToken);
    }

    private sealed class WidgetValidator(Func<Widget, CancellationToken, Task<CoreEx.Validation.IValidationResult<Widget>>> validate) : CoreEx.Validation.IValidator<Widget>
    {
        public int Calls { get; private set; }

        public Task<CoreEx.Validation.IValidationResult<Widget>> ValidateAsync(Widget value, CancellationToken cancellationToken = default)
        {
            Calls++;
            return validate(value, cancellationToken);
        }
    }

    private sealed class WidgetValidationResult(Widget? value, bool hasErrors = false, Exception? error = null) : CoreEx.Validation.IValidationResult<Widget>
    {
        public Widget? Value => value;
        public bool HasErrors => hasErrors;
        public CoreEx.Entities.MessageItemCollection? Messages => null;
        public Exception? ToException() => error;
        public CoreEx.Validation.IValidationResult ThrowOnError() => error is not null ? throw error : this;
        public CoreEx.Results.Result ToResult() => error is null ? CoreEx.Results.Result.Success : CoreEx.Results.Result.Fail(error);
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://example.com/") };

    [Test]
    public async Task ValidatedBuilder_SharesSingleUseExecution_AndRejectsLateConfiguration()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"name\":\"Widget\"}", Encoding.UTF8, "application/json")
        }));
        using var client = CreateClient(handler);
        var builder = client.Get("/api");
        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(w)));
        var validated = builder.WithValidator(validator);

        (await validated.GetValueAsync()).Name.Should().Be("Widget");
        Func<Task> repeatValidated = () => validated.ToResultAsync();
        Func<Task> repeatBuilder = () => builder.SendAsync();
        await repeatValidated.Should().ThrowAsync<InvalidOperationException>();
        await repeatBuilder.Should().ThrowAsync<InvalidOperationException>();
        Action lateValidator = () => builder.WithValidator(validator);
        Action lateStatus = () => validated.WithStatusResult(HttpStatusCode.NotFound, CoreEx.Results.Result.NotFoundError());
        lateValidator.Should().Throw<InvalidOperationException>();
        lateStatus.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Properties_And_ConfigureChaining()
    {
        using var client = new HttpClient();
        var builder = client.Send(HttpMethod.Post, "/api/test", r => r.Headers.Add("X-First", "1"));

        builder.Client.Should().BeSameAs(client);
        builder.Method.Should().Be(HttpMethod.Post);
        builder.RequestUri.Should().Be("/api/test");

        builder.Configure(r => r.Headers.Add("X-Second", "2"));

        using var handler = new InspectHandler((req, _) =>
        {
            req.Headers.GetValues("X-First").Single().Should().Be("1");
            req.Headers.GetValues("X-Second").Single().Should().Be("2");
            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var testClient = CreateClient(handler);
        var configuredBuilder = new HttpRequestBuilder(testClient, HttpMethod.Post, "/api/test", r => r.Headers.Add("X-First", "1"))
            .Configure(r => r.Headers.Add("X-Second", "2"));

        using var response = configuredBuilder.GetResponseAsync().GetAwaiter().GetResult();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetResponseAsync_And_SendAsync_DoNotDisposeResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var response1 = await client.Send(HttpMethod.Get, "/test").GetResponseAsync();
        lastResponse!.IsDisposed.Should().BeFalse();
        response1.Dispose();
        lastResponse.IsDisposed.Should().BeTrue();

        var response2 = await client.Send(HttpMethod.Get, "/test").SendAsync();
        lastResponse!.IsDisposed.Should().BeFalse();
        response2.Dispose();
        lastResponse.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task GetValueAsync_ReturnsDeserializedValue_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("{\"name\":\"WidgetA\"}", Encoding.UTF8, "application/json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var widget = await client.Send(HttpMethod.Get, "/widgets/1").GetValueAsync<Widget>();
        widget.Name.Should().Be("WidgetA");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task GetValueOrDefaultAsync_ReturnsNullForNoContent_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NoContent);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var widget = await client.Send(HttpMethod.Get, "/widgets/1").GetValueOrDefaultAsync<Widget>();
        widget.Should().BeNull();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task GetStringAsync_ReturnsString_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("hello world", Encoding.UTF8, "text/plain"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var str = await client.Send(HttpMethod.Get, "/greeting").GetStringAsync();
        str.Should().Be("hello world");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ThrowOnErrorAsync_ThrowsOnFailure_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.InternalServerError);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        Func<Task> act = async () => await client.Send(HttpMethod.Get, "/fail").ThrowOnErrorAsync();
        await act.Should().ThrowAsync<HttpRequestException>();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ToResultAsync_ReturnsResult_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/ok").ToResultAsync();
        result.IsSuccess.Should().BeTrue();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ToResultAsync_Typed_ReturnsValue_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("{\"name\":\"test\"}", Encoding.UTF8, "application/json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/item").ToResultAsync<Widget>();
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("test");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ToResultOrDefaultAsync_Typed_ReturnsNullForNoContent_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NoContent);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/item").ToResultOrDefaultAsync<Widget>();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ToStringResultAsync_ReturnsString_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("result string", Encoding.UTF8, "text/plain"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/str").ToStringResultAsync();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("result string");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ToProblemDetailsAsync_ReturnsProblemDetails_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        var pdJson = "{\"status\":400,\"title\":\"Bad Request\",\"type\":\"https://tools.ietf.org/html/rfc7231#section-6.5.1\"}";
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.BadRequest, new StringContent(pdJson, Encoding.UTF8, "application/problem+json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var pde = await client.Send(HttpMethod.Get, "/bad").ToProblemDetailsAsync();
        pde.Should().NotBeNull();
        pde!.ProblemDetails.Title.Should().Be("Bad Request");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task ThrowOnBusinessExceptionAsync_ThrowsOnBusinessException_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        var pdJson = "{\"status\":400,\"title\":\"Business Error\",\"errorType\":\"business\"}";
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.BadRequest, new StringContent(pdJson, Encoding.UTF8, "application/problem+json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        Func<Task> act = async () => await client.Send(HttpMethod.Get, "/biz").ThrowOnBusinessExceptionAsync();
        await act.Should().ThrowAsync<BusinessException>();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithJson_SerializesPayload_AndSupportsFluentChaining()
    {
        var payload = new Widget { Name = "PayloadWidget" };
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler(async (req, ct) =>
        {
            req.Method.Should().Be(HttpMethod.Post);
            req.RequestUri.Should().Be(new Uri("https://example.com/api/widgets"));
            req.Content.Should().NotBeNull();
            req.Content!.Headers.ContentType?.MediaType.Should().Be("application/json");
            var body = await req.Content.ReadAsStringAsync(ct);
            body.Should().Contain("PayloadWidget");

            lastResponse = new DisposableResponse(HttpStatusCode.Created, new StringContent("{\"name\":\"CreatedWidget\"}", Encoding.UTF8, "application/json"));
            return lastResponse;
        });

        using var client = CreateClient(handler);
        var created = await client.Post("/api/widgets").WithJson(payload)
            .GetValueAsync<Widget>();

        created.Name.Should().Be("CreatedWidget");
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithValidator_ValidatesResponse_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("{\"name\":\"ValidatedWidget\"}", Encoding.UTF8, "application/json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var replacement = new Widget { Name = "ReplacedWidget" };
        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(replacement)));

        using var client = CreateClient(handler);
        var validatedBuilder = client.Send(HttpMethod.Get, "/api/widgets/1").WithValidator(validator);

        validatedBuilder.Builder.Should().NotBeNull();
        validatedBuilder.Validator.Should().BeSameAs(validator);

        var widget = await validatedBuilder.GetValueAsync();
        widget.Should().BeSameAs(replacement);
        validator.Calls.Should().Be(1);
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithValidator_ValidationFailure_ThrowsHttpRequestException_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("{\"name\":\"BadWidget\"}", Encoding.UTF8, "application/json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var error = ValidationException.Create("name", "Name is invalid.");
        var validator = new WidgetValidator((_, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(null, true, error)));

        using var client = CreateClient(handler);
        Func<Task> act = async () => await client.Send(HttpMethod.Get, "/api/widgets/1").WithValidator(validator).GetValueAsync();
        var ex = (await act.Should().ThrowAsync<HttpRequestException>()).Which;
        ex.InnerException.Should().BeSameAs(error);
        validator.Calls.Should().Be(1);
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithValidator_ToResultAsync_ReturnsFailureOnValidationError_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.OK, new StringContent("{\"name\":\"BadWidget\"}", Encoding.UTF8, "application/json"));
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var error = ValidationException.Create("name", "Name is invalid.");
        var validator = new WidgetValidator((_, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(null, true, error)));

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/api/widgets/1").WithValidator(validator).ToResultAsync();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<HttpRequestException>().Which.InnerException.Should().BeSameAs(error);
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithValidator_GetValueOrDefaultAsync_ReturnsNullOnNoContent_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NoContent);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(w)));

        using var client = CreateClient(handler);
        var widget = await client.Send(HttpMethod.Get, "/api/widgets/1").WithValidator(validator).GetValueOrDefaultAsync();
        widget.Should().BeNull();
        validator.Calls.Should().Be(0);
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task FluentWithMethods_ConfigureRequestMessageCorrectly()
    {
        HttpRequestMessage? captured = null;
        using var handler = new InspectHandler((req, _) =>
        {
            captured = req;
            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var client = CreateClient(handler);
        await client.Send(HttpMethod.Patch, "/api/widgets/1")
            .WithIfMatch("abc")
            .WithMergePatchJsonContentType()
            .WithIdempotencyKey("idemp-123")
            .WithQuery("name eq 'test'", "name asc")
            .WithIncludeText()
            .WithIncludeInactive()
            .WithFields("id", "name")
            .WithoutFields("internalNotes")
            .WithPaging(skip: 10, take: 20, count: true)
            .WithSearch("red bike")
            .GetResponseAsync();

        captured.Should().NotBeNull();
        captured!.Headers.IfMatch.Should().ContainSingle().Which.Tag.Should().Be("\"abc\"");
        captured.Headers.GetValues(HttpNames.IdempotencyKeyHeaderName).Should().ContainSingle().Which.Should().Be("idemp-123");
        captured.Content.Should().BeNull(); // MergePatchJsonContentType applies to Content if present, but here content is not set yet
        var query = captured.RequestUri!.Query;
        query.Should().Contain("$filter=name%20eq%20%27test%27");
        query.Should().Contain("$orderby=name%20asc");
        query.Should().Contain("$text=true");
        query.Should().Contain("$inactive=true");
        query.Should().Contain("$fields=id%2Cname");
        query.Should().Contain("$exclude=internalNotes");
        query.Should().Contain("$skip=10");
        query.Should().Contain("$take=20");
        query.Should().Contain("$count=true");
        query.Should().Contain("$search=red%20bike");
    }

    [Test]
    public async Task FluentWithMethods_WithPayloadAndIfNoneMatch()
    {
        HttpRequestMessage? captured = null;
        using var handler = new InspectHandler((req, _) =>
        {
            captured = req;
            return Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.OK));
        });

        using var client = CreateClient(handler);
        await client.Put("/api/widgets/1").WithJson(new Widget { Name = "test" })
            .WithIfNoneMatch("xyz")
            .WithMergePatchJsonContentType()
            .WithPaging(new PagingArgs(5, 15, true))
            .WithQuery(new QueryArgs { Filter = "status eq 'active'" })
            .GetResponseAsync();

        captured.Should().NotBeNull();
        captured!.Headers.IfNoneMatch.Should().ContainSingle().Which.Tag.Should().Be("\"xyz\"");
        captured.Content!.Headers.ContentType?.MediaType.Should().Be(HttpNames.MergePatchJsonMediaTypeName);
        var query = captured.RequestUri!.Query;
        query.Should().Contain("$skip=5");
        query.Should().Contain("$take=15");
        query.Should().Contain("$count=true");
        query.Should().Contain("$filter=status%20eq%20%27active%27");
    }

    [Test]
    public void NullArguments_ThrowArgumentNullException()
    {
        using var client = new HttpClient();
        Action nullClient = () => Extensions.Send(null!, HttpMethod.Get, "/");
        Action nullMethod = () => client.Send(null!, "/");
        Action nullValidator = () => client.Send(HttpMethod.Get, "/").WithValidator<Widget>(null!);
        Action nullConfigure = () => client.Send(HttpMethod.Get, "/").Configure(null!);

        nullClient.Should().Throw<ArgumentNullException>();
        nullMethod.Should().Throw<ArgumentNullException>();
        nullValidator.Should().Throw<ArgumentNullException>();
        nullConfigure.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task WithStatusResult_DirectResult_InterceptsStatus_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NotFound);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.NotFound, CoreEx.Results.Result.NotFoundError())
            .ToResultAsync<Widget>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithStatusResult_FactoryOverloads_InterceptStatus_AndDisposeResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.Conflict);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.Conflict, () => CoreEx.Results.Result.ConflictError())
            .ToResultAsync<Widget>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConflictException>();
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithStatusResult_ResponseAwareFactory_InterceptsStatus()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.BadRequest)));
        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.BadRequest, res => CoreEx.Results.Result.BusinessError($"Status was {(int)res.StatusCode}"))
            .ToResultAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<BusinessException>().Which.Message.Should().Be("Status was 400");
    }

    [Test]
    public async Task WithStatusResult_AsyncFactory_InterceptsStatus()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.PreconditionFailed)));
        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.PreconditionFailed, async (res, ct) =>
            {
                await Task.Yield();
                return CoreEx.Results.Result.ConcurrencyError();
            })
            .ToResultAsync<Widget>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<ConcurrencyException>();
    }

    [Test]
    public async Task WithStatusResult_ShortCircuitsValidator_AndDisposesResponse()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NotFound);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(w)));

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.NotFound, () => CoreEx.Results.Result.NotFoundError())
            .WithValidator(validator)
            .ToResultAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        validator.Calls.Should().Be(0); // Short-circuited!
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithStatusResult_ChainedAfterValidator_ShortCircuitsValidator()
    {
        DisposableResponse? lastResponse = null;
        using var handler = new InspectHandler((_, _) =>
        {
            lastResponse = new DisposableResponse(HttpStatusCode.NotFound);
            return Task.FromResult<HttpResponseMessage>(lastResponse);
        });

        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(w)));

        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithValidator(validator)
            .WithStatusResult(HttpStatusCode.NotFound, () => CoreEx.Results.Result.NotFoundError())
            .ToResultAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        validator.Calls.Should().Be(0);
        lastResponse!.IsDisposed.Should().BeTrue();
    }

    [Test]
    public async Task WithStatusResult_GetValueAsync_ThrowsMappedException()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = CreateClient(handler);
        Func<Task> act = async () => await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.NotFound, () => CoreEx.Results.Result.NotFoundError())
            .GetValueAsync<Widget>();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task WithStatusResult_ThrowOnErrorAsync_ThrowsMappedException()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = CreateClient(handler);
        Func<Task> act = async () => await client.Send(HttpMethod.Get, "/items/123")
            .WithStatusResult(HttpStatusCode.NotFound, () => CoreEx.Results.Result.NotFoundError())
            .ThrowOnErrorAsync();

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Test]
    public async Task WithErrorStatus_Alias_BehavesIdentically()
    {
        using var handler = new InspectHandler((_, _) => Task.FromResult<HttpResponseMessage>(new HttpResponseMessage(HttpStatusCode.NotFound)));
        using var client = CreateClient(handler);
        var result = await client.Send(HttpMethod.Get, "/items/123")
            .WithErrorStatus(HttpStatusCode.NotFound, _ => CoreEx.Results.Result.NotFoundError())
            .ToResultAsync<Widget>();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
    }

    [Test]
    public async Task ValidatedHttpResponse_WithStatusResult_InterceptsStatus()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var validator = new WidgetValidator((w, _) => Task.FromResult<CoreEx.Validation.IValidationResult<Widget>>(new WidgetValidationResult(w)));

        var result = await response.WithValidator(validator)
            .WithStatusResult(HttpStatusCode.NotFound, () => CoreEx.Results.Result.NotFoundError())
            .ToResultAsync();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().BeOfType<NotFoundException>();
        validator.Calls.Should().Be(0);
    }
}
