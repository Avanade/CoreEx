namespace Contoso.Test.Aspire;

[TestFixture]
public class ProductSearchOperationTests
{
    [TestCase("semantic", "Semantic")]
    [TestCase("fulltext", "FullText")]
    [TestCase("hybrid", "Hybrid")]
    public async Task Search_UsesEncodedRestRequestAndServerMode_RendersRankedProductLite(string mode, string expectedMode)
    {
        const string text = "mud & oil + #?/$= café [red]";
        using var handler = new SearchHandler(_ => Json("""[{"sku":"Z-FIRST","text":"Trail [red]bike[/]","price":1234.5},{"sku":"A-SECOND","text":"Second","price":9.95}]"""), mode);
        using var client = CreateClient(handler);
        using var console = new ScriptConsole(text, "/back");

        await new ProductSearchOperation(client, console).RunAsync();

        handler.Requests.Should().Equal("/api/products/$query", $"/api/products?$search={Uri.EscapeDataString(text)}&$take=10");
        console.Output.Should().Contain($"Searching Products ({expectedMode})").And.Contain($"{expectedMode} · Returned 2 products in ")
            .And.Contain("Rank").And.Contain("SKU").And.Contain("Product (Text)")
            .And.Contain("Price (USD)").And.Contain("1,234.50").And.Contain("9.95").And.Contain("Trail [red]bike[/]").And.Contain(text);
        console.Output.IndexOf("Z-FIRST", StringComparison.Ordinal).Should().BeLessThan(console.Output.IndexOf("A-SECOND", StringComparison.Ordinal));
        console.Output.Should().NotContain("total matches").And.NotContain("candidate pool").And.NotContain("HTTP request:")
            .And.NotContain("excludes schema lookup").And.NotContain("Search: ");
        console.Output.Split(text).Should().HaveCount(2, "the prompt echoes the input once; results must not repeat it");
    }

    [Test]
    public async Task Search_NoResultsAndRepeat_ReusesSessionSchemaAndDoesNotRequestCount()
    {
        using var handler = new SearchHandler(_ => Json("[]"));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("nothing", "try again", "");

        await new ProductSearchOperation(client, console).RunAsync();

        handler.Requests.Should().Equal("/api/products/$query", "/api/products?$search=nothing&$take=10",
            "/api/products?$search=try%20again&$take=10");
        console.Output.Split("No products found.").Should().HaveCount(3);
        console.Output.Should().NotContain("Price (USD)");
    }

    [Test]
    public async Task ReenteringOperation_RefreshesSessionSchema()
    {
        using var handler = new SearchHandler(_ => Json("[]"));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("first", "/back", "second", "/back");
        var operation = new ProductSearchOperation(client, console);

        await operation.RunAsync();
        await operation.RunAsync();

        handler.Requests.Should().Equal("/api/products/$query", "/api/products?$search=first&$take=10",
            "/api/products/$query", "/api/products?$search=second&$take=10");
    }

    [Test]
    public async Task ElapsedTime_IncludesSchemaLookupAndResponseDecoding()
    {
        using var handler = new SearchHandler(_ => Json("[]"), beforeSchema: cancellationToken => Task.Delay(150, cancellationToken));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud", "/back");

        await new ProductSearchOperation(client, console).RunAsync();

        var timing = System.Text.RegularExpressions.Regex.Match(console.Output, @"Returned 0 products in ([0-9.,]+)s");
        timing.Success.Should().BeTrue();
        double.Parse(timing.Groups[1].Value, System.Globalization.CultureInfo.CurrentCulture).Should().BeGreaterThanOrEqualTo(0.1);
        console.Output.Should().Contain("Reading Products search mode...").And.Contain("Searching Products (Semantic)...");
    }

