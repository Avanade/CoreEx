// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the invocation wrapping for the <see cref="IValidator"/> instances.
    /// </summary>
    public class ValidationInvoker : InvokerBase<object>
    {
        private static ValidationInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static ValidationInvoker Current => CoreEx.ExecutionContext.GetService<ValidationInvoker>() ?? (_default ??= new ValidationInvoker());
    }
}