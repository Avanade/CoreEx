using System.Net;

namespace CoreEx.Test.Unit;

[TestFixture]
public class ExtendedExceptionExtensionsTests
{
    [Test]
    public void WithErrorCode_SetsErrorCode()
    {
        var ex = new BusinessException(null, null).WithErrorCode("ERR001");
        ex.ErrorCode.Should().Be("ERR001");
    }

    [Test]
    public void WithErrorType_SetsErrorType()
    {
        var ex = new BusinessException(null, null).WithErrorType("custom-type");
        ex.ErrorType.Should().Be("custom-type");
    }

    [Test]
    public void WithStatusCode_SetsStatusCode()
    {
        var ex = new BusinessException(null, null).WithStatusCode(HttpStatusCode.Conflict);
        ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public void WithDetail_SetsDetail()
    {
        var ex = new BusinessException(null, null).WithDetail("more info");
        ex.Detail.Should().Be("more info");
    }

    [Test]
    public void WithKey_SetsExtensionUnderKeyName()
    {
        var ex = new BusinessException(null, null).WithKey(123);
        ex.Extensions.Should().ContainKey("key").WhoseValue.Should().Be(123);
    }

    [Test]
    public void WithExtension_SetsNamedExtension()
    {
        var ex = new BusinessException(null, null).WithExtension("custom", "value");
        ex.Extensions.Should().ContainKey("custom").WhoseValue.Should().Be("value");
        ex.HasExtensions.Should().BeTrue();
    }

    [Test]
    public void WithExtension_NullOrEmptyName_Throws()
    {
        Action act = () => new BusinessException(null, null).WithExtension(string.Empty, "value");
        act.Should().Throw<ArgumentException>();
    }

    [Test]
    public void AsTransient_SetsIsTransientAndDefaultRetryAfter()
    {
        var ex = new BusinessException(null, null).AsTransient();
        ex.IsTransient.Should().BeTrue();
        ex.RetryAfter.Should().Be(TransientException.DefaultRetryAfter);
    }

    [Test]
    public void AsTransient_WithExplicitRetryAfter_UsesIt()
    {
        var retry = TimeSpan.FromSeconds(30);
        var ex = new BusinessException(null, null).AsTransient(retry);
        ex.RetryAfter.Should().Be(retry);
    }

    [Test]
    public void FluentChaining_CombinesMultipleBuilders()
    {
        var ex = new BusinessException("msg")
            .WithErrorCode("E1")
            .WithErrorType("custom")
            .WithStatusCode(HttpStatusCode.BadRequest)
            .WithDetail("detail")
            .WithKey("k1")
            .AsTransient();

        ex.ErrorCode.Should().Be("E1");
        ex.ErrorType.Should().Be("custom");
        ex.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        ex.Detail.Should().Be("detail");
        ex.Extensions["key"].Should().Be("k1");
        ex.IsTransient.Should().BeTrue();
    }
}
