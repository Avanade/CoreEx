// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Events.Attachments
{
    /// <summary>
    /// Enables the reading and writing of a <see cref="EventData.Value"/> attachment that exceeds the <see cref="MaxDataSize"/>.
    /// </summary>
    /// <remarks>This is the enabling interface to support the <see href="https://learn.microsoft.com/en-us/azure/architecture/patterns/claim-check">Claim-Check pattern</see>. This should be used
    /// by the <see cref="IEventSerializer"/> to perform in a underlying messaging sub-system agnostic manner.</remarks>
    public interface IAttachmentStorage
    {
        /// <summary>
        /// Gets or sets the maximum size of the <see cref="EventSendData.Data"/> to become an attachment.
        /// </summary>
        int MaxDataSize { get; set; }

        /// <summary>
        /// Writes the <paramref name="attachmentData"/> to the underlying storage and returns the <see cref="EventAttachment"/> details.
        /// </summary>
        /// <param name="event">The initiating <see cref="EventData"/>.</param>
        /// <param name="attachmentData">The attachment contents serialized as <see cref="BinaryData"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="EventAttachment"/> details.</returns>
        Task<EventAttachment> WriteAsync(EventData @event, BinaryData attachmentData, CancellationToken cancellationToken);

        /// <summary>
        /// Reads the <paramref name="attachment"/> from the underlying storage and returns the contents as <see cref="BinaryData"/>.
        /// </summary>
        /// <param name="attachment">The <see cref="EventAttachment"/> details.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The attachment contents as <see cref="BinaryData"/>.</returns>
        Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken);
    }
}