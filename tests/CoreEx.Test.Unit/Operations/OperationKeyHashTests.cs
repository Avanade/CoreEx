namespace CoreEx.Test.Unit.Operations;

[TestFixture]
public class OperationKeyHashTests
{
    [Test]
    public void Compute_Is_CompositeKey_Canonical_Hash() => KeyHash.Compute("D", "T", "Op", "1").Should().Equal(CompositeKey.Create("D", "T", "Op", "1").ToCanonicalHash());

    [Test]
    public void Compute_Distinguishes_Absent_And_Empty_Tenant() => KeyHash.Compute("D", null, "Op", "1").Should().NotEqual(KeyHash.Compute("D", "", "Op", "1"));

    private abstract class KeyHash() : OperationStoreBase(null!, null!)
    {
        public static byte[] Compute(string domainName, string? tenantId, string operationName, string instanceId) => ComputeKeyHash(domainName, tenantId, operationName, instanceId);
    }
}
