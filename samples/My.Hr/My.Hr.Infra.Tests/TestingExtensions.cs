public static class TestingExtensions
{
    public static Task<T> GetValueAsync<T>(this Output<T> output)
    {
        var tcs = new TaskCompletionSource<T>();
        output.Apply(v => { tcs.SetResult(v); return v; });
        return tcs.Task;
    }

        public static Task<object> GetValueAsync(this Output<object?> output)
    {
        var tcs = new TaskCompletionSource<object>();
        output.Apply(v => { tcs.SetResult(v); return v; });
        return tcs.Task;
    }
}