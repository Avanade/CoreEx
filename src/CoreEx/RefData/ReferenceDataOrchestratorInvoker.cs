// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Invokers;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the invocation wrapping for the <see cref="ReferenceDataOrchestrator"/> instances.
    /// </summary>
    public class ReferenceDataOrchestratorInvoker : InvokerBase<ReferenceDataOrchestrator>
    {
        private static ReferenceDataOrchestratorInvoker? _default;

        /// <summary>
        /// Gets the current configured instance (see <see cref="ExecutionContext.ServiceProvider"/>).
        /// </summary>
        public static ReferenceDataOrchestratorInvoker Current => CoreEx.ExecutionContext.GetService<ReferenceDataOrchestratorInvoker>() ?? (_default ??= new ReferenceDataOrchestratorInvoker());
    }
}