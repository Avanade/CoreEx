using CoreEx.Entities;
using CoreEx.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Unit.Runtime;

public class ReflectionMetadataTests
{
    private record class EntityR
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    private class EntityA
    {
        public int Id { get; set; } = 88;

        [Display(Name = "NAME")]
        [JsonPropertyName("fullname")]
        public string? Name { get; set; } = "Bob";

        [ContractIgnore]
        public bool Internal { get; set; }

        public override bool Equals(object? obj) => RuntimeMetadata.AreEqual(this, obj);
        public override int GetHashCode() => RuntimeMetadata.GetHashCode(this);
    }

    private class EntityB : EntityA
    {
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; }

        public override bool Equals(object? obj) => RuntimeMetadata.AreEqual(this, obj);
        public override int GetHashCode() => RuntimeMetadata.GetHashCode(this);
    }

    private class EntityC : EntityB
    {
        public ChangeLog? ChangeLog { get; set; }
        public List<EntityA>? Children { get; set; }
        public Dictionary<string, EntityA>? Kids { get; set; }

        public override bool Equals(object? obj) => RuntimeMetadata.AreEqual(this, obj);
        public override int GetHashCode() => RuntimeMetadata.GetHashCode(this);
    }

    private class EntityD(string id) : IReadOnlyIdentifier<string>
    {
        public string Id => id;

        [String(casing: StringCase.Upper)]
        public string? Code { get; set; }

        [Clean(CleanOption.None)]
        public string? Description { get; set; }

        public ChangeLog? ChangeLog { get; set; }

        [Clean(CleanOption.Clean)]
        public ChangeLog? ChangeLog2 { get; set; }

        [DateTime(DateTimeTransform.DateOnly)]
        public DateTime? Date { get; set; }

        public List<string>? Tags { get; set; }

        public string[] Strings { get; set; } = [];

        public override bool Equals(object? obj) => RuntimeMetadata.AreEqual(this, obj);
        public override int GetHashCode() => RuntimeMetadata.GetHashCode(this);
    }

    private partial class EntityX
    {
        [DateTime(DateTimeTransform.DateOnly)]
        public DateTime D1 { get; set; }

        [DateTime(DateTimeTransform.DateOnly)]
        public required DateTime D2 { get; init; }

        [String(casing: StringCase.Upper)]
        public string? S1 { get; set; }

        [String(casing: StringCase.Upper)]
        public string? S2 { get; init; }

        public override bool Equals(object? obj) => RuntimeMetadata.AreEqual(this, obj);
        public override int GetHashCode() => RuntimeMetadata.GetHashCode(this);
    }

    [Test]
    public void AreEqual_IEnumerable_ReferenceType()
    {
        var arr1 = new[] { new EntityA { Name = "Bob" }, new EntityA { Name = "Jen" } };
        var arr2 = new[] { new EntityA { Name = "Bob" }, new EntityA { Name = "Jen" } };
        var arr3 = new[] { new EntityA { Name = "Bob" }, new EntityA { Name = "Kat" } };
        var arr4 = new[] { new EntityA { Name = "Bob" } };
        RuntimeMetadata.AreEqual(arr1, arr2).Should().BeTrue();
        RuntimeMetadata.AreEqual(arr1, arr3).Should().BeFalse();
        RuntimeMetadata.AreEqual(arr1, arr4).Should().BeFalse();
    }

    [Test]
    public void AreEqual_Reflection()
    {
        var e1 = new EntityC { Id = 1, Name = "Bob", Amount = 1.3m, ChangeLog = new ChangeLog { CreatedBy = "Dave" }, Children = [new EntityA { Id = 10, Name = "Jen" }] };
        var e2 = new EntityC { Id = 1, Name = "Bob", Amount = 1.3m, ChangeLog = new ChangeLog { CreatedBy = "Dave" }, Children = [new EntityA { Id = 10, Name = "Jen" }] };
        e1.Equals(e2).Should().BeTrue();

        e2.Name = "Fred";
        e1.Equals(e2).Should().BeFalse();

        e2.Name = "Bob";
        e1.Equals(e2).Should().BeTrue();

        e2.ChangeLog = new ChangeLog { CreatedBy = "Mary" };
        e1.Equals(e2).Should().BeFalse();

        e2.ChangeLog = new ChangeLog { CreatedBy = "Dave" };
        e1.Equals(e2).Should().BeTrue();

        e2.Children[0].Name = "Kat";
        e1.Equals(e2).Should().BeFalse();

        e2.Children = [new EntityA { Id = 10, Name = "Jen" }, new EntityA { Id = 11, Name = "Sam" }];
        e1.Equals(e2).Should().BeFalse();
        var e2AsA = (EntityA)e2;
        e1.Equals(e2AsA).Should().BeFalse();

        e2.Children = [new EntityA { Id = 10, Name = "Jen" }];
        e1.Equals(e2).Should().BeTrue();
        e1.Equals(e2AsA).Should().BeTrue();
    }

    [Test]
    public void GetHashCode_Reflection()
    {
        var e1 = new EntityA { Id = 1, Name = "Bob" };
        var e2 = new EntityA { Id = 1, Name = "Bob" };
        e1.GetHashCode().Should().Be(e2.GetHashCode());

        e2.Name = "Fred";
        e1.GetHashCode().Should().NotBe(e2.GetHashCode());
    }

    [Test]
    public void GetHashCode_IEnumerable()
    {
        var arr1 = new[] { 1, 2, 3 };
        var arr2 = new[] { 1, 2, 3 };
        RuntimeMetadata.GetHashCode(arr1).Should().Be(RuntimeMetadata.GetHashCode(arr2));
    }

    [Test]
    public void IsDefault_TrueAndFalse()
    {
        var e1 = new EntityA { Id = 1, Name = "Mary" };
        RuntimeMetadata.IsDefault(e1).Should().BeFalse();

        e1.Id = 88;
        e1.Name = "Bob";
        RuntimeMetadata.IsDefault(e1).Should().BeFalse(); // This is because the default values are set in the class definition and cannot be determined using reflection.

        e1.Id = default;
        e1.Name = default;
        RuntimeMetadata.IsDefault(e1).Should().BeTrue();
    }

    [Test]
    public void Test()
    {
        int[] arr1 = { 1, 2, 3 };
        int[]? arr2 = { 1 };

        arr1 = default!;
        arr2 = default;
    }
}