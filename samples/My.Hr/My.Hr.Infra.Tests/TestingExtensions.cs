namespace My.Hr.Infra.Tests;

public static class TestingExtensions
{
    public static Task<T> GetValueAsync<T>(this Output<T> output)
    {
        var tcs = new TaskCompletionSource<T>();
        output.Apply(v => { tcs.SetResult(v); return v; });
        return tcs.Task;
    }

    public static Task<T> GetValueAsync<T>(this object outputObj)
    {
        if (outputObj is Output<T> output)
        {
           return output.GetValueAsync();
        }

        return Task.FromException<T>(new ArgumentException("Provided object is not Output<T>", nameof(outputObj)));
    }
}