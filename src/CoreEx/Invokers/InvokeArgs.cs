// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Represents the runtime arguments for an <see cref="InvokerBase{TInvoker}"/> or <see cref="InvokerBase{TInvoker, TArgs}"/> invocation.
    /// </summary>
    public struct InvokeArgs
    {
        /// <summary>
        /// Gets or sets the <see cref="System.Diagnostics.Activity"/> leveraged for standardized (open-telemetry) tracing.
        /// </summary>
        public Activity? Activity { get; set; }

        /// <summary>
        /// Get or sets the calling member name (see <see cref="CallerMemberNameAttribute"/>).
        /// </summary>
        public string? MemberName { get; set; }
    }
}