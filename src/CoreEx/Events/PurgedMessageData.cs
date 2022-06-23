// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Events
{
    /// <summary>
    /// Represents a messaging-system agnostic dead lettered message.
    /// </summary>
    public class PurgedMessageData
    {
        /// <summary> creates a <see cref="PurgedMessageData"/> </summary>
        public PurgedMessageData(string messageId, string subject, string correlationId,
        string deadLetterReason, string deadLetterErrorDescription, string deadLetterSource, string body)
        {
            MessageId = messageId;
            Subject = subject;
            CorrelationId = correlationId;
            DeadLetterReason = deadLetterReason;
            DeadLetterErrorDescription = deadLetterErrorDescription;
            DeadLetterSource = deadLetterSource;
            Body = body;
        }
        /// <summary>
        /// Gets the unique message identifier.
        /// </summary>
        public string MessageId { get; private set; }
        /// <summary>
        /// Gets the messages body.
        /// </summary>
        public string? Body { get; private set; }
        /// <summary>
        /// Gets the messages Subject.
        /// </summary>
        public string? Subject { get; private set; }
        /// <summary>
        /// Gets the messages Correlation Id.
        /// </summary>
        public string CorrelationId { get; private set; }
        /// <summary>
        /// Gets the messages Dead Letter Reason.
        /// </summary>
        public string DeadLetterReason { get; private set; }
        /// <summary>
        /// Gets the messages Dead Letter Error Description.
        /// </summary>
        public string DeadLetterErrorDescription { get; private set; }
        /// <summary>
        /// Gets the messages Dead Letter Source.
        /// </summary>
        public string DeadLetterSource { get; private set; }
    }
}