// Enable UTF-8 encoding for emoji and special character support.
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Build configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var context = new TestContext(config);
(bool ProductApiOk, bool ShoppingApiOk, bool OrdersApiOk) status = (false, false, false);

/***** Main program and choice pump. *****/

// Check API health on startup.
await CheckApiStatusAsync();

// Present main menu and handle choices until user exits.
var choiceManager = new ChoiceManager(context);

while (true)
{
    var choice = AnsiConsole.Prompt(choiceManager.AddRunnerChoices(new SelectionPrompt<string>().Title($"Select [green]option[/]:")));
    AnsiConsole.WriteLine();

    // Execute the selection choice.
    var result = await choiceManager.RunChoiceAsync(choice, status.ProductApiOk && status.ShoppingApiOk);

    // Handle the result of the choice execution.
    switch (result)
    {
        case ChoiceResult.Stop:
            AnsiConsole.MarkupLine("[blue]Thanks for using Contoso E2E Runner :-)[/]");
            return;

        case ChoiceResult.RequiresApi:
            AnsiConsole.MarkupLine("API(s) are not available :no_bicycles: - please start the Contoso.Aspire application [yellow]:oncoming_fist:[/]");
            ContinuePrompt();
            break;
            
        case ChoiceResult.RetryApi:
            await CheckApiStatusAsync();
            break;

        case ChoiceResult.ContinueWithPrompt:
            ContinuePrompt();
            break;
    }

    // Refresh banner and config.
    DisplayBannerAndConfig();
}

/***** Utility *****/

// Display main banner and configuration.
void DisplayBannerAndConfig()
{
    AnsiConsole.Clear();
    AnsiConsole.Write(
        new FigletText("Contoso E2E")
            .Centered()
            .Color(Color.Orange3));

    AnsiConsole.Write(
        new Panel(
            new Markup($"{(status.ProductApiOk ? "[green]:check_mark:[/] " : "[red]:cross_mark:[/]")} [grey]Products API:[/] {context.ProductsHttpClient.BaseAddress?.ToString().EscapeMarkup()}\n"
            + $"{(status.ShoppingApiOk ? "[green]:check_mark:[/] " : "[red]:cross_mark:[/]")} [grey]Shopping API:[/] {context.ShoppingHttpClient.BaseAddress?.ToString().EscapeMarkup()}\n"
            + $"{(status.OrdersApiOk ? "[green]:check_mark:[/] " : "[red]:cross_mark:[/]")} [grey]Orders API:[/] {context.OrdersHttpClient.BaseAddress?.ToString().EscapeMarkup()}"))
            .Header("[bold]API status:[/]")
            .BorderColor(Color.Grey)
            .Padding(1, 0));

    AnsiConsole.WriteLine();
}

// Present a prompt to continue, used after scenarios or when APIs are not healthy.
void ContinuePrompt()
{
    AnsiConsole.WriteLine();
    AnsiConsole.Markup("[grey]Press any key to continue...[/]");
    Console.ReadKey(true);
}

// Check API health with option to cancel by pressing ESC. Returns true if APIs are healthy, false if not or if check was cancelled.
async Task CheckApiStatusAsync()
{
    DisplayBannerAndConfig();

    status = await AnsiConsole.Status()
        .Spinner(Spinner.Known.Dots)
        .StartAsync<(bool ProductApiOk, bool ShoppingApiOk, bool OrdersApiOk)>("[grey]Checking API status... (press [yellow]ESC[/] to cancel)...[/]", async _ =>
        {
            var healthCheckTask = Task.Run(async () =>
            {
                var productApi = TestContext.HealthCheckAsync(context.ProductsHttpClient);
                var shoppingApi = TestContext.HealthCheckAsync(context.ShoppingHttpClient);
                var ordersApi = TestContext.HealthCheckAsync(context.OrdersHttpClient);

                await Task.WhenAll(productApi, shoppingApi, ordersApi);
                return (productApi.Result, shoppingApi.Result, ordersApi.Result);
            });

            // Wait for health check or ESC key
            bool wasCancelled = false;
            while (!healthCheckTask.IsCompleted)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                {
                    wasCancelled = true;
                    break;
                }

                await Task.Delay(100);
            }

            if (wasCancelled)
                return (false, false, false);
            else
                return await healthCheckTask;
        });

    if (!status.ProductApiOk || !status.ShoppingApiOk || !status.OrdersApiOk)
    {
        DisplayBannerAndConfig();
        AnsiConsole.MarkupLine("API(s) are not available :no_bicycles: - please start the Contoso.Aspire application [yellow]:oncoming_fist:[/]");
        ContinuePrompt();
    }

    DisplayBannerAndConfig();
}