namespace Contoso.Orders.Test.Unit;

public class EntryPoint
{
    public static void ConfigureApplication(IHostApplicationBuilder builder)
    {
        builder.Services.AddExecutionContext();
        builder.Services.AddMemoryCache();
        builder.Services.AddReferenceDataOrchestrator<ReferenceDataProvider>();

        var jdr = JsonDataReader.ParseYaml<Contoso.Orders.Database.Program>("ref-data.yaml", JsonDataReaderOptions.CreateForReferenceData());
        builder.Services.AddSingleton(new ReferenceDataProvider(jdr));
    }

    public class ReferenceDataProvider(JsonDataReader jdr) : IReferenceDataProvider
    {
        public IEnumerable<(Type, Type)> Types =>
        [
            (typeof(OrderStatus), typeof(OrderStatusCollection))
        ];

        public Task<IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default) => type switch
        {
            _ when type == typeof(OrderStatus) => Task.FromResult((IReferenceDataCollection)jdr.Deserialize<OrderStatusCollection>("Orders.$^OrderStatus")!),
            _ => throw new InvalidOperationException($"Type {type.FullName} is not a known {nameof(IReferenceData)}.")
        };
    }
}