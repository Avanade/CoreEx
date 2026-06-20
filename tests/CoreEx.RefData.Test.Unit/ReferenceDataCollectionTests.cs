using CoreEx.RefData.Abstractions;
using System.Runtime.CompilerServices;

namespace CoreEx.RefData.Test.Unit;

public class ReferenceDataCollectionTests
{
    private class TestRefData : ReferenceData<TestRefData> { }

    [Test]
    public void Constructor_Default()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        coll.SortOrder.Should().Be(ReferenceDataSortOrder.SortOrder);
    }

    [Test]
    public void Constructor_WithSortOrderAndComparer()
    {
        var coll = new ReferenceDataCollection<TestRefData>(ReferenceDataSortOrder.Code, StringComparer.Ordinal);
        coll.SortOrder.Should().Be(ReferenceDataSortOrder.Code);
    }

    [Test]
    public void Add_And_Contains()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        coll.Contains(item).Should().BeTrue();
        coll.Count.Should().Be(1);
    }

    [Test]
    public void AddRange_AddsAll()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var items = new[] { new TestRefData { Id = "1", Code = "A" }, new TestRefData { Id = "2", Code = "B" } };
        coll.AddRange(items);
        coll.Count.Should().Be(2);
    }

    [Test]
    public void Clear_RemovesAll()
    {
        var coll = new ReferenceDataCollection<TestRefData>
        {
            new() { Id = "1", Code = "A" }
        };
        coll.Clear();
        coll.Count.Should().Be(0);
    }

    [Test]
    public void ContainsId_And_GetById()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        coll.ContainsId("1").Should().BeTrue();
        coll.GetById("1").Should().Be(item);
    }

    [Test]
    public void ContainsCode_And_GetByCode()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        coll.ContainsCode("A").Should().BeTrue();
        coll.GetByCode("A").Should().Be(item);
    }

    [Test]
    public void TryGetById_And_TryGetByCode()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        coll.TryGetById("1", out var foundById).Should().BeTrue();
        foundById.Should().Be(item);
        coll.TryGetByCode("A", out var foundByCode).Should().BeTrue();
        foundByCode.Should().Be(item);
    }

    [Test]
    public void GetEnumerator_Works()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        foreach (var i in coll)
            i.Should().Be(item);
    }

    [Test]
    public void ICollection_CopyTo_And_Remove()
    {
        var coll = new ReferenceDataCollection<TestRefData>();
        var item = new TestRefData { Id = "1", Code = "A" };
        coll.Add(item);
        var arr = new TestRefData[1];
        var act = () => ((ICollection<TestRefData>)coll).CopyTo(arr, 0);
        act.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void GetItems_Sorting()
    {
        var items = new List<TestRefData>
        {
            new() { Id = "1", Code = "D", Text = "GGG", SortOrder = 4 },
            new() { Id = "2", Code = "B", Text = "JJJ", SortOrder = 3 },
            new() { Id = "3", Code = "A", Text = "HHH", SortOrder = 2 },
            new() { Id = "4", Code = "C", Text = "III", SortOrder = 1 },
            new() { Id = "5", Code = "E", Text = "FFF", SortOrder = 5, IsInactive = true },
            new() { Id = "6", Code = "-", Text = "EEE", SortOrder = 6 }
        };

        ((IReferenceData)items.Last()).SetInvalid();

        var coll = new ReferenceDataCollection<TestRefData>(ReferenceDataSortOrder.SortOrder);
        coll.AddRange(items);

        coll.GetItems().Select(x => x.Id).Should().BeEquivalentTo("5", "4", "3", "2", "1");

        coll.GetItems(ReferenceDataSortOrder.SortOrder, true).Select(x => x.Id).Should().BeEquivalentTo("4", "3", "2", "1");
        coll.GetItems(ReferenceDataSortOrder.Id, true).Select(x => x.Id).Should().BeEquivalentTo("1", "2", "3", "4");
        coll.GetItems(ReferenceDataSortOrder.Code, true).Select(x => x.Code).Should().BeEquivalentTo("A", "B", "C", "D");
        coll.GetItems(ReferenceDataSortOrder.Text, true).Select(x => x.Code).Should().BeEquivalentTo("D", "A", "C", "B");

        coll.GetItems(ReferenceDataSortOrder.Code, null).Select(x => x.Code).Should().BeEquivalentTo("A", "B", "C", "D", "E");
        coll.GetItems(ReferenceDataSortOrder.Code, false, null).Select(x => x.Code).Should().BeEquivalentTo("E", "-");
        coll.GetItems(ReferenceDataSortOrder.Code, false, false).Select(x => x.Code).Should().BeEquivalentTo("-");
    }

    [Test]
    public async Task AddRangeAsync_IQueryable_AddsAll()
    {
        var items = new List<TestRefData>
        {
            new() { Id = "1", Code = "A" },
            new() { Id = "2", Code = "B" }
        };
        var col = new ReferenceDataCollection<TestRefData>();
        await col.AddRangeAsync(items.AsQueryable());
        col.Count.Should().Be(2);
        col.GetById("1")!.Code.Should().Be("A");
        col.GetById("2")!.Code.Should().Be("B");
    }

    [Test]
    public async Task AddRangeAsync_IAsyncEnumerable_AddsAll()
    {
        static async IAsyncEnumerable<TestRefData> GetItems()
        {
            yield return new TestRefData { Id = "3", Code = "C" };
            await Task.Delay(1);
            yield return new TestRefData { Id = "4", Code = "D" };
        }

        var col = new ReferenceDataCollection<TestRefData>();
        await col.AddRangeAsync(GetItems());
        col.Count.Should().Be(2);
        col.GetById("3")!.Code.Should().Be("C");
        col.GetById("4")!.Code.Should().Be("D");
    }

    [Test]
    public async Task AddRangeAsync_IAsyncEnumerable_Null_DoesNothing()
    {
        var col = new ReferenceDataCollection<TestRefData>();
        await col.AddRangeAsync((IAsyncEnumerable<TestRefData>?)null);
        col.Count.Should().Be(0);
    }

    [Test]
    public async Task AddRangeAsync_IQueryable_CancellationToken()
    {
        var items = new List<TestRefData>
        {
            new() { Id = "5", Code = "E" }
        };
        var col = new ReferenceDataCollection<TestRefData>();
        using var cts = new CancellationTokenSource();
        await col.AddRangeAsync(items.AsQueryable(), cts.Token);
        col.Count.Should().Be(1);
        col.GetById("5")!.Code.Should().Be("E");
    }

    [Test]
    public async Task AddRangeAsync_IAsyncEnumerable_CancellationToken()
    {
        static async IAsyncEnumerable<TestRefData> GetItems([EnumeratorCancellation] CancellationToken ct = default)
        {
            yield return new TestRefData { Id = "6", Code = "F" };
            await Task.Delay(1, ct);
        }

        var col = new ReferenceDataCollection<TestRefData>();
        using var cts = new CancellationTokenSource();
        await col.AddRangeAsync(GetItems(cts.Token), cts.Token);
        col.Count.Should().Be(1);
        col.GetById("6")!.Code.Should().Be("F");
    }
}