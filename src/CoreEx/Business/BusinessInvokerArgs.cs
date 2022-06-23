// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using System;
using System.Transactions;

namespace CoreEx.Business
{
    /// <summary>
    /// Provides arguments for the <see cref="BusinessInvokerBase"/>.
    /// </summary>
    public class BusinessInvokerArgs
    {
        /// <summary>
        /// Gets or sets the <i>default</i> <see cref="BusinessInvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>false</c> and <see cref="OperationType"/> is <c>null</c>.
        /// </summary>
        public static BusinessInvokerArgs Default { get; set; } = new BusinessInvokerArgs();

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Read"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static BusinessInvokerArgs Read { get; } = new BusinessInvokerArgs { OperationType = CoreEx.OperationType.Read };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Create"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static BusinessInvokerArgs Create { get; } = new BusinessInvokerArgs { OperationType = CoreEx.OperationType.Create };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Update"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static BusinessInvokerArgs Update { get; } = new BusinessInvokerArgs { OperationType = CoreEx.OperationType.Update };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Delete"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static BusinessInvokerArgs Delete { get; } = new BusinessInvokerArgs { OperationType = CoreEx.OperationType.Delete };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="OperationType"/> is <see cref="CoreEx.OperationType.Unspecified"/> and <see cref="IncludeTransactionScope"/> is <c>false</c>.
        /// </summary>
        public static BusinessInvokerArgs Unspecified { get; } = new BusinessInvokerArgs { OperationType = CoreEx.OperationType.Unspecified };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>true</c> and <see cref="TransactionScopeOption"/> is <see cref="TransactionScopeOption.Suppress"/>.
        /// </summary>
        public static BusinessInvokerArgs TransactionSuppress { get; } = new BusinessInvokerArgs { IncludeTransactionScope = true, TransactionScopeOption = TransactionScopeOption.Suppress };

        /// <summary>
        /// Gets the <see cref="BusinessInvokerArgs"/> where <see cref="IncludeTransactionScope"/> is <c>true</c> and <see cref="TransactionScopeOption"/> is <see cref="TransactionScopeOption.RequiresNew"/>.
        /// </summary>
        public static BusinessInvokerArgs TransactionRequiresNew { get; } = new BusinessInvokerArgs { IncludeTransactionScope = true, TransactionScopeOption = TransactionScopeOption.RequiresNew };

        /// <summary>
        /// Indicates whether to wrap the invocation with a <see cref="TransactionScope"/> (see <see cref="TransactionScopeOption"/>). Defaults to <c>false</c>.
        /// </summary>
        public bool IncludeTransactionScope { get; set; } = false;

        /// <summary>
        /// Gets or sets the <see cref="System.Transactions.TransactionScopeOption"/> (see <see cref="IncludeTransactionScope"/>). Defaults to <see cref="TransactionScopeOption.Required"/>.
        /// </summary>
        public TransactionScopeOption TransactionScopeOption { get; set; } = TransactionScopeOption.Required;

        /// <summary>
        /// Indicates whether to automatically perform an <see cref="IEventPublisher.SendAsync(System.Threading.CancellationToken)"/> where there is an <see cref="IEventPublisher"/> instance.
        /// </summary>
        /// <remarks>The will be initiated before the corresponding <see cref="IncludeTransactionScope"/> is <see cref="TransactionScope.Complete"/> to be included in any commit/rollback.</remarks>
        public IEventPublisher? EventPublisher { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CoreEx.OperationType"/> to override the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext.OperationType"/>.
        /// </summary>
        public CoreEx.OperationType? OperationType { get; set; }

        /// <summary>
        /// Gets or sets the unhandled <see cref="Exception"/> handler.
        /// </summary>
        public Action<Exception>? ExceptionHandler { get; set; }
    }
}
