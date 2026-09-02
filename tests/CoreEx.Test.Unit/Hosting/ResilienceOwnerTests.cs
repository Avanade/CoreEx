using CoreEx.Hosting;
using Polly;

namespace CoreEx.Test.Unit.Hosting;

[TestFixture]
public class ResilienceOwnerTests
{
    [Test]
    public void GetOwner_ReturnsPreviouslySetOwner()
    {
        var owner = new TestOwner();
        var ctx = ResilienceContextPool.Shared.Get();
        try
        {
            ctx.Properties.Set(ResilienceOwner<TestOwner>.PropertyKey, owner);

            ResilienceOwner<TestOwner>.GetOwner(ctx).Should().BeSameAs(owner);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public void GetOwner_NotSet_ReturnsDefault()
    {
        var ctx = ResilienceContextPool.Shared.Get();
        try
        {
            ResilienceOwner<TestOwner>.GetOwner(ctx).Should().BeNull();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    [Test]
    public void PropertyKey_IsDistinctPerOwnerType()
    {
        // Different closed generic types must not collide on the same underlying property key name (each TOwner gets its own key based on its own full type name).
        var ctx = ResilienceContextPool.Shared.Get();
        try
        {
            var owner = new TestOwner();
            var otherOwner = new OtherTestOwner();

            ctx.Properties.Set(ResilienceOwner<TestOwner>.PropertyKey, owner);
            ctx.Properties.Set(ResilienceOwner<OtherTestOwner>.PropertyKey, otherOwner);

            ResilienceOwner<TestOwner>.GetOwner(ctx).Should().BeSameAs(owner);
            ResilienceOwner<OtherTestOwner>.GetOwner(ctx).Should().BeSameAs(otherOwner);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    private sealed class TestOwner;

    private sealed class OtherTestOwner;
}
