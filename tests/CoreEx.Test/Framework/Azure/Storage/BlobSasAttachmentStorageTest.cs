using Azure.Storage.Blobs;
using CoreEx.Azure.Storage;
using CoreEx.Events;
using NUnit.Framework;
using System;
using System.Threading;

namespace CoreEx.Test.Framework.Azure.Storage
{
    [TestFixture]
    public class BlobSasAttachmentStorageTest
    {
        private BlobContainerClient bcc;

        [SetUp]
        public void Init()
        {
            var containerName = "testevents";
            var csn = $"{nameof(BlobAttachmentStorage)}ConnectionString";
            var cs = Environment.GetEnvironmentVariable(csn);
            if (cs is null)
                Assert.Inconclusive($"Test cannot run as the environment variable '{csn}' is not defined.");

            bcc = new BlobContainerClient(cs, containerName);
            bcc.CreateIfNotExists();
        }

        [Test]
        public void EventAttachment_SuccessfulSendToStorage()
        {

            var testEvent = new EventData { Id = Guid.NewGuid().ToString() };
            var eventData = "{ \"id\": \"1234\" }";
            var attachmentData = new BinaryData(eventData);
            var bas = new BlobSasAttachmentStorage(bcc);

            var result = bas.WriteAsync(testEvent, attachmentData, CancellationToken.None).Result;

            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Attachment);
            Assert.IsTrue(result.Attachment.Contains(testEvent.Id));

            var eventReceivedData = bas.ReadAync(result, CancellationToken.None).Result;

            Assert.IsNotNull(eventReceivedData);
            Assert.AreEqual(eventData, eventReceivedData.ToString());

            bcc.DeleteBlob(testEvent.Id);
        }

        [TearDown]
        public void Cleanup()
        {
            // Cleanup
            bcc.Delete();
        }
    }
}