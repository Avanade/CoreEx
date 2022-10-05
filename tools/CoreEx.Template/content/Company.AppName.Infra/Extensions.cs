using System;
using System.Threading.Tasks;
using Pulumi;

namespace Company.AppName.Infra;

public static class Extensions
{
    public static Input<string> GetConfigValue(string name, Input<string> defaultValue)
    {
        var config = new Config();

        var configValue = config.Get(name);

        if (string.IsNullOrEmpty(configValue))
        {
            Log.Info($"Defaulting {name} because it wasn't present in configuration");
            return defaultValue.ToOutput();
        }
        else
        {
            return Output.Create(configValue);
        }
    }

    public static Task<T> GetValue<T>(this Output<T> output) => output.GetValue(_ => _);

    public static Task<TResult> GetValue<T, TResult>(this Output<T> output, Func<T, TResult> valueResolver)
    {
        var tcs = new TaskCompletionSource<TResult>();
        output.Apply(_ =>
        {
            var result = valueResolver(_);
            tcs.SetResult(result);
            return result;
        });
        return tcs.Task;
    }

    public static Task<T> GetValue<T>(this Input<T> input) => input.GetValue(_ => _);

    public static Task<TResult> GetValue<T, TResult>(this Input<T> input, Func<T, TResult> valueResolver)
    {
        var tcs = new TaskCompletionSource<TResult>();
        input.Apply(_ =>
        {
            var result = valueResolver(_);
            tcs.SetResult(result);
            return result;
        });
        return tcs.Task;
    }
}