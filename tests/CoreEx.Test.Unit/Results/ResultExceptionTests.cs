namespace CoreEx.Test.Unit.Results;

public class ResultExceptionTests
{
    [Test]
    public void FailureBoundaries_PreserveException(
        [Values("Result.ThrowOnError", "Result<T>.ThrowOnError", "Result<T>.Value", "IResult.Value", "IResult<T>.Value")] string boundary,
        [Values] bool previouslyThrown,
        [Values("ordinary", "cancellation", "aggregate")] string kind)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception error = kind switch
        {
            "cancellation" => new OperationCanceledException(cancellation.Token),
            "aggregate" => new AggregateException(new InvalidOperationException("Original failure.")),
            _ => new InvalidOperationException("Original failure.")
        };
        if (previouslyThrown)
            error = CaptureOriginalFailure(error);

        var originalStack = error.StackTrace;
        var result = new Result(error);
        var typedResult = new Result<int>(error);
        Action action = boundary switch
        {
            "Result.ThrowOnError" => () => result.ThrowOnError(),
            "Result<T>.ThrowOnError" => () => typedResult.ThrowOnError(),
            "Result<T>.Value" => () => { _ = typedResult.Value; },
            "IResult.Value" => () => { _ = ((IResult)result).Value; },
            _ => () => { _ = ((IResult<int>)typedResult).Value; }
        };

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var thrown = Assert.Catch(() => action());
            thrown.Should().BeSameAs(error);
            thrown!.GetType().Should().Be(error.GetType());
            thrown.StackTrace.Should().NotBeNullOrEmpty();
            if (originalStack is not null)
                thrown.StackTrace.Should().Contain(originalStack);

            if (thrown is OperationCanceledException canceled)
                canceled.CancellationToken.Should().Be(cancellation.Token);

            if (thrown is AggregateException aggregate)
                aggregate.InnerExceptions.Should().ContainSingle().Which.Should().BeOfType<InvalidOperationException>();
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static Exception CaptureOriginalFailure(Exception error)
    {
        try
        {
            throw error;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
