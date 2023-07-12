using Azure.Storage.Blobs;
using CoreEx.Events;
using CoreEx.Events.Attachments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.Storage
{
    /// <summary>
    /// This class is used to store event payloads as attachments in Azure Blob Storage
    /// </summary>
    public class BlobAttachmentStorage : IAttachmentStorage
    {
        private readonly BlobContainerClient _blobContainerClient;

        /// <summary>
        /// Creates a new instance of <see cref="BlobAttachmentStorage"/>
        /// </summary>
        /// <param name="blobContainerClient"></param>
        public BlobAttachmentStorage(BlobContainerClient blobContainerClient)
        {
            _blobContainerClient = blobContainerClient;
        }

        /// <summary>
        /// The maximum size of the attachment data in bytes
        /// </summary>
        public int MaxDataSize { get; set; }

        /// <summary>
        /// The content type of the attachment
        /// Defaults to application/json
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Reads the attachment data from Azure Blob Storage and returns the data
        /// </summary>
        /// <param name="attachment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Attachment data as <see cref="BinaryData"/></returns>
        /// <exception cref="NotImplementedException"></exception>
        public Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken)
        {
            var blobClient = _blobContainerClient.GetBlobClient(attachment.Attachment);
            var blobDownloadInfo = blobClient.Download(cancellationToken);
            
            return Task.FromResult(BinaryData.FromStream(blobDownloadInfo.Value.Content));
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
            var blobName = @event.Id ?? Guid.NewGuid().ToString();

            // if @event.tenantId is set, prepend to create a tenant specific folder
            if (@event.TenantId != null)
            {
                blobName = $"{@event.TenantId}/{blobName}";
            }
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            await blobClient.UploadAsync(attachmentData.ToStream(), cancellationToken);

            return new EventAttachment { Attachment = blobName, ContentType = ContentType };
        }
    }
}
