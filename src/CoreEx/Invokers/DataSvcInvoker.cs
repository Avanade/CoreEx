// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Invokers
{
    /// <summary>
    /// Wraps a <b>Data Service invoke</b> enabling standard <b>business tier</b> functionality to be added to all invocations.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class DataSvcInvoker : InvokerBase
    {
        private static DataSvcInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static DataSvcInvoker Current => CoreEx.ExecutionContext.GetService<DataSvcInvoker>() ?? (_default ??= new DataSvcInvoker());
    }
}