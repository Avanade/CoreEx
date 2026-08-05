namespace CoreEx.RefData.Test.Unit;

public partial class ReferenceDataOrchestratorTests
{
    [Test]
    public void Add_And_Count()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData>
        {
            new DummyRefData { Code = "A" },
            new DummyRefData { Code = "B" }
        };

        coll.Count.Should().Be(2);
    }

    [Test]
    public void Contains_ExistingCode_ReturnsTrue()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" } };
        coll.Contains(new DummyRefData { Code = "A" }).Should().BeTrue();
    }

    [Test]
    public void Contains_MissingCode_ReturnsFalse()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" } };
        coll.Contains(new DummyRefData { Code = "Z" }).Should().BeFalse();
    }

    [Test]
    public void CopyTo_PopulatesArrayWithResolvedItems()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData>
        {
            new DummyRefData { Code = "A" },
            new DummyRefData { Code = "B" }
        };

        var array = new DummyRefData[2];
        coll.CopyTo(array, 0);

        array.Select(x => x.Code).Should().BeEquivalentTo("A", "B");
        array.Select(x => x.Text).Should().BeEquivalentTo("Alpha", "Beta");
    }

    [Test]
    public void CopyTo_ArrayTooSmall_Throws()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" }, new DummyRefData { Code = "B" } };
        var array = new DummyRefData[1];
        Action act = () => coll.CopyTo(array, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void IndexOf_ReturnsCorrectIndex()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" }, new DummyRefData { Code = "B" } };
        coll.IndexOf(new DummyRefData { Code = "B" }).Should().Be(1);
    }

    [Test]
    public void Remove_RemovesByCode()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" }, new DummyRefData { Code = "B" } };
        coll.Remove(new DummyRefData { Code = "A" }).Should().BeTrue();
        coll.Count.Should().Be(1);
        coll.ToCodeList().Should().BeEquivalentTo(["B"]);
    }

    [Test]
    public void GetEnumerator_ResolvesItemsViaOrchestrator()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" }, new DummyRefData { Code = "C" } };
        coll.Select(x => x.Text).Should().BeEquivalentTo("Alpha", "Charlie");
    }

    [Test]
    public void HasInvalidItems_WhenCodeNotRegistered_ReturnsTrue()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "unknown-code" } };
        coll.HasInvalidItems.Should().BeTrue();
    }

    [Test]
    public void HasInvalidItems_AllCodesValid_ReturnsFalse()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" } };
        coll.HasInvalidItems.Should().BeFalse();
    }

    [Test]
    public void HasInactiveItems_WhenItemInactive_ReturnsTrue()
    {
        // DummyRefData Code "D" is registered as IsInactive = true.
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "D" } };
        coll.HasInactiveItems.Should().BeTrue();
    }

    [Test]
    public void ToCodeList_ReturnsCodesInOrder()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" }, new DummyRefData { Code = "B" } };
        coll.ToCodeList().Should().Equal("A", "B");
    }

    [Test]
    public void ToRefDataList_ReturnsResolvedItems()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" } };
        coll.ToRefDataList().Select(x => x.Code).Should().BeEquivalentTo("A");
    }

    [Test]
    public void Constructor_WithItems_ExtractsCodes()
    {
        var items = new[] { new DummyRefData { Code = "A" }, new DummyRefData { Code = "B" } };
        var coll = new ReferenceDataCodeCollection<DummyRefData>(items);
        coll.ToCodeList().Should().Equal("A", "B");
    }

    [Test]
    public void Constructor_WithCodesParams_SetsCodes()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData>("A", "B");
        coll.ToCodeList().Should().Equal("A", "B");
    }

    [Test]
    public void Constructor_WithRefListCodes_UsesExternalList()
    {
        List<string?>? codes = ["A", "B"];
        var coll = new ReferenceDataCodeCollection<DummyRefData>(ref codes);
        coll.ToCodeList().Should().Equal("A", "B");
    }

    [Test]
    public void IsReadOnly_IsFalse()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData>();
        coll.IsReadOnly.Should().BeFalse();
    }

    [Test]
    public void Clear_RemovesAll()
    {
        var coll = new ReferenceDataCodeCollection<DummyRefData> { new DummyRefData { Code = "A" } };
        coll.Clear();
        coll.Count.Should().Be(0);
    }
}
