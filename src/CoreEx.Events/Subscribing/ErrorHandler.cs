namespace CoreEx.Events.Subscribing;

/// <summary>
/// Provides the standardized error handling to ensure/enable consistency of behavior.
/// </summary>
public sealed class ErrorHandler
{
    private const string _logFormat = "{Message} [Source: {Source}, Handling: {Handling}]";
    private readonly List<HandlerConfig> _handlers = [];

    /// <summary>
    /// Indicates whether to automatically treat any <see cref="Exception"/> that implements <see cref="IExtendedException"/> that <see cref="IExtendedException.IsTransient"/> is <see langword="true"/>
    /// as transient and handle with a <see cref="ErrorHandling.Retry"/>.
    /// </summary>
    /// <remarks>Default to <see langword="false"/>.
    /// <para><i>Note:</i> the <see cref="EventSubscriberBase"/> overrides this to <see langword="true"/> to ensure this is the default desired functionality.</para></remarks>
    public bool AutoTransientHandling { get; set; } = false;

    /// <summary>
    /// Gets or sets the <see cref="ErrorHandling"/> to use where the <see cref="Exception"/> implements <see cref="IExtendedException"/> that <see cref="IExtendedException.IsError"/> is <see langword="true"/>.
    /// </summary>
    /// <remarks>This acts as a catch-all after the individual exception checks have occurred.
    /// <para><i>Note:</i> the <see cref="EventSubscriberBase"/> overrides this to <see cref="ErrorHandling.CompleteAsError"/> as it is assumes these should be treated as poison by default.</para></remarks>
    public ErrorHandling? WhereIsExtendedErrorHandling { get; set; }

    /// <summary>
    /// Adds the <see cref="ErrorHandling"/> for the specified <typeparamref name="TException"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/>.</typeparam>
    /// <param name="errorHandling">The <see cref="ErrorHandling"/>.</param>
    /// <returns>The current <see cref="ErrorHandler"/> to support fluent-style method chaining.</returns>
    /// <remarks>Will be checked in the sequence added.</remarks>
    public ErrorHandler Add<TException>(ErrorHandling errorHandling) where TException : Exception
    {
        _handlers.Add(new HandlerConfig { HandlingFactory = ex => ex is TException ? errorHandling : null });
        return this;
    }

    /// <summary>
    /// Adds the <paramref name="errorHandlingFactory"/> for the specified <typeparamref name="TException"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/>.</typeparam>
    /// <param name="errorHandlingFactory">The <see cref="ErrorHandling"/> factory.</param>
    /// <returns>The current <see cref="ErrorHandler"/> to support fluent-style method chaining.</returns>
    /// <remarks>Where a <see langword="null"/> is returned from the factory this indicates that the exception has not been handled and the next configured handler will be checked.
    /// <para>Will be checked in the sequence added.</para></remarks>
    public ErrorHandler Add<TException>(Func<TException, ErrorHandling?> errorHandlingFactory) where TException : Exception
    {
        _handlers.Add(new HandlerConfig { HandlingFactory = ex => ex is TException te ? errorHandlingFactory(te) : null });
        return this;
    }

    /// <summary>
    /// Adds the <see cref="ErrorHandling"/> for the specified <see cref="Type.IsAssignableFrom(Type?)"/> <typeparamref name="TException"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/>.</typeparam>
    /// <param name="errorHandling">The <see cref="ErrorHandling"/>.</param>
    /// <returns>The current <see cref="ErrorHandler"/> to support fluent-style method chaining.</returns>
    /// <remarks>Will be checked in the sequence added.</remarks>
    public ErrorHandler AddAssignableFrom<TException>(ErrorHandling errorHandling) where TException : Exception
    {
        _handlers.Add(new HandlerConfig { HandlingFactory = ex => typeof(TException).IsAssignableFrom(ex.GetType()) ? errorHandling : null });
        return this;
    }

    /// <summary>
    /// Adds the <paramref name="errorHandlingFactory"/> for the specified <see cref="Type.IsAssignableFrom(Type?)"/> <typeparamref name="TException"/> <see cref="Type"/>.
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/>.</typeparam>
    /// <param name="errorHandlingFactory">The <see cref="ErrorHandling"/> factory.</param>
    /// <returns>The current <see cref="ErrorHandler"/> to support fluent-style method chaining.</returns>
    /// <remarks>Where a <see langword="null"/> is returned from the factory this indicates that the exception has not been handled and the next configured handler will be checked.
    /// <para>Will be checked in the sequence added.</para></remarks>
    public ErrorHandler AddAssignableFrom<TException>(Func<TException, ErrorHandling?> errorHandlingFactory) where TException : Exception
    {
        _handlers.Add(new HandlerConfig { HandlingFactory = ex => ex is TException te && typeof(TException).IsAssignableFrom(ex.GetType()) ? errorHandlingFactory(te) : null });
        return this;
    }

