namespace CoreEx.AspNetCore.Idempotency;

/// <summary>
/// Provides the <see cref="IIdempotencyProvider"/> invoker.
/// </summary>
[InvokerName("CoreEx.AspNetCore.Idempotency.IdempotencyProviderInvoker")]
public class IdempotencyProviderInvoker : InvokerBase<IIdempotencyProvider>
{
    private static IdempotencyProviderInvoker? _default;

    /// <summary>
    /// Gets the default <see cref="IdempotencyProviderInvoker"/> instance.
    /// </summary>
    public static IdempotencyProviderInvoker Default => ExecutionContext.GetService<IdempotencyProviderInvoker>() ?? (_default ??= new IdempotencyProviderInvoker());
}