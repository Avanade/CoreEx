namespace CoreEx.Test.Unit;

[TestFixture]
public class OperationKeyTests
{
    [Test]
    public void Identity_Uses_All_Components()
    {
        var first = OperationKey.ForTenant("orders", "tenant-a", "import", "instance-1");
        var same = new OperationKey(OperationKeyScope.Tenant, "orders", "tenant-a", "import", "instance-1");
        var otherTenant = OperationKey.ForTenant("orders", "tenant-b", "import", "instance-1");
        var domainWide = OperationKey.ForDomain("orders", "import", "instance-1");
        var otherDomain = OperationKey.ForTenant("products", "tenant-a", "import", "instance-1");
        var otherOperation = OperationKey.ForTenant("orders", "tenant-a", "export", "instance-1");
        var otherInstance = OperationKey.ForTenant("orders", "tenant-a", "import", "instance-2");
        var otherCase = OperationKey.ForTenant("Orders", "tenant-a", "import", "instance-1");

        same.Should().Be(first);
        otherTenant.Should().NotBe(first);
        domainWide.Should().NotBe(first);
        otherDomain.Should().NotBe(first);
        otherOperation.Should().NotBe(first);
        otherInstance.Should().NotBe(first);
        otherCase.Should().NotBe(first);
    }

    [Test]
    public void Identity_Does_Not_Concatenate_Components()
    {
        var first = OperationKey.ForTenant("a|b", "c", "import", "same");
        var second = OperationKey.ForTenant("a", "b|c", "import", "same");

        first.Should().NotBe(second);
    }

    [Test]
    public void Scope_Requires_Tenant_Only_For_Tenant_Operations()
    {
        Action missingTenant = () => _ = new OperationKey(OperationKeyScope.Tenant, "orders", null, "import", "instance");
        Action unexpectedTenant = () => _ = new OperationKey(OperationKeyScope.DomainWide, "orders", "tenant-a", "import", "instance");

        missingTenant.Should().Throw<ArgumentException>();
        unexpectedTenant.Should().Throw<ArgumentException>();
    }

    [Test]
    public void Composite_InstanceId_Uses_The_PerCall_Formatter()
    {
        static string Format(CompositeKey value) => JsonSerializer.Serialize(value.Args);

        var instanceId = CompositeKey.Create("order,1", 2);

        OperationKey.ForDomain("orders", "import", instanceId, Format).InstanceId.Should().Be("[\"order,1\",2]");
        OperationKey.ForTenant("orders", "tenant-a", "import", instanceId, Format).InstanceId.Should().Be("[\"order,1\",2]");
    }

    [Test]
    public void Composite_InstanceId_Default_Uses_The_CollisionResistant_Formatter()
    {
        var instanceId = CompositeKey.Create("order,1", 2);
        const string expected = "2:1:s28:006F0072006400650072002C00313:i321:2";

        OperationKey.ForDomain("orders", "import", instanceId).InstanceId.Should().Be(expected);
        OperationKey.ForTenant("orders", "tenant-a", "import", instanceId).InstanceId.Should().Be(expected);
        OperationKey.ForDomain("orders", "import", CompositeKey.Create("a,b", "c"))
            .Should().NotBe(OperationKey.ForDomain("orders", "import", CompositeKey.Create("a", "b,c")));
    }

    [Test]
    public void Json_RoundTrip_Preserves_Complete_Identity()
    {
        var key = OperationKey.ForTenant("orders", "tenant-a", "import", "instance-1");
        var json = JsonSerializer.Serialize(key, JsonDefaults.SerializerOptions);

        JsonSerializer.Deserialize<OperationKey>(json, JsonDefaults.SerializerOptions).Should().Be(key);
    }
}
