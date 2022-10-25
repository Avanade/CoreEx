// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using System;
using System.Transactions;

namespace CoreEx.Invokers
{
    /// <summary>
    /// Provides arguments for the <see cref="InvokerBase"/> to manage the likes of <see cref="Transaction">transactions</see> and <see cref="EventPublisher">event sending.</see>.
    /// </summary>
    public class InvokerArgs
    {
        /// <summary>
        /// Gets or sets the <i>default</i> <see cref="InvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>false</c> and <see cref="OperationType"/> is <c>null</c>.
        /// </summary>
        public static InvokerArgs Default { get; set; } = new InvokerArgs();

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Read"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static InvokerArgs Read { get; } = new InvokerArgs { OperationType = CoreEx.OperationType.Read };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Create"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static InvokerArgs Create { get; } = new InvokerArgs { OperationType = CoreEx.OperationType.Create };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Update"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static InvokerArgs Update { get; } = new InvokerArgs { OperationType = CoreEx.OperationType.Update };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Delete"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static InvokerArgs Delete { get; } = new InvokerArgs { OperationType = CoreEx.OperationType.Delete };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Unspecified"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static InvokerArgs Unspecified { get; } = new InvokerArgs { OperationType = CoreEx.OperationType.Unspecified };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>true</c> and <see cref="TransactionScopeOption"/> is <see cref="TransactionScopeOption.Suppress"/>.
        /// </summary>
        public static InvokerArgs TransactionSuppress { get; } = new InvokerArgs { IncludeTransactionScope = true, TransactionScopeOption = TransactionScopeOption.Suppress };

        /// <summary>
        /// Gets the <see cref="InvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>true</c> and <see cref="TransactionScopeOption"/> is <see cref="TransactionScopeOption.RequiresNew"/>.
        /// </summary>
        public static InvokerArgs TransactionRequiresNew { get; } = new InvokerArgs { IncludeTransactionScope = true, TransactionScopeOption = TransactionScopeOption.RequiresNew };

        /// <summary>
        /// Indicates whether to wrap the invocation with a <see cref="TransactionScope"/> (see <see cref="TransactionScopeOption"/>). Defaults to <c>false</c>.
        /// </summary>
        public bool IncludeTransactionScope { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="System.Transactions.TransactionScopeOption"/> (see <see cref="IncludeTransactionScope"/>). Defaults to <see cref="TransactionScopeOption.Required"/>.
        /// </summary>
        public TransactionScopeOption TransactionScopeOption { get; set; } = TransactionScopeOption.Required;

        /// <summary>
        /// Gets or sets the <see cref="IEventPublisher"/> to automatically perform an <see cref="IEventPublisher.SendAsync(System.Threading.CancellationToken)"/> on success.
        /// </summary>
        /// <remarks>Where set will automatically perform an <see cref="IEventPublisher.SendAsync(System.Threading.CancellationToken)"/>. This will be initiated before the <see cref="IncludeTransactionScope">corresponding</see> <see cref="Transaction"/>
        /// is <see cref="TransactionScope.Complete">completed</see>; to ensure success before the final commit, otherwise a transaction rollback/cancel will occur.</remarks>
        public IEventPublisher? EventPublisher { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CoreEx.OperationType"/> to override the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.OperationType"/>.
        /// </summary>
        /// <remarks>Note that this is not thread-safe in the sense that where set across multiple concurrent tasks the order in which they execute will update the shared <see cref="ExecutionContext.OperationType"/>. It is recommended that 
        /// this is set at the top of the call stack before any further concurrent tasks are performed.</remarks>
        public CoreEx.OperationType? OperationType { get; set; }

        /// <summary>
        /// Gets or sets the unhandled <see cref="Exception"/> handler.
        /// </summary>
        public Action<Exception>? ExceptionHandler { get; set; }
    }
}