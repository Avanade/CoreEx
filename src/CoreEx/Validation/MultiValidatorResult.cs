// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Results;
using System;
using System.Linq;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the result of a <see cref="MultiValidator"/> <see cref="MultiValidator.ValidateAsync(System.Threading.CancellationToken)"/>.
    /// </summary>
    public class MultiValidatorResult : IValidationResult, IToResult
    {
        private MessageItemCollection? _messages;

        /// <inheritdoc/>
        /// <remarks>This is nonsensical and as such will throw a <see cref="NotSupportedException"/>.</remarks>
        object? IValidationResult.Value => throw new NotSupportedException();
        
        /// <inheritdoc/>
        public bool HasErrors { get; private set; }

        /// <inheritdoc/>
        public MessageItemCollection Messages
        {
            get
            {
                if (_messages != null)
                    return _messages;

                _messages = new();
                _messages.CollectionChanged += Messages_CollectionChanged;
                return _messages;
            }
        }

        /// <summary>
        /// Handle the add of a message.
        /// </summary>
        private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (HasErrors)
                return;

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var m in e.NewItems!)
                    {
                        MessageItem mi = (MessageItem)m;
                        if (mi.Type == MessageType.Error)
                        {
                            HasErrors = true;
                            return;
                        }
                    }

                    break;

                default:
                    throw new InvalidOperationException("Operation invalid for Messages; only add supported.");
            }
        }

        /// <inheritdoc/>
        public Result? FailureResult { get; private set; }

        /// <summary>
        /// Sets the <see cref="FailureResult"/>.
        /// </summary>
        /// <param name="result">The <see cref="Result"/>.</param>
        internal void SetFailureResult(Result? result)
        {
            if (result is null)
                return;

            if (FailureResult.HasValue)
                throw new InvalidOperationException("The ValidationContext is already in a Failure state.");

            if (result.Value.IsFailure)
                FailureResult = result;
        }

        /// <inheritdoc/>
        public Exception? ToException() => FailureResult.HasValue ? FailureResult.Value.Error : (HasErrors ? new ValidationException(Messages!) : null);

        /// <inheritdoc/>
        IValidationResult IValidationResult.ThrowOnError() => ThrowOnError();

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where an error was found (and optionally if warnings).
        /// </summary>
        /// <param name="includeWarnings">Indicates whether to throw where only warnings exist.</param>
        /// <returns>The <see cref="MultiValidatorResult"/> to support fluent-style method-chaining.</returns>
        public MultiValidatorResult ThrowOnError(bool includeWarnings = false)
        {
            var ex = ToException();
            if (ex is not null)
                throw ex;

            if (includeWarnings && Messages != null && Messages.Any(x => x.Type == MessageType.Warning))
                throw new ValidationException(Messages);

            return this;
        }

        /// <inheritdoc/>
        /// <remarks>This is largely nonsensical from a <typeparamref name="T"/> perspective and as such the <see cref="IResult{T}.Value"/> will be set to its default value (see <see cref="Result{T}.None"/>).</remarks>
        Result<T> ITypedToResult.ToResult<T>() => FailureResult.HasValue ? FailureResult.Value.Bind<T>() : (HasErrors ? Result<T>.ValidationError(Messages!) : Result<T>.None);

        /// <inheritdoc/>
        public Result ToResult() => FailureResult ?? (HasErrors ? Result.ValidationError(Messages!) : Result.Success);
    }
}