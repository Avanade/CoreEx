namespace CoreEx.Cosmos.Test.Unit;

[TestFixture]
public class CosmosDbContainerQueryTests : CosmosTestBase
{
    private const string ContainerId = "query-items";
    private static string? _partitionKey;

    private static async Task<CosmosDbContainer<TestItem>> GetContainerAsync()
    {
        await GetOrCreateContainerAsync(ContainerId).ConfigureAwait(false);
        return CreateCosmosDb().Container<TestItem>(ContainerId, o => o.WithPartitionKey(m => m.PartitionKey));
    }

    /// <summary>
    /// Seeds (once) 5 items sharing a single partition so that <c>Skip</c>/<c>Take</c> paging is deterministic within that partition.
    /// </summary>
    private static async Task<(CosmosDbContainer<TestItem> Container, string PartitionKey)> GetSeededContainerAsync()
    {
        var container = await GetContainerAsync();

        if (_partitionKey is null)
        {
            var pk = NewId();
            for (var i = 0; i < 5; i++)
                await container.CreateAsync(new TestItem { Id = NewId(), PartitionKey = pk, Name = $"Item-{i:D2}" });

            _partitionKey = pk;
        }

        return (container, _partitionKey);
    }

    [Test]
    public async Task Query_WithPartitionFilter_ReturnsAllSeededItems()
    {
        var (container, partitionKey) = await GetSeededContainerAsync();

        var items = await container.Query(q => q.Where(m => m.PartitionKey == partitionKey)).ToListAsync();

        items.Should().HaveCount(5);
    }

    [Test]
    public async Task ToItemsResultAsync_AppliesSkipAndTake()
    {
        var (container, partitionKey) = await GetSeededContainerAsync();

        var query = container.Query(q => q.Where(m => m.PartitionKey == partitionKey).OrderBy(m => m.Name));

        var page = await query.WithPaging(PagingArgs.CreateWithCount(skip: 2, take: 2)).ToItemsResultAsync();

        page.Items.Should().HaveCount(2);
        page.Items!.Select(m => m.Name).Should().ContainInOrder("Item-02", "Item-03");
        page.Paging!.TotalCount.Should().Be(5);
    }
}
