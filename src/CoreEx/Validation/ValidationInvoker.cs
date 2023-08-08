// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the invocation wrapping for the <see cref="IValidator"/> instances.
    /// </summary>
    public class ValidationInvoker : InvokerBase<object>
    {
        private const string ValidationHasErrors = "validation.haserrors";
        private const string ValidationFailureResult = "validation.failureresult";
        private static ValidationInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static ValidationInvoker Current => CoreEx.ExecutionContext.GetService<ValidationInvoker>() ?? (_default ??= new ValidationInvoker());

        /// <inheritdoc/>
        protected override TResult OnInvoke<TResult>(InvokeArgs invokeArgs, object invoker, System.Func<InvokeArgs, TResult> func) => throw new NotImplementedException();

        /// <inheritdoc/>
        protected override Task<TResult> OnInvokeAsync<TResult>(InvokeArgs invokeArgs, object invoker, Func<InvokeArgs, CancellationToken, Task<TResult>> func, CancellationToken cancellationToken)
        {
            var result = base.OnInvokeAsync(invokeArgs, invoker, func, cancellationToken);
            if (invokeArgs.Activity is not null && result is IValidationResult vr)
            {
                invokeArgs.Activity.AddTag(ValidationHasErrors, vr.HasErrors);
                invokeArgs.Activity.AddTag(ValidationFailureResult, vr.FailureResult is null || vr.FailureResult.Value.IsSuccess ? null : vr.FailureResult.ToString());
            }

            return result;
        }
    }
}