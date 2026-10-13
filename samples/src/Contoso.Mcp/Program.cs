namespace Contoso.Mcp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddNamedHttpClient("ProductsApi");
        builder.AddNamedHttpClient("ShoppingApi");

        builder.Services
            .AddMcpServer()
            .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
            .WithToolsFromAssembly();

        var app = builder.Build();

        app.MapGet("/health", () => Results.Ok("Healthy"));
        app.MapMcp("/mcp");

        app.Run();
    }
}