    /// <summary>
    /// Handles the error based on the provided <see cref="ErrorHandlerArgs"/> and the configured error handling rules.
    /// </summary>
    /// <param name="args">The <see cref="ErrorHandlerArgs"/>.</param>
    /// <param name="defaultErrorHandling">The default <see cref="ErrorHandling"/> where the error is not configured.</param>
    /// <returns>The <see cref="Result"/> (will always be <see cref="Result.IsFailure"/>).</returns>
    internal Result Handle(ErrorHandlerArgs args, ErrorHandling? defaultErrorHandling)
    {
        // Determine the error handling to use.
        ErrorHandling? errorHandling = args.ErrorHandlingOverride;
        if (errorHandling is null)
        {
            errorHandling = ResolveErrorHandling(args.Exception);
            if (errorHandling is null)
            {
                if (defaultErrorHandling.HasValue)
                    errorHandling = defaultErrorHandling.Value;
                else
                    return args.Exception;
            }
        }

        // Action the configured error handling.
        args.SubscriberArgs.ResultingException = args.Exception;
        args.SubscriberArgs.ResultingErrorHandling = errorHandling;

        var logArgs = new object[] { args.Exception.Message, args.SourceType.Name, errorHandling.Value.ToString() };

        switch (errorHandling.Value)
        {
            case ErrorHandling.CompleteAsSilent:
                args.Logger.LogDebug(args.Exception, _logFormat, logArgs);
                return new EventSubscriberHandledException(ErrorHandling.CompleteAsSilent, null, args.Exception);

            case ErrorHandling.CompleteAsInformation:
                args.Logger.LogInformation(args.Exception, _logFormat, logArgs);
                return new EventSubscriberHandledException(ErrorHandling.CompleteAsInformation, null, args.Exception);

            case ErrorHandling.CompleteAsWarning:
                args.Logger.LogWarning(args.Exception, _logFormat, logArgs);
                return new EventSubscriberHandledException(ErrorHandling.CompleteAsWarning, null, args.Exception);

            case ErrorHandling.CompleteAsError:
                args.Logger.LogError(args.Exception, _logFormat, logArgs);
                return new EventSubscriberHandledException(ErrorHandling.CompleteAsError, null, args.Exception);

            case ErrorHandling.Retry:
                args.Logger.LogDebug(args.Exception, _logFormat, logArgs);
                return new EventSubscriberRetryException(null, args.Exception);

            case ErrorHandling.DeadLetter:
                args.Logger.LogDebug(args.Exception, _logFormat, logArgs);
                return new EventSubscriberDeadLetterException(null, args.Exception);

            case ErrorHandling.Catastrophic:
                args.Logger.LogCritical(args.Exception, _logFormat, logArgs);
                return new EventSubscriberCatastrophicException(null, args.Exception);

            case ErrorHandling.None:
            default:
                args.Logger.LogDebug(args.Exception, _logFormat, logArgs);
                return new EventSubscriberUnhandledException(null, args.Exception);
        }
    }

    /// <summary>
    /// Attempts to resolve the <see cref="ErrorHandling"/> for the specified <paramref name="exception"/> using the underlying configuration.
    /// </summary>
    private ErrorHandling? ResolveErrorHandling(Exception exception)
    {
        // Loop-de-loop through the configured handlers to determine the handling to use.
        foreach (var handler in _handlers)
        {
            var handling = handler.GetHandling(exception);
            if (handling.HasValue)
                return handling.Value;
        }

        // Check the extended exception configuration where applicable.
        if (exception is IExtendedException extendedException)
        {
            if (extendedException.IsTransient && AutoTransientHandling)
                return ErrorHandling.Retry;

            if (extendedException.IsError && WhereIsExtendedErrorHandling.HasValue)
                return WhereIsExtendedErrorHandling.Value;
        }

        // No configuration.
        return null;
    }

    /// <summary>
    /// Provides the per <see cref="Exception"/> <see cref="Type"/> <see cref="HandlingFactory"/> configuration for the <see cref="ErrorHandler"/>.
    /// </summary>
    private sealed class HandlerConfig()
    {
        public required Func<Exception, ErrorHandling?> HandlingFactory { get; init; }

        public ErrorHandling? GetHandling(Exception ex) => HandlingFactory(ex);
    }
} 