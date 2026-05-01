using CoreEx.RefData.Abstractions;

namespace CoreEx.RefData.Test.Unit;

public class ReferenceDataTests
{
    private class TestRefData : ReferenceData<int, TestRefData> { }

    [Test]
    public void Id_Code_Text_Description_SortOrder_ETag()
    {
        var rd = new TestRefData
        {
            Id = 123,
            Code = "ABC",
            Text = "MyText",
            Description = "MyDesc",
            SortOrder = 5,
            ETag = "etag"
        };

        rd.Id.Should().Be(123);
        rd.Code.Should().Be("ABC");
        rd.GetText().Should().Be("MyText");
        rd.GetDescription().Should().Be("MyDesc");
        rd.SortOrder.Should().Be(5);
        rd.ETag.Should().Be("etag");
        rd.Text.Should().Contain("MyText");
        rd.Description.Should().Contain("MyDesc");
    }

    [Test]
    public void ToString_ReturnsTextOrCodeOrId()
    {
        var rd = new TestRefData { Text = "T", Code = "C", Id = 1 };
        rd.ToString().Should().Contain("T");

        rd = new TestRefData { Text = null, Code = "C", Id = 1 };
        rd.ToString().Should().Contain("C");

        rd = new TestRefData { Text = null, Code = null, Id = 1 };
        rd.ToString().Should().Contain("1");
    }

    [Test]
    public void IsActive_Default_True()
    {
        var rd = new TestRefData();
        rd.IsActive.Should().BeTrue();
    }

    [Test]
    public void IsActive_SetFalse()
    {
        var rd = new TestRefData { IsInactive = true };
        rd.IsActive.Should().BeFalse();
    }

    [Test]
    public void IsActive_StartDate_EndDate()
    {
        var now = DateTimeOffset.UtcNow;
        var rd = new TestRefData { StartsOn = now.AddDays(-1), EndsOn = now.AddDays(1) };
        rd.IsActive.Should().BeTrue();

        rd = new TestRefData { StartsOn = now.AddDays(1), EndsOn = now.AddDays(2) };
        rd.IsActive.Should().BeFalse();

        rd = new TestRefData { StartsOn = now.AddDays(-2), EndsOn = now.AddDays(-1) };
        rd.IsActive.Should().BeFalse();
    }

    [Test]
    public void SetInvalid_SetsIsValidFalse_AndIsActiveFalse()
    {
        var rd = new TestRefData();
        ((IReferenceData)rd).SetInvalid();
        ((IReferenceData)rd).IsValid.Should().BeFalse();
        rd.IsActive.Should().BeFalse();
    }

    [Test]
    public void HasMappings_And_Mappings()
    {
        var rd = new TestRefData();
        rd.HasMappings.Should().BeFalse();
        rd.Mappings.Should().BeNull();

        rd.SetMapping("x", 123);
        rd.HasMappings.Should().BeTrue();
        rd.Mappings.Should().NotBeNull();
        rd.Mappings.Should().ContainKey("x");
        rd.Mappings["x"].Should().Be(123);
    }

    [Test]
    public void TryGetMapping_FoundAndNotFound()
    {
        var rd = new TestRefData();
        rd.SetMapping("a", 42);
        rd.TryGetMapping<int>("a", out var v).Should().BeTrue();
        v.Should().Be(42);

        rd.TryGetMapping<int>("b", out var v2).Should().BeFalse();
        v2.Should().Be(0);
    }

    [Test]
    public void Implicit_Cast_To_Id()
    {
        var rd = new TestRefData { Id = 88 };
        int id = rd;
        id.Should().Be(88);

        int? id2 = rd;
        id2.Should().Be(88);

        TestRefData? rd2 = null;
        id = rd2;
        id.Should().Be(0);

        id2 = rd2;
        id2.Should().Be(0);
    }

    [Test]
    public void Implicit_Cast_To_Code()
    {
        var rd = new TestRefData { Code = "XYZ" };
        string code = rd;
        code.Should().Be("XYZ");

        string? code2 = rd;
        code2.Should().Be("XYZ");

        TestRefData? rd2 = null;
        code = rd2;
        code.Should().BeNull();

        code2 = rd2;
        code2.Should().BeNull();
    }

    [Test]
    public void IComparable()
    {
        var rd = new TestRefData { Code = "XYZ" };
        var rd2 = new TestRefData { Code = "ABC" };

        rd.CompareTo(rd2).Should().BeGreaterThan(0);
    }
}