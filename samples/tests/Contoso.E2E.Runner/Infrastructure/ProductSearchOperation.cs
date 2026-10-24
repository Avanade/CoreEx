namespace Contoso.E2E.Runner.Infrastructure;

/// <summary>Interactively searches the live Products API without running scenario setup or load simulation.</summary>
public sealed class ProductSearchOperation(HttpClient client, IAnsiConsole? console = null)
{
    private readonly HttpClient _client = client.ThrowIfNull();
    private readonly IAnsiConsole _console = console ?? AnsiConsole.Console;
    private string? _mode;

    /// <summary>Prompts for successive searches until the user returns to the menu or cancels.</summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _mode = null;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ConsoleCancelEventHandler cancel = (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        Console.CancelKeyPress += cancel;
        try
        {
            _console.Write(new Rule("[bold blue]Product Search[/]").RuleStyle("blue").LeftJustified());
            _console.MarkupLine("[grey]Search again after each result. Blank input or /back returns to the menu; Ctrl+C cancels.[/]");
            while (true)
            {
                cancellation.Token.ThrowIfCancellationRequested();
                var text = await _console.PromptAsync(new TextPrompt<string>("Search text:").AllowEmpty(), cancellation.Token).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("/back", StringComparison.OrdinalIgnoreCase))
                    return;

                try
                {
                    await SearchAsync(text, cancellation.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    var detail = ex is CoreEx.Http.ProblemDetailsException problem ? problem.ProblemDetails.Detail : null;
                    var message = ex is OperationCanceledException ? "Products API request timed out." : ex.Message;
                    _console.MarkupLine($"[red]Product Search failed: {message.EscapeMarkup()}[/]");

                    if (!string.IsNullOrWhiteSpace(detail))
                        _console.MarkupLine($"[red]{detail.EscapeMarkup()}[/]");

                    _console.MarkupLine("[grey]Check the Products API and its configured search/index/model readiness. No fallback or rebuild was attempted.[/]");
                }
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            _console.MarkupLine("[yellow]Product Search cancelled; returning to the menu.[/]");
        }
        finally
        {
            _mode = null;
            Console.CancelKeyPress -= cancel;
        }
    }

    /// <summary>Reads the configured server mode, then requests and displays the first ten results in server rank order.</summary>
    private async Task SearchAsync(string text, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        (string Mode, ProductLite[] Products) result;
        if (_console.Profile.Capabilities.Interactive && _console.Profile.Capabilities.Ansi)
        {
            result = await _console.Status().Spinner(Spinner.Known.Dots).StartAsync(_mode is null ? "Reading Products search mode..." : $"Searching Products ({_mode})...",
                status => RequestAsync(text, message => status.Status(message), cancellationToken)).ConfigureAwait(false);
        }
        else
            result = await RequestAsync(text, message => _console.MarkupLine($"[grey]{message.EscapeMarkup()}[/]"), cancellationToken).ConfigureAwait(false);

        stopwatch.Stop();
        var products = result.Products;
        _console.MarkupLine($"[grey]{result.Mode} · Returned {products.Length} {(products.Length == 1 ? "product" : "products")} in {stopwatch.Elapsed.TotalSeconds:F2}s.[/]");
        if (products.Length == 0)
        {
            _console.MarkupLine("[yellow]No products found.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).BorderColor(Color.Grey)
            .AddColumn(new TableColumn("Rank").RightAligned())
            .AddColumn("SKU")
            .AddColumn("Product (Text)")
            .AddColumn(new TableColumn("Price (USD)").RightAligned());

        for (var i = 0; i < products.Length; i++)
        {
            var product = products[i];
            table.AddRow((i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), (product.Sku ?? string.Empty).EscapeMarkup(),
                (product.Text ?? string.Empty).EscapeMarkup(), product.Price.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-US")));
        }

        _console.Write(table);
    }

    /// <summary>Discovers the server mode and loads results without overriding ranking or rebuilding anything.</summary>
    private async Task<(string Mode, ProductLite[] Products)> RequestAsync(string text, Action<string> status, CancellationToken cancellationToken)
    {
        if (_mode is null)
            _mode = await ReadModeAsync(status, cancellationToken).ConfigureAwait(false);

        status($"Searching Products ({_mode})...");

        using var response = await _client.GetAsync($"/api/products?$search={Uri.EscapeDataString(text)}&$take=10", cancellationToken).ConfigureAwait(false);
        var products = await response.GetValueAsync<ProductLite[]>(cancellationToken).ConfigureAwait(false);
        return (_mode, products);
    }

    private async Task<string> ReadModeAsync(Action<string> status, CancellationToken cancellationToken)
    {
        status("Reading Products search mode...");
        using var schemaResponse = await _client.GetAsync("/api/products/$query", cancellationToken).ConfigureAwait(false);
        var schema = await schemaResponse.GetValueAsync<JsonElement>(cancellationToken).ConfigureAwait(false);
        var mode = schema.TryGetProperty("search", out var search) && search.TryGetProperty("mode", out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.ToLowerInvariant() switch
            {
                "semantic" => "Semantic",
                "fulltext" => "FullText",
                "hybrid" => "Hybrid",
                _ => null
            }
            : null;

        if (mode is null)
            throw new InvalidOperationException("Products query schema does not advertise a supported Semantic, FullText or Hybrid search mode.");

        return mode;
    }
}
