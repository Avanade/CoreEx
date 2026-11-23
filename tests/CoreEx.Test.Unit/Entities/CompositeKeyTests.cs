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
        k1.ToString().Should().Be("123456~abc~2025-04-13T09:02:53.0000000+00:00~~-10034.3456~true");

        CompositeKey.Create(123456, "abc").ToString().Should().Be("123456~abc");
        CompositeKey.Create(Guid.Empty).ToString().Should().Be("00000000-0000-0000-0000-000000000000");
    }

    [Test]
    public void ToString_Matches_Canonical_And_Never_Throws()
    {
        var key = CompositeKey.Create("order-1", 2, @"a\b", string.Empty);
        key.ToString().Should().Be(@"order-1~2~a\b~");
        key.ToString().Should().Be(key.ToCanonicalString());

        default(CompositeKey).ToString().Should().BeEmpty();
        new CompositeKey().ToString().Should().BeEmpty();
        CompositeKey.Create<object?>(null).ToString().Should().BeEmpty();
        CompositeKey.Create<int?>(null).ToString().Should().BeEmpty();
        CompositeKey.Create("a~b", "c").ToString().Should().Be("a~b~c");
        CompositeKey.Create(new FormattableValue()).ToString().Should().Be("1.5");
    }

    [Test]
    public void ToCanonicalString_UsesReadableDelimitedValues()
    {
        CompositeKey.Create("a~b").ToCanonicalString().Should().Be("a~b");
        CompositeKey.Create("abc").ToCanonicalString().Should().Be("abc");
        CompositeKey.Create("héllo 😀").ToCanonicalString().Should().Be("héllo 😀");
    }

    [Test]
    public void ToCanonicalString_RejectsKeysThatCannotBeAnIdentity()
    {
        AssertRejected(default);
        AssertRejected(new CompositeKey());
        AssertRejected(CompositeKey.Create<object?>(null));
        AssertRejected(CompositeKey.Create("a", (string?)null));
        AssertRejected(CompositeKey.Create("a\0b"));
        AssertRejected(CompositeKey.Create("\uD800"));
        AssertRejected(CompositeKey.Create("\uDC00a"));
        AssertRejected(CompositeKey.Create('\uD800'));
        AssertRejected(CompositeKey.Create("a~b", "c"));
        AssertRejected(CompositeKey.Create("a", "~"));

        static void AssertRejected(CompositeKey key)
        {
            Action format = () => key.ToCanonicalString();
            format.Should().Throw<ArgumentException>();
        }
    }

    [Test]
    public void ToCanonicalString_IsValueBasedAndPreservesBoundaries()
    {
        CompositeKey.Create(123).ToCanonicalString().Should().Be(CompositeKey.Create("123").ToCanonicalString());
        CompositeKey.Create(123).Should().NotBe(CompositeKey.Create("123"));
        CompositeKey.Create(DayOfWeek.Monday).ToCanonicalString().Should().Be(CompositeKey.Create(1).ToCanonicalString());
        CompositeKey.Create(1.0m).ToCanonicalString().Should().Be(CompositeKey.Create(1.00m).ToCanonicalString());
        CompositeKey.Create(-0.0).ToCanonicalString().Should().Be(CompositeKey.Create(0).ToCanonicalString());
        CompositeKey.Create(new byte[] { 1, 2 }).ToCanonicalString().Should().Be(CompositeKey.Create("AQI=").ToCanonicalString());
        CompositeKey.Create("a", "b", "c").ToCanonicalString().Should().NotBe(CompositeKey.Create("a", "bc").ToCanonicalString());
        CompositeKey.Create(string.Empty, "a").ToCanonicalString().Should().NotBe(CompositeKey.Create("a").ToCanonicalString());
        CompositeKey.Create("a").ToCanonicalString().Should().NotBe(CompositeKey.Create("A").ToCanonicalString());
        CompositeKey.Create("a").ToCanonicalString().Should().NotBe(CompositeKey.Create("a ").ToCanonicalString());
    }

    [Test]
    public void ToCanonicalString_IsReplayStable()
    {
        CompositeKey[] keys = [CompositeKey.Create("order-1", 2, string.Empty), CompositeKey.Create(@"a\b", Guid.Empty), CompositeKey.Create(1.50m),
            CompositeKey.Create(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(5))), CompositeKey.Create("héllo 😀"), CompositeKey.Create(string.Empty, string.Empty)];

        foreach (var key in keys)
        {
            var canonical = key.ToCanonicalString();
            var replayed = CompositeKey.Create(canonical).ToCanonicalString();

            replayed.Should().Be(canonical);
            CompositeKey.Create(replayed).ToCanonicalString().Should().Be(canonical);
        }
    }

    [Test]
    public void ToCanonicalString_NormalizesDatesAndSupportsExtendedNumbers()
    {
        var utc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var offset = utc.ToOffset(TimeSpan.FromHours(5));
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        CompositeKey.Create(utc).ToCanonicalString().Should().Be(CompositeKey.Create(offset).ToCanonicalString());
        CompositeKey.Create(dt).ToCanonicalString().Should().Be(CompositeKey.Create(DateTime.SpecifyKind(dt, DateTimeKind.Unspecified)).ToCanonicalString());
        CompositeKey.Create((Int128)123, (UInt128)123, (Half)123, new System.Numerics.BigInteger(123)).ToCanonicalString()
            .Should().Be(CompositeKey.Create("123", "123", "123", "123").ToCanonicalString());
    }

    [Test]
    public void ToCanonicalString_UsesStableFormatsForSupportedTypes()
    {
        object[] ones = [(sbyte)1, (byte)1, (short)1, (ushort)1, 1, 1u, 1L, 1UL, (nint)1, (nuint)1,
            (Int128)1, (UInt128)1, new System.Numerics.BigInteger(1), (Half)1, 1f, 1d, 1.00m, DayOfWeek.Monday, '1', "1"];

        foreach (var value in ones)
            CompositeKey.Create(value).ToCanonicalString().Should().Be("1", $"the format of {value.GetType()} is part of the persisted identity");

        CompositeKey.Create(true).ToCanonicalString().Should().Be("true");
        CompositeKey.Create(new byte[] { 1, 2 }).ToCanonicalString().Should().Be("AQI=");
        CompositeKey.Create(Guid.Empty).ToCanonicalString().Should().Be("00000000-0000-0000-0000-000000000000");
        CompositeKey.Create(new DateTime(2025, 1, 1)).ToCanonicalString().Should().Be("2025-01-01T00:00:00.0000000");
        CompositeKey.Create(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)).ToCanonicalString().Should()
            .Be("2025-01-01T00:00:00.0000000+00:00");
        CompositeKey.Create(TimeSpan.Zero).ToCanonicalString().Should().Be("00:00:00");
        CompositeKey.Create(new DateOnly(2025, 1, 1)).ToCanonicalString().Should().Be("2025-01-01");
        CompositeKey.Create(TimeOnly.MinValue).ToCanonicalString().Should().Be("00:00:00.0000000");
    }

    [Test]
    public void ToCanonicalString_RejectsUnsupportedTypes()
    {
        Action format = () => CompositeKey.Create(new object()).ToCanonicalString();

        format.Should().Throw<NotSupportedException>();
    }

    [Test]
    public void ToString_UsesInvariantFormatting()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            CompositeKey.Create((Half)1.5, 1.5f, 1.5d, 1.5m).ToString().Should().Be("1.5~1.5~1.5~1.5");
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

    private sealed class FormattableValue : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) => 1.5m.ToString(format, formatProvider);
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

    [Test]
    public void Equality_ByteArray_Compares_By_Content()
    {
        CompositeKey single1 = new byte[] { 0x01, 0x02 };
        CompositeKey single2 = new byte[] { 0x01, 0x02 };
        single1.Should().Be(single2);
        single1.GetHashCode().Should().Be(single2.GetHashCode());
        single1.Should().NotBe((CompositeKey)new byte[] { 0x01, 0x03 });

        var multi1 = CompositeKey.Create("a", new byte[] { 0x01, 0x02 });
        var multi2 = CompositeKey.Create("a", new byte[] { 0x01, 0x02 });
        multi1.Should().Be(multi2);
        multi1.GetHashCode().Should().Be(multi2.GetHashCode());
        multi1.Should().NotBe(CompositeKey.Create("a", new byte[] { 0x02 }));

        var args1 = new CompositeKey("a", new byte[] { 0x01, 0x02 }, 3);
        var args2 = new CompositeKey("a", new byte[] { 0x01, 0x02 }, 3);
        args1.Should().Be(args2);
        args1.GetHashCode().Should().Be(args2.GetHashCode());
    }

    [Test]
    public void Equality_GenericCreate_And_Params_Are_Equivalent()
    {
        var id = Guid.NewGuid();
        var generic = CompositeKey.Create(id, 7);
        var args = new CompositeKey(id, 7);
        generic.Should().Be(args);
        generic.GetHashCode().Should().Be(args.GetHashCode());
        CompositeKey.Create<string?>(null).Args.Should().Equal([null]);

        default(CompositeKey).Args.Should().BeEmpty();
        default(CompositeKey).Should().Be(new CompositeKey());
        default(CompositeKey).GetHashCode().Should().Be(new CompositeKey().GetHashCode());
    }
}
