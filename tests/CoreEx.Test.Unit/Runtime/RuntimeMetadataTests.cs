using CoreEx.Entities;
using CoreEx.Localization;
using CoreEx.Metadata;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Unit.Runtime;

public partial class RuntimeMetadataTests
{
    [Contract]
    private partial record class EntityR
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    [Contract]
    private partial class EntityA
    {
        public int Id { get; set; } = 88;

        [Display(Name = "NAME")]
        [JsonPropertyName("fullname")]
        public string? Name { get; set; } = "Bob";

        [ContractIgnore]
        public bool Internal { get; set; }
    }

    [Contract]
    private partial class EntityB : EntityA
    {
        [Display(Name = "AMOUNT")] // Should be ignored; as LText is specified and it takes precedence.
        [Localization("EntityB.Amount", "Dollars")]
        [DisplayFormat(DataFormatString = "{0:C}")]
        public decimal Amount { get; set; }
    }

    [Contract]
    private partial class EntityC : EntityB
    {
        public List<EntityA>? Children { get; set; }
        public Dictionary<string, EntityA>? Kids { get; set; }
        public ChangeLog? ChangeLog { get; set; }
    }

    [Contract]
    private partial class EntityD(string id) : IReadOnlyIdentifier<string>
    {
        public string Id => id;

        [String(casing: StringCase.Upper)]
        public partial string? Code { get; set; }

        [Clean(CleanOption.None)]
        public string? Description { get; set; }

        public ChangeLog? ChangeLog { get; set; }

        [Clean(CleanOption.Clean)]
        public ChangeLog? ChangeLog2 { get; set; }

        [DateTime(DateTimeTransform.DateOnly)]
        public partial DateTime? Date { get; set; } 

        public List<string>? Tags { get; set; }

        public string[] Strings { get; set; } = [];
    }

    [Contract]
    private partial class EntityX
    {
        [DateTime(DateTimeTransform.DateOnly)]
        public partial DateTime D1 { get; set; }

        [DateTime(DateTimeTransform.DateOnly)]
        public required partial DateTime D2 { get; init; }

        [String(casing: StringCase.Upper)]
        public partial string? S1 { get; set; }

        [String(casing: StringCase.Upper)]
        public partial string? S2 { get; init; }
    }

    [Test]
    public void AreEqual_ReferenceEquals_True()
    {
        var obj = new object();
        RuntimeMetadata.AreEqual(obj, obj).Should().BeTrue();
    }

    [Test]
    public void AreEqual_NullCases()
    {
        RuntimeMetadata.AreEqual<object>(null, null).Should().BeTrue();
        RuntimeMetadata.AreEqual<object>(null, new object()).Should().BeFalse();
        RuntimeMetadata.AreEqual<object>(new object(), null).Should().BeFalse();
    }

    [Test]
    public void AreEqual_Strings()
    {
        RuntimeMetadata.AreEqual("abc", "abc").Should().BeTrue();
        RuntimeMetadata.AreEqual("abc", "def").Should().BeFalse();
    }

