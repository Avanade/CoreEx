// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides the invocation wrapping for the <see cref="TypedHttpClientBase"/> instances.
    /// </summary>
    public class TypedHttpClientInvoker : InvokerBase<object>
    {
        private static TypedHttpClientInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static TypedHttpClientInvoker Current => CoreEx.ExecutionContext.GetService<TypedHttpClientInvoker>() ?? (_default ??= new TypedHttpClientInvoker());
    }
}