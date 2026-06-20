using CoreEx.Entities;

namespace CoreEx.Test.Unit.Entities;

[TestFixture]
public class CompositeKeyTests
{
    [Test]
    public void Create_StaticFactory_CreatesKey()
    {
        var key = CompositeKey.Create(1, "abc", null);
        key.Args.Should().BeEquivalentTo(new object?[] { 1, "abc", null });
    }

    [Test]
    public void Constructor_Empty()
    {
        var key = new CompositeKey();
        key.Args.Length.Should().Be(0);
    }

    [Test]
    public void Constructor_WithArgs()
    {
        var key = CompositeKey.Create(1, "x", 3.14);
        key.Args.Should().BeEquivalentTo(new object?[] { 1, "x", 3.14 });
    }

    [Test]
    public void Constructor_NullArgs()
    {
        var key = new CompositeKey(null!);
        key.Args.Length.Should().Be(1);
        key.Args[0].Should().BeNull();
    }

    [Test]
    public void Args_Immutable()
    {
        var key = new CompositeKey(1, 2);
        var arr = key.Args;
        arr[0].Should().Be(1);
        arr[1].Should().Be(2);
        // ImmutableArray: cannot add/remove, so just check length and values
        arr.Length.Should().Be(2);
    }

    [Test]
    public void Equals_SameArgs_True()
    {
        var k1 = CompositeKey.Create(1, "a");
        var k2 = CompositeKey.Create(1, "a");
        k1.Equals(k2).Should().BeTrue();
        k1.Should().Be(k2);
        (k1 == k2).Should().BeTrue();
        (k1 != k2).Should().BeFalse();
        k1.Equals((object)k2).Should().BeTrue();
    }

    [Test]
    public void Equals_DifferentArgs_False()
    {
        var k1 = new CompositeKey(1, "a");
        var k2 = new CompositeKey(2, "a");
        k1.Equals(k2).Should().BeFalse();
        k1.Should().NotBe(k2);
        (k1 == k2).Should().BeFalse();
        (k1 != k2).Should().BeTrue();
        k1.Equals((object)k2).Should().BeFalse();
    }

    [Test]
    public void Equals_Object_NotCompositeKey_False()
    {
        var k1 = new CompositeKey(1, "a");
        k1.Equals("not a key").Should().BeFalse();
    }

    [Test]
    public void GetHashCode_EqualKeys_SameHash()
    {
        var k1 = new CompositeKey(1, "a");
        var k2 = CompositeKey.Create(1, "a");
        k1.GetHashCode().Should().Be(k2.GetHashCode());
    }

    [Test]
    public void GetHashCode_DifferentKeys_DifferentHash()
    {
        var k1 = new CompositeKey(1, "a");
        var k2 = CompositeKey.Create(2, "a");
        k1.GetHashCode().Should().NotBe(k2.GetHashCode());
    }

    [Test]
    public void ToString_Formatting()
    {
        var k1 = new CompositeKey(123456, "abc", new DateTimeOffset(2025, 04, 13, 17, 02, 53, TimeSpan.FromHours(8)), null, -10034.3456m, true);
        k1.ToString().Should().Be("123456,abc,2025-04-13T09:02:53.0000000+00:00,,-10034.3456,true");

        var k2 = CompositeKey.Create(123456, "abc");
        k2.ToString().Should().Be("123456,abc");
    }

    [Test]
    public void Single_Key()
    {
        CompositeKey k1 = 123;
        CompositeKey k2 = 123;
        k1.Should().Be(k2);
        k1.GetHashCode().Should().Be(k2.GetHashCode());
        k1.ToString().Should().Be("123");
        k2.ToString().Should().Be("123"); 
    }

    [Test]
    public void Cast_To_CompositeKey()
    {
        CompositeKey k1 = 123; // Implicit cast from uint
        k1.Args.Should().BeEquivalentTo(new object?[] { 123 });
        CompositeKey k2 = (uint?)456; // Implicit cast from nullable uint
        k2.Args.Should().BeEquivalentTo(new object?[] { 456u });
        CompositeKey k3 = 789ul; // Implicit cast from ulong
        k3.Args.Should().BeEquivalentTo(new object?[] { 789ul });
        CompositeKey k4 = (ulong?)101112; // Implicit cast from nullable ulong
        k4.Args.Should().BeEquivalentTo(new object?[] { 101112ul });
        CompositeKey k5 = new DateOnly(2025, 04, 13); // Implicit cast from DateOnly
        k5.Args.Should().BeEquivalentTo(new object?[] { new DateOnly(2025, 04, 13) });
        CompositeKey k6 = (DateOnly?)new DateOnly(2025, 05, 14); // Implicit cast from nullable DateOnly
        k6.Args.Should().BeEquivalentTo(new object?[] { new DateOnly(2025, 05, 14) });
        CompositeKey k7 = new TimeOnly(17, 02, 53); // Implicit cast from TimeOnly
        k7.Args.Should().BeEquivalentTo(new object?[] { new TimeOnly(17, 02, 53) });
        CompositeKey k8 = (TimeOnly?)new TimeOnly(18, 03, 54); // Implicit cast from nullable TimeOnly
        k8.Args.Should().BeEquivalentTo(new object?[] { new TimeOnly(18, 03, 54) });
        byte[] bytes = { 0x01, 0x02 };
        CompositeKey k9 = bytes; // Implicit cast from byte[]
        k9.Args.Should().BeEquivalentTo(new object?[] { bytes });
        object?[] arr = { "a", null };
        CompositeKey k10 = arr; // Implicit cast from object?[]
        k10.Args.Should().BeEquivalentTo(arr);
    }
}