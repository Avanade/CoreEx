using CoreEx.Http;
using CoreEx.Http.Abstractions;
using System.Net;

namespace CoreEx.Test.Unit.Http;

[TestFixture]
public class ProblemDetailsExceptionTests
{
    [Test]
    public void Constructor_UsesTitleOrDetailAsMessage()
    {
        var pdWithTitle = new ProblemDetails { Title = "My Title", Detail = "My Detail" };
        var ex1 = new ProblemDetailsException(pdWithTitle, null);
        ex1.Message.Should().Be("My Title");

        var pdNoTitle = new ProblemDetails { Detail = "Only Detail" };
        var ex2 = new ProblemDetailsException(pdNoTitle, null);
        ex2.Message.Should().Be("Only Detail");
    }

    [Test]
    public void ToException_MapsStandardProperties()
    {
        var pd = new ProblemDetails
        {
            Title = "err",
            Detail = "some detail",
            Status = (int)HttpStatusCode.Conflict,
            ErrorType = "custom-type",
            ErrorCode = "CODE1"
        };
        var pde = new ProblemDetailsException(pd, null);

        var ex = pde.ToException<ValidationException>();

        ex.Detail.Should().Be("some detail");
        ex.StatusCode.Should().Be(HttpStatusCode.Conflict);
        ex.ErrorType.Should().Be("custom-type");
        ex.ErrorCode.Should().Be("CODE1");
    }

    [Test]
    public void ToException_ExtensionsDictionary_MapsErrorCodeAndTypeAndOthers()
    {
        var pd = new ProblemDetails
        {
            Title = "err",
            Extensions = new Dictionary<string, object?>
            {
                { HttpNames.ErrorCodeName, "FROM-EXT-CODE" },
                { HttpNames.ErrorTypeName, "from-ext-type" },
                { "custom", "value" }
            }
        };
        var pde = new ProblemDetailsException(pd, null);

        var ex = pde.ToException<ValidationException>();

        ex.ErrorCode.Should().Be("FROM-EXT-CODE");
        ex.ErrorType.Should().Be("from-ext-type");
        ex.Extensions.Should().ContainKey("custom").WhoseValue.Should().Be("value");
        ex.Extensions.Should().NotContainKey(HttpNames.ErrorCodeName);
        ex.Extensions.Should().NotContainKey(HttpNames.ErrorTypeName);
    }

    [Test]
    public void TryGetBusinessException_WhenErrorTypeMatches_ReturnsTrueAndException()
    {
        var pd = new ProblemDetails { Title = "biz error", ErrorType = BusinessException.BusinessErrorType };
        var pde = new ProblemDetailsException(pd, null);

        var result = pde.TryGetBusinessException(out var ex);

        result.Should().BeTrue();
        ex.Should().NotBeNull();
        ex!.Message.Should().Be("biz error");
    }

    [Test]
    public void TryGetBusinessException_WhenErrorTypeDoesNotMatch_ReturnsFalse()
    {
        var pd = new ProblemDetails { Title = "other error", ErrorType = "validation" };
        var pde = new ProblemDetailsException(pd, null);

        var result = pde.TryGetBusinessException(out var ex);

        result.Should().BeFalse();
        ex.Should().BeNull();
    }

    [Test]
    public void ThrowOnBusinessException_WhenBusinessError_Throws()
    {
        var pd = new ProblemDetails { Title = "biz error", ErrorType = BusinessException.BusinessErrorType };
        var pde = new ProblemDetailsException(pd, null);

        Action act = () => pde.ThrowOnBusinessException();

        act.Should().Throw<BusinessException>().WithMessage("biz error");
    }

    [Test]
    public void ThrowOnBusinessException_WhenNotBusinessError_ReturnsSelf_NoThrow()
    {
        var pd = new ProblemDetails { Title = "other", ErrorType = "validation" };
        var pde = new ProblemDetailsException(pd, null);

        var result = pde.ThrowOnBusinessException();

        result.Should().BeSameAs(pde);
    }
}
