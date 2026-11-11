namespace CoreEx.IO.Test.Unit;

public partial class JsonLinesFileProcessorTests
{
    [Test]
    public async Task GeneratedIntegrationCallsApplicationWithImmutableRevisionAndPreservesBusinessResult()
    {
        var application = new ApplicationDouble();
        await using var services = new ServiceCollection().AddExecutionContext()
            .AddSingleton<IOrderImportApplication>(application).AddDynamicServicesUsing<OrdersJsonlProcessor>().BuildServiceProvider();
        var processor = services.GetRequiredService<OrdersJsonlProcessor>();
        var source = Encoding.UTF8.GetBytes(Fixture("valid.jsonl"));
        Task<Stream> OpenRevision(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Stream>(new ChunkStream(source));
        }

        var context = OrdersJsonlProcessor.CreateContext("immutable/orders.jsonl", "attempt-1", "version-17", FileProcessingMode.ValidateThenExecute);
        var summary = await processor.ProcessAsync(OpenRevision, context);
        summary.IsSuccess.Should().BeTrue();
        application.Receipts.Should().Equal(("version-17", "A"), ("version-17", "B"));
        application.TotalQuantity.Should().Be(6);
        context = OrdersJsonlProcessor.CreateContext("immutable/orders.jsonl", "attempt-2", "version-17", FileProcessingMode.ValidateThenExecute);
        summary = await processor.ProcessAsync(OpenRevision, context);
        summary.IsSuccess.Should().BeTrue();
        application.TotalQuantity.Should().Be(6, "the application double owns receipt-based deduplication, not the parser");

        application.Failure = Result.Fail(new BusinessException("Application rejected order."));
        context = OrdersJsonlProcessor.CreateContext("immutable/orders.jsonl", "attempt-3", "version-18", FileProcessingMode.ValidateThenExecute);
        summary = await processor.ProcessAsync(OpenRevision, context);
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Error.Should().BeSameAs(application.Failure.Value.Error);
    }

    private sealed class ApplicationDouble : IOrderImportApplication
    {
        public List<(string Revision, string Id)> Receipts { get; } = [];
        public int TotalQuantity { get; private set; }
        public Result? Failure { get; set; }
        public Task<Result> ImportAsync(OrdersJsonlOrder order, FileProcessingIdentity identity, FileUnitInfo info, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var receipt = (identity.SourceRevision.ThrowIfNull(), order.Id.ThrowIfNull());
            if (Failure is { } failure)
                return Task.FromResult(failure);

            if (!Receipts.Contains(receipt))
            {
                Receipts.Add(receipt);
                TotalQuantity += order.Lines.ThrowIfNull().Sum(x => x.Quantity ?? 0);
            }

            return Task.FromResult(Result.Success);
        }
    }
}
