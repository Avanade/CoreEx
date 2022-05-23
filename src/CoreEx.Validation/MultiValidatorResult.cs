// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the result of a <see cref="MultiValidator"/> <see cref="MultiValidator.RunAsync(bool, System.Threading.CancellationToken)"/>.
    /// </summary>
    public class MultiValidatorResult
    {
        private readonly MessageItemCollection _messages = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiValidatorResult"/> class.
        /// </summary>
        public MultiValidatorResult() => _messages.CollectionChanged += Messages_CollectionChanged;

        /// <summary>
        /// Indicates whether there has been a validation error.
        /// </summary>
        public bool HasErrors { get; private set; }

        /// <summary>
        /// Gets the errors <see cref="MessageItemCollection"/>.
        /// </summary>
        public MessageItemCollection Messages { get { return _messages; } }

        /// <summary>
        /// Handle the add of a message.
        /// </summary>
        private void Messages_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (HasErrors)
                return;

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var m in e.NewItems)
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

        /// <summary>
        /// Throws a <see cref="ValidationException"/> where an error was found.
        /// </summary>
        public void ThrowOnError()
        {
            if (HasErrors)
                throw new ValidationException(Messages);
        }
    }
}