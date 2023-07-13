// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Storage.Blobs;
using CoreEx.Events;
using CoreEx.Events.Attachments;
using System;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.Storage
{
    /// <summary>
    /// Provides the reading and writing of a <see cref="EventData.Value"/> attachment that exceeds the <see cref="MaxDataSize"/> as identified by a corresponding <see cref="EventAttachment"/> within 
    /// <see href="https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction">Azure Blob Storage</see>.
    /// </summary>
    public class BlobAttachmentStorage : IAttachmentStorage
    {
        private readonly BlobContainerClient _blobContainerClient;

        /// <summary>
        /// Initializes a new instance of <see cref="BlobAttachmentStorage"/> class.
        /// </summary>
        /// <param name="blobContainerClient">The <see cref="BlobContainerClient"/>.</param>
        public BlobAttachmentStorage(BlobContainerClient blobContainerClient) => _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

        /// <inheritdoc/>
        public int MaxDataSize { get; set; }

        /// <summary>
        /// Gets or sets the content type of the attachment. 
        /// </summary>
        /// <remarks>Defaults to <see cref="MediaTypeNames.Application.Json"/></remarks>
        public string ContentType { get; set; } = MediaTypeNames.Application.Json;

        /// <inheritdoc/>
        public async Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken)
        {
            var blobClient = _blobContainerClient.GetBlobClient(attachment.Attachment);
            var blobDownloadInfo = await blobClient.DownloadAsync(cancellationToken).ConfigureAwait(false);

            return BinaryData.FromStream(blobDownloadInfo.Value.Content);
        }

        /// <inheritdoc/>
        public async Task<EventAttachment> WriteAsync(EventData @event, BinaryData attachmentData, CancellationToken cancellationToken)
        {
            var blobName = @event.Id ?? Guid.NewGuid().ToString();

            // Where @event.tenantId is set, prepend to create a tenant specific folder.
            if (@event.TenantId != null)
                blobName = $"{@event.TenantId}/{blobName}";

            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(attachmentData.ToStream(), cancellationToken).ConfigureAwait(false);

            return new EventAttachment { Attachment = blobName, ContentType = ContentType };
        }
    }
}
