using CoreEx.Json;

namespace CoreEx.Test.Unit.Json;

[TestFixture]
public class JsonMergeTests
{
    #region Merge

    [Test]
    public void Merge_Root_Nulls()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""null""", """null""");
        r.Should().Be("""null""");
    }

    [Test]
    public void Merge_Root_Intrinsics_Are_Replacement()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""true""", """0""");
        r.Should().Be("""true""");
    }

    [Test]
    public void Merge_Root_Arrays_Are_Replacement()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("[1,2,3]", "[4,5,6]");
        r.Should().Be("[1,2,3]");
    }

    [Test]
    public void Merge_Object_No_Merge()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{}""", """{"age":10}""");
        r.Should().Be("""{"age":10}""");
    }

    [Test]
    public void Merge_Objects_Combine()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"weight":100}""", """{"age":10}""");
        r.Should().Be("{\"weight\":100,\"age\":10}");
    }

    [Test]
    public void Merge_Objects_Replace_Property()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"age":100}""", """{"weight":100,"age":10}""");
        r.Should().Be("""{"age":100,"weight":100}""");
    }

    [Test]
    public void Merge_Objects_Remove_Property_With_Null()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"age":null}""", """{"age":100}""");
        r.Should().Be("""{}""");
    }

    [Test]
    public void Merge_Objects_Nested_Mix()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"person":{"name":"bob","groovy":true}}""", """{"person":{"name":"gary","age":10}}""");
        r.Should().Be("""{"person":{"name":"bob","groovy":true,"age":10}}""");
    }

    [Test]
    public void Merge_Object_Nested_Array_Unmerged()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"person":{}}""", """{"person":{"names":["bob","gary"]}}""");
        r.Should().Be("""{"person":{"names":["bob","gary"]}}""");
    }

    [Test]
    public void Merge_Object_Nested_Array_Replace()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"person":{"names":["bob","jim"]}}""", """{"person":{"names":["bob","gary","simon"]}}""");
        r.Should().Be("""{"person":{"names":["bob","jim"]}}""");
    }

    [Test]
    public void Merge_Object_Nested_Object_Array_Replace()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"persons":[{"Name":"bob","age":10}]}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""");
        r.Should().Be("""{"persons":[{"Name":"bob","age":10}]}""");
    }

    [Test]
    public void Merge_Object_Nested_Object_Array_Replace2()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"other":33}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""");
        r.Should().Be("""{"other":33,"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""");
    }

    [Test]
    public void Merge_Object_Nested_Object_Array_Remove()
    {
        var p = new JsonMergePatch();
        var r = p.Merge("""{"other":33,"persons":null}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""");
        r.Should().Be("""{"other":33}""");
    }

    #endregion

    #region TryMerge

    [Test]
    public void TryMerge_Root_Nulls()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""null""", """null""", out var r);
        c.Should().BeFalse();
        r.Should().BeNull();
    }

    [Test]
    public void TryMerge_Root_Intrinsics_Are_Replacement()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""true""", """0""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""true""");
    }

    [Test]
    public void TryMerge_Root_Arrays_Are_Replacement()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("[1,2,3]", "[4,5,6]", out var r);
        c.Should().BeTrue();
        r.Should().Be("[1,2,3]");
    }

    [Test]
    public void TryMerge_Object_No_Merge()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{}""", """{"age":10}""", out var r);
        c.Should().BeFalse();
        r.Should().BeNull();
    }

    [Test]
    public void TryMerge_Objects_Combine()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"weight":100}""", """{"age":10}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("{\"weight\":100,\"age\":10}");
    }

    [Test]
    public void TryMerge_Objects_Replace_Property()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"age":100}""", """{"weight":100,"age":10}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"age":100,"weight":100}""");
    }

    [Test]
    public void TryMerge_Objects_Remove_Property_With_Null()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"age":null}""", """{"age":100}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{}""");
    }

    [Test]
    public void TryMerge_Objects_Nested_Mix()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"person":{"name":"bob","groovy":true}}""", """{"person":{"name":"gary","age":10}}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"person":{"name":"bob","groovy":true,"age":10}}""");
    }

    [Test]
    public void TryMerge_Object_Nested_Array_Unmerged()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"person":{}}""", """{"person":{"names":["bob","gary"]}}""", out var r);
        c.Should().BeFalse();
        r.Should().BeNull();
    }

    [Test]
    public void TryMerge_Object_Nested_Array_Replace()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"person":{"names":["bob","jim"]}}""", """{"person":{"names":["bob","gary","simon"]}}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"person":{"names":["bob","jim"]}}""");
    }

    [Test]
    public void TryMerge_Object_Nested_Object_Array_Replace()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"persons":[{"Name":"bob","age":10}]}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"persons":[{"Name":"bob","age":10}]}""");
    }

    [Test]
    public void TryMerge_Object_Nested_Object_Array_Replace2()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"other":33}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"other":33,"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""");
    }

    [Test]
    public void TryMerge_Object_Nested_Object_Array_Remove()
    {
        var p = new JsonMergePatch();
        var c = p.TryMerge("""{"other":33,"persons":null}""", """{"persons":[{"Name":"simon","age":10},{"Name":"jack","age":30}]}""", out var r);
        c.Should().BeTrue();
        r.Should().Be("""{"other":33}""");
    }

    #endregion

    [Test]
    public void Merge_Into_Null()
    {
        var p = new JsonMergePatch();
        var r = p.Merge<Person>("""{"name":"bob","age":30}""", null!);
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeFalse();
        r.Value.Merged.Should().BeNull();
    }

    [Test]
    public void Merge_Into_Value_Type_Mismatch()
    {
        var p = new JsonMergePatch();
        var r = p.Merge<Person>("""{"name":"bob","age":"thirty"}""", null!);
        r.Should().NotBeNull();
        r.IsFailure.Should().BeTrue();
        r.Error.Should().NotBeNull();
    }

    [Test]
    public void Merge_Into_Value_With_Changes()
    {
        var p = new JsonMergePatch();
        var r = p.Merge<Person>("""{"name":"bob","age":30}""", new Person { Name = "Jerry", Address = new Address { Street = "Main" } });
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeTrue();
        ObjectComparer.Assert(new Person { Name = "bob", Age = 30, Address = new Address { Street = "Main" } }, r.Value.Merged);
    }

    [Test]
    public void Merge_Into_Value_Changes_Unchanged()
    {
        var p = new JsonMergePatch();
        var r = p.Merge<Person>("""{"x-name":"bob","x-age":30}""", new Person { Name = "Jerry", Address = new Address { Street = "Main" } });
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeTrue();  // Is true as the underlying JSON was modified; although did not result in change to value itself as 'x-' fields were ignored during final deserializtion.
        ObjectComparer.Assert(new Person { Name = "Jerry", Address = new Address { Street = "Main" } }, r.Value.Merged);
    }

    [Test]
    public void Merge_Into_Value_Unchained_Unchanged()
    {
        var p = new JsonMergePatch();
        var r = p.Merge<Person>("""{"name":"Jerry"}""", new Person { Name = "Jerry", Address = new Address { Street = "Main" } });
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeFalse();
        ObjectComparer.Assert(new Person { Name = "Jerry", Address = new Address { Street = "Main" } }, r.Value.Merged);
    }

    [Test]
    public async Task MergeAsync_Changed()
    {
        var p = new JsonMergePatch();
        var r = await p.MergeAsync<Person>(new BinaryData("""{"name":"bob","age":30}"""), _ => Task.FromResult<Person?>(new Person()));
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeTrue();
        ObjectComparer.Assert(new Person { Name = "bob", Age = 30 }, r.Value.Merged);
    }

    [Test]
    public async Task MergeAsync_Get_Null()
    {
        var p = new JsonMergePatch();
        var r = await p.MergeAsync<Person>(new BinaryData("""{"name":"bob","age":30}"""), _ => Task.FromResult<Person?>(null));
        r.Should().NotBeNull();
        r.IsSuccess.Should().BeTrue();
        r.Value.HasChanges.Should().BeFalse();
        r.Value.Merged.Should().BeNull();
    }

    public class Person
    {
        public string? Name { get; set; }
        public int Age { get; set; }
        public string[]? NickNames { get; set; }
        public Address? Address { get; set; }
        public List<Person>? Children { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }
}