    [Test]
    public async Task Cancellation_DuringSchemaLookupReturnsToMenuWithoutSearching()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SearchHandler(_ => throw new InvalidOperationException("Unexpected search."), beforeSchema: token =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        });
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud");

        await new ProductSearchOperation(client, console).RunAsync(cancellation.Token);

        handler.Requests.Should().Equal("/api/products/$query");
        console.Output.Should().Contain("Product Search cancelled; returning to the menu.").And.NotContain("Returned ").And.NotContain("Product Search failed:");
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase("/back")]
    [TestCase(" /BACK ")]
    public async Task BackOrBlank_DoesNotCallApi(string text)
    {
        using var handler = new SearchHandler(_ => throw new InvalidOperationException("Unexpected search."));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole(text);

        await new ProductSearchOperation(client, console).RunAsync();

        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task FailedModelRequest_DisplaysEscapedProblemAndAllowsRetryWithoutFallback()
    {
        var attempt = 0;
        using var handler = new SearchHandler(_ => ++attempt == 1
            ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("""{"title":"Model [red]unready[/]","detail":"Install [model] before searching.","status":503}""", null, "application/problem+json")
            }
            : Json("[]"));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud", "retry", "/back");

        await new ProductSearchOperation(client, console).RunAsync();

        console.Output.Should().Contain("Product Search failed: Model [red]unready[/]").And.Contain("Install [model] before searching.")
            .And.Contain("No fallback or rebuild was attempted.").And.Contain("No products found.");
        handler.Requests.Should().HaveCount(3);
        handler.Requests.Should().OnlyContain(r => r == "/api/products/$query" || r.EndsWith("&$take=10", StringComparison.Ordinal));
    }

    [TestCase("http")]
    [TestCase("connection")]
    [TestCase("timeout")]
    [TestCase("json")]
    [TestCase("null")]
    public async Task RequestFailure_IsVisibleAndDoesNotBecomeNoResults(string failure)
    {
        using var handler = new SearchHandler(_ => failure switch
        {
            "http" => new HttpResponseMessage(HttpStatusCode.BadGateway),
            "connection" => throw new HttpRequestException("API [offline]"),
            "timeout" => throw new TaskCanceledException("Timed out."),
            "json" => Json("{not-json}"),
            _ => Json("null")
        });
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud", "/back");

        await new ProductSearchOperation(client, console).RunAsync();

        console.Output.Should().Contain("Product Search failed:").And.NotContain("No products found.");
        if (failure == "timeout")
            console.Output.Should().Contain("Products API request timed out.");
        if (failure == "connection")
            console.Output.Should().Contain("API [offline]");
        handler.Requests.Should().HaveCount(2);
    }

    [TestCase("missing")]
    [TestCase("keyword")]
    public async Task UnsupportedSchema_FailsWithoutSearchingOrInventingMode(string mode)
    {
        using var handler = new SearchHandler(_ => throw new InvalidOperationException("Unexpected search."), mode);
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud", "/back");

        await new ProductSearchOperation(client, console).RunAsync();

        handler.Requests.Should().Equal("/api/products/$query");
        console.Output.Should().Contain("does not advertise a supported").And.NotContain("No products found.").And.NotContain("Server search mode:");
    }

    [Test]
    public async Task Cancellation_FlowsToHttpAndReturnsToMenu()
    {
        using var cancellation = new CancellationTokenSource();
        using var handler = new SearchHandler(cancellationToken =>
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Json("[]");
        });
        using var client = CreateClient(handler);
        using var console = new ScriptConsole("mud");

        await new ProductSearchOperation(client, console).RunAsync(cancellation.Token);

        console.Output.Should().Contain("Product Search cancelled; returning to the menu.").And.NotContain("Product Search failed:").And.NotContain("No products found.");
        handler.Requests.Should().HaveCount(2);
    }

    [Test]
    public async Task AlreadyCancelled_DoesNotPromptOrRequest()
    {
        using var handler = new SearchHandler(_ => throw new InvalidOperationException("Unexpected search."));
        using var client = CreateClient(handler);
        using var console = new ScriptConsole();

        await new ProductSearchOperation(client, console).RunAsync(new CancellationToken(true));

        handler.Requests.Should().BeEmpty();
        console.Output.Should().Contain("Product Search cancelled");
    }

    [Test]
    public void InteractiveOperation_IsNotDiscoveredAsScenarioOrSetup()
    {
        var assembly = typeof(ProductSearchOperation).Assembly;
        Contoso.E2E.Runner.Scenarios.ScenarioManager.Create(assembly).Scenarios.Values
            .Should().NotContain(s => s.Factory().GetType() == typeof(ProductSearchOperation));
        Contoso.E2E.Runner.Scenarios.SetUpManager.Create(assembly).SetUps.Values
            .Should().NotContain(s => s.Factory().GetType() == typeof(ProductSearchOperation));
    }

    private static HttpClient CreateClient(HttpMessageHandler handler) => new(handler) { BaseAddress = new Uri("https://products.example/") };

    private static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value, null, "application/json") };

    private sealed class SearchHandler(Func<CancellationToken, HttpResponseMessage> search, string mode = "semantic", Func<CancellationToken, Task>? beforeSchema = null) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Method.Should().Be(HttpMethod.Get);
            var path = request.RequestUri!.PathAndQuery;
            Requests.Add(path);
            if (path == "/api/products/$query" && beforeSchema is not null)
                await beforeSchema(cancellationToken).ConfigureAwait(false);
            return path == "/api/products/$query"
                ? Json(mode == "missing" ? "{}" : JsonSerializer.Serialize(new { search = new { mode, ranked = true } }))
                : search(cancellationToken);
        }
    }

    internal sealed class ScriptConsole : IAnsiConsole, IAnsiConsoleInput, IDisposable
    {
        private readonly StringWriter _output = new();
        private readonly IAnsiConsole _console;
        private readonly Queue<ConsoleKeyInfo> _keys = new();

        public ScriptConsole(params string[] lines)
        {
            _console = AnsiConsole.Create(new AnsiConsoleSettings { Ansi = AnsiSupport.No, Interactive = InteractionSupport.Yes, Out = new AnsiConsoleOutput(_output) });
            _console.Profile.Width = 180;
            foreach (var line in lines)
            {
                foreach (var character in line)
                    _keys.Enqueue(new ConsoleKeyInfo(character, (ConsoleKey)0, false, false, false));
                _keys.Enqueue(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));
            }
        }

        public string Output => _output.ToString();
        public Profile Profile => _console.Profile;
        public IAnsiConsoleCursor Cursor => _console.Cursor;
        public IAnsiConsoleInput Input => this;
        public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;
        public RenderPipeline Pipeline => _console.Pipeline;
        public void Clear(bool home) => _console.Clear(home);
        public void Write(IRenderable renderable) => _console.Write(renderable);
        public void WriteAnsi(Action<AnsiWriter> action) => _console.WriteAnsi(action);
        public bool IsKeyAvailable() => _keys.Count > 0;
        public ConsoleKeyInfo? ReadKey(bool intercept) => _keys.Dequeue();
        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadKey(intercept));
        }

        public void Dispose() => _output.Dispose();
    }
}
