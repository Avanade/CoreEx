// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Azure.Storage.Blobs;
using Azure.Storage.Sas;
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
    /// <see href="https://learn.microsoft.com/en-us/azure/storage/blobs/storage-blobs-introduction">Azure Blob Storage</see> using a <see href="https://learn.microsoft.com/en-us/azure/storage/common/storage-sas-overview">SAS token</see>.
    /// </summary>
    public class BlobSasAttachmentStorage : IAttachmentStorage
    {
        private readonly BlobContainerClient? _blobContainerClient;

        /// <summary>
        /// Initializes a new instance of <see cref="BlobSasAttachmentStorage"/> class with a <paramref name="blobContainerClient"/>
        /// </summary>
        /// <param name="blobContainerClient">The <see cref="BlobContainerClient"/>.</param>
        public BlobSasAttachmentStorage(BlobContainerClient blobContainerClient) => _blobContainerClient = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));

        /// <summary>
        /// Initializes a new instance of <see cref="BlobSasAttachmentStorage"/> class.
        /// </summary>
        public BlobSasAttachmentStorage() { }

        /// <inheritdoc/>
        public int MaxDataSize { get; set; }

        /// <summary>
        /// Gets or sets the expiratation <see cref="TimeSpan"/> that is added to <see cref="DateTimeOffset.UtcNow"/> to create the SAS token.
        /// </summary>
        /// <remarks>Defaults to two (2) days.</remarks>
        public TimeSpan Expiration { get; set; } = TimeSpan.FromDays(2);

        /// <summary>
        /// Gets or sets the content type of the attachment. 
        /// </summary>
        /// <remarks>Defaults to <see cref="MediaTypeNames.Application.Json"/></remarks>
        public string ContentType { get; set; } = MediaTypeNames.Application.Json;

        /// <inheritdoc/>
        public async Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken)
        {
            var blobClient = new BlobClient(new Uri(attachment.Attachment!));
            var blobDownloadInfo = await blobClient.DownloadAsync(cancellationToken).ConfigureAwait(false);

            return BinaryData.FromStream(blobDownloadInfo.Value.Content);
        }

        /// <summary>
        /// Writes the attachment data to Azure Blob Storage and returns a SAS token to the blob
        /// </summary>
        /// <param name="event"></param>
        /// <param name="attachmentData"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Reference to Event Atttachment as <see cref="EventAttachment"/></returns>
        public async Task<EventAttachment> WriteAsync(EventData @event, BinaryData attachmentData, CancellationToken cancellationToken)
        {
            if(_blobContainerClient == null)
                throw new InvalidOperationException($"The {nameof(BlobContainerClient)} must be passed in the constructor in order to write.");

            var blobName = @event.Id ?? Guid.NewGuid().ToString();

            // Where @event.tenantId is set, prepend to create a tenant specific folder
            if(@event.TenantId != null)
                blobName = $"{@event.TenantId}/{blobName}";

            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(Expiration));
            await blobClient.UploadAsync(attachmentData.ToStream(), cancellationToken).ConfigureAwait(false);

            return new EventAttachment { Attachment = sasUri.ToString(), ContentType = ContentType };
        }
    }
}