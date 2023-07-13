using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using CoreEx.Events;
using CoreEx.Events.Attachments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Azure.Storage
{
    /// <summary>
    /// This class is used to store event payloads as attachments in Azure Blob Storage using a SAS token
    /// </summary>
    public class BlobSasAttachmentStorage : IAttachmentStorage
    {
        private readonly BlobContainerClient? _blobContainerClient;

        /// <summary>
        /// Creates a new instance of <see cref="BlobSasAttachmentStorage"/>
        /// </summary>
        /// <param name="blobContainerClient"></param>
        public BlobSasAttachmentStorage(BlobContainerClient blobContainerClient)
        {
            _blobContainerClient = blobContainerClient;
        }

        /// <summary>
        /// Default constructor for <see cref="BlobSasAttachmentStorage"/>
        /// Used for ReadAsync as SAS token is provided rather than storage account
        /// </summary>
        public BlobSasAttachmentStorage() { }

        /// <summary>
        /// The maximum size of the attachment data in bytes
        /// </summary>
        public int MaxDataSize { get; set; }

        /// <summary>
        /// Number of days the SAS token is valid for
        /// Defaults to 2 days
        /// </summary>
        public int SasExpirationInDays { get; set; } = 2;

        /// <summary>
        /// The content type of the attachment
        /// Defaults to application/json
        /// </summary>
        public string ContentType { get; set; } = "application/json";

        /// <summary>
        /// Reads the attachment data from Azure Blob Storage using the SAS token
        /// </summary>
        /// <param name="attachment"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Attachment data as <see cref="BinaryData"/></returns>
        public Task<BinaryData> ReadAync(EventAttachment attachment, CancellationToken cancellationToken)
        {
            var sasToken = attachment.Attachment;
            // get the blob client from the sas token
            var blobClient = new BlobClient(new Uri(sasToken));
            // download the blob
            var blobDownloadInfo = blobClient.Download(cancellationToken);
            // return the blob data
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
            if(_blobContainerClient == null)
            {
                throw new ArgumentNullException(nameof(_blobContainerClient), "BlobContainerClient must be initialized in order to write");
            }

            var blobName = @event.Id ?? Guid.NewGuid().ToString();

            // if @event.tenantId is set, prepend to create a tenant specific folder
            if(@event.TenantId != null)
            {
                blobName = $"{@event.TenantId}/{blobName}";
            }
            var blobClient = _blobContainerClient.GetBlobClient(blobName);
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddDays(SasExpirationInDays));
            await blobClient.UploadAsync(attachmentData.ToStream(), cancellationToken);

            return new EventAttachment { Attachment = sasUri.ToString(), ContentType = ContentType };
        }
    }
}