    [Test]
    public void AreEqual_IEnumerable_ValueType()
    {
        var arr1 = new[] { 1, 2, 3 };
        var arr2 = new[] { 1, 2, 3 };
        var arr3 = new[] { 1, 2, 4 };
        var arr4 = new[] { 1, 2 };
        RuntimeMetadata.AreEqual(arr1, arr2).Should().BeTrue();
        RuntimeMetadata.AreEqual(arr1, arr3).Should().BeFalse();
        RuntimeMetadata.AreEqual(arr1, arr4).Should().BeFalse();
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
    public void AreEqual_Dictionary()
    {
        var e1 = new EntityC { Kids = [] };
        e1.Kids.Add("Sal", new EntityA { Name = "Sal" });
        e1.Kids.Add("Kev", new EntityA { Name = "Kev" });

        var e2 = new EntityC { Kids = [] };
        e2.Kids.Add("Kev", new EntityA { Name = "Kev" });
        e2.Kids.Add("Sal", new EntityA { Name = "Sal" });

        e1.Equals(e2).Should().BeTrue();

        e2.Kids["Kev"].Name = "Jon";
        e1.Equals(e2).Should().BeFalse();

        e2.Kids.Remove("Kev");
        e2.Kids.Add("Jas", new EntityA { Name = "Jas" });
        e1.Equals(e2).Should().BeFalse();
    }

    [Test]
    public void AreEqual_IRuntimeMetadata()
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
    public void GetHashCode_Null_Zero()
    {
        RuntimeMetadata.GetHashCode<object>(null).Should().Be(0);
    }

    [Test]
    public void GetHashCode_String()
    {
        RuntimeMetadata.GetHashCode("abc").Should().Be("abc".GetHashCode());
    }

    [Test]
    public void GetHashCode_IRuntimeMetadata()
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
    public void GetHashCode_Default()
    {
        RuntimeMetadata.GetHashCode(7).Should().Be(7.GetHashCode());
    }

    [Test]
    public void IsDefault_TrueAndFalse()
    {
        var e1 = new EntityA { Id = 1, Name = "Mary" };
        e1.IsDefault().Should().BeFalse();

        // This is because the Contract generated code sets the default values.
        e1.Id = 88;
        e1.Name = "Bob";
        e1.IsDefault().Should().BeTrue();
    }

    [Test]
    public void IsDefault_Empty_ICollection()
    {
        EntityC? e = new()
        {
            Children = [],
            Kids = []
        };

        e.IsDefault().Should().BeTrue();

        e.Children = [new EntityA()];
        e.IsDefault().Should().BeFalse();

        e.Children = null;
        e.IsDefault().Should().BeTrue();

        e.Kids = new Dictionary<string, EntityA> { { "A", new EntityA() } };
        e.IsDefault().Should().BeFalse();

        e.Kids = null;
        e.IsDefault().Should().BeTrue();
    }

    [Test]
    public void CopyFrom_CopiesValues()
    {
        var e1 = new EntityB { Id = 1, Name = "Bob", Amount = 1.3m };
        var e2 = new EntityB();
        e2.CopyFrom(e1);
        e2.Equals(e1).Should().BeTrue();

        var eA = new EntityA { Id = 7, Name = "Fred" };
        e2.CopyFrom(eA);
        e2.Id.Should().Be(7);
        e2.Name.Should().Be("Fred");

        var eC = new EntityC { Id = 11, Name = "Jen", Amount = 2.3m, ChangeLog = new ChangeLog { CreatedBy = "Dave" }, Children = [new EntityA { Id = 10, Name = "Jen" }] };
        e2.CopyFrom(eC);
        e2.Id.Should().Be(11);
        e2.Name.Should().Be("Jen");
        e2.Amount.Should().Be(2.3m);
    }

    [Test]
    public void CopyFrom_TypeNotAssignable_DoesNothing()
    {
        var e1 = new EntityB { Id = 1, Name = "Bob", Amount = 1.3m };
        var e2 = new ChangeLog();
        e1.CopyFrom(e2);

        e1.Id.Should().Be(1);
        e1.Name.Should().Be("Bob");
        e1.Amount.Should().Be(1.3m);
    }

    [Test]
    public void PropertyRuntimeMetadata_Text()
    {
        var a = new EntityA();
        var pd = a.GetPropertyRuntimeMetadata().ToDictionary(p => p.Name);

        var idProp = pd["Id"];
        idProp.Text.KeyAndOrText.Should().Be("Id");
        idProp.Text.FallbackText.Should().Be("Identifier"); // Not specified, so using Name.ToSentenceCase().

        var nameProp = pd["Name"];
        nameProp.Text.KeyAndOrText.Should().Be("Name");
        nameProp.Text.FallbackText.Should().Be("NAME"); // From DisplayAttribute.

        var b = new EntityB();
        var pd2 = b.GetPropertyRuntimeMetadata().ToDictionary(p => p.Name);

        var amtProp = pd2["Amount"];
        amtProp.Text.KeyAndOrText.Should().Be("EntityB.Amount");
        amtProp.Text.FallbackText.Should().Be("Dollars"); // From LTextAttribute, which takes precedence over DisplayAttribute.
    }

    [Test]
    public void Read_Only_Property()
    {
        var d = new EntityD("123")
        {
            Code = "abc",
            Description = "xyz"
        };

        // Copy all properties except Id (as readonly).
        var d2 = new EntityD("456");
        d2.CopyFrom(d);
        d2.Id.Should().Be("456");
        d2.Code.Should().Be("ABC");
        d2.Description.Should().Be("xyz");
        d2.IsDefault().Should().BeFalse();

        // Check equality.
        d.Equals(d2).Should().BeFalse();
        d.GetHashCode().Should().NotBe(d2.GetHashCode());

        d2 = new EntityD("123");
        d2.CopyFrom(d);
        d.Equals(d2).Should().BeTrue();
        d.GetHashCode().Should().Be(d2.GetHashCode());

        d2 = new EntityD(null!);
        d2.IsDefault().Should().BeTrue();
    }

    [Test]
    public void Clean_Including_Property_Graph()
    {
        var d = new EntityD("123")
        {
            Code = "abc  ",
            Description = "  xyz   ",
            Date = DateTime.Now,
            ChangeLog = new ChangeLog() { CreatedBy = "" },
            ChangeLog2 = new ChangeLog() { CreatedBy = "" },
            Tags = [],
            Strings = []
        };

        Cleaner.Clean(d);

        d.Id.Should().Be("123");
        d.Code.Should().Be("ABC");
        d.Description.Should().Be("  xyz   ");
        d.Date.Value.Kind.Should().Be(DateTimeKind.Unspecified);
        d.Date.Value.TimeOfDay.Should().Be(TimeSpan.Zero);
        d.ChangeLog.Should().BeNull();
        d.ChangeLog2.Should().NotBeNull();
        d.ChangeLog2.IsDefault().Should().BeTrue();
        d.Tags.Should().BeNull(); // List
        d.Strings.Should().NotBeNull().And.HaveCount(0); // Array
    }

    [Test]
    public void GetPropertyRuntimeMetadata_Type_With_Reflection()
    {
        var prms = RuntimeMetadata.GetPropertyRuntimeMetadata(typeof(EntityC), "Kids", "Internal").ToArray();
        prms.Length.Should().Be(5);
        prms.Select(x => x.Name).Should().BeEquivalentTo(["Id", "Name", "Amount", "ChangeLog", "Children"]);

        var c = new EntityC { Name = "BARRY" };
        var p = prms.Single(p => p.Name == "Name");
        p.GetValue(c).Should().Be("BARRY");

        p.SetValue(c, "FRED");
        c.Name.Should().Be("FRED");
    }

    [Test]
    public void GetPropertyRuntimeMetadata_T_With_Reflection()
    {
        var prms = RuntimeMetadata.GetPropertyRuntimeMetadata<EntityE>().ToArray();
        prms.Length.Should().Be(2);
        prms.Select(x => x.Name).Should().BeEquivalentTo(["Id", "Name"]);

        var c = new EntityE(88) { Name = "BARRY" };
        var p = prms.Single(p => p.Name == "Name");
        p.GetValue(c).Should().Be("BARRY");

        p.SetValue(c, "FRED");
        c.Name.Should().Be("FRED");
    }

    [Test]
    public void GetPropertyRuntimeMetadata_PropertyExpression_IEntity()
    {
        var prm = RuntimeMetadata.GetForExpression<EntityC, string?>(c => c.Name);
        prm.Name.Should().Be("Name");
        prm.GetValue<string?>(new EntityC { Name = "BARRY" }).Should().Be("BARRY");
    }

    [Test]
    public void GetPropertyRuntimeMetadata_PropertyExpression_Class()
    {
        var prm = RuntimeMetadata.GetForExpression<EntityE, string?>(e => e.Name);
        prm.Name.Should().Be("Name");
        prm.GetValue<string?>(new EntityE(88) { Name = "BARRY" }).Should().Be("BARRY");
    }

    [Test]
    public void CopyInto_With_Reflection()
    {
        var e1 = new EntityE(1) { Name = "Bob" };
        var e2 = new EntityE(2) { Name = "Kate" };

        RuntimeMetadata.CopyInto(e1, e2);
        e2.Id.Should().Be(2); // Not copied as read-only.
        e2.Name.Should().Be("Bob");
    }

    public class EntityE(int id)
    {
        public int Id { get; } = id;
        public string? Name { get; set; }
    }
}