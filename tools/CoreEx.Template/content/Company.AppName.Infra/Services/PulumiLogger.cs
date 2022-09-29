using System;
using Microsoft.Extensions.Logging;

namespace Company.AppName.Infra.Services;

public class PulumiLogger<T> : ILogger<T>
{
    public static readonly PulumiLogger<T> Instance = new();

    private PulumiLogger()
    { }

    public IDisposable BeginScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        throw new NotImplementedException();
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
            case LogLevel.Debug:
                Pulumi.Log.Debug(formatter(state, exception));
                break;
            case LogLevel.Information:
                Pulumi.Log.Info(formatter(state, exception));
                break;
            case LogLevel.Warning:
                Pulumi.Log.Warn(formatter(state, exception));
                break;
            case LogLevel.Error:
            case LogLevel.Critical:
                Pulumi.Log.Error(formatter(state, exception));
                break;
        }
    }
}