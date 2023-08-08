// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Provides the invocation wrapping for the service instances.
    /// </summary>
    public class ServiceInvoker : InvokerBase<object>
    {
        private static ServiceInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static ServiceInvoker Current => CoreEx.ExecutionContext.GetService<ServiceInvoker>() ?? (_default ??= new ServiceInvoker());
    }
}