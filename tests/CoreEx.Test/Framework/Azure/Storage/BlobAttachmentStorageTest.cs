using Azure.Storage.Blobs;
using CoreEx.Azure.Storage;
using CoreEx.Events;
using NUnit.Framework;
using System;
using System.Threading;

namespace CoreEx.Test.Framework.Azure.Storage
{
    [TestFixture]
    public class BlobAttachmentStorageTest
    {
        private BlobContainerClient? _bcc;

        [OneTimeSetUp]
        public void Init()
        {
            var containerName = "testevents";
            var csn = $"{nameof(BlobAttachmentStorage)}ConnectionString";
            var cs = Environment.GetEnvironmentVariable(csn);
            if (cs is null)
                Assert.Inconclusive($"Test cannot run as the environment variable '{csn}' is not defined.");

            _bcc = new BlobContainerClient(cs, containerName);
            _bcc.CreateIfNotExists();
        }

        [Test]
        public void BlobAttachmentStorage_SuccessfulSendToStorage()
        {
            var testEvent = new EventData { Id = Guid.NewGuid().ToString() };
            var eventData = "{ \"data\": \"1234\" }";
            var attachmentData = new BinaryData(eventData);
            var bas = new BlobAttachmentStorage(_bcc!);

            var result = bas.WriteAsync(testEvent, attachmentData, CancellationToken.None).Result;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Attachment, Is.Not.Null);
            Assert.That(result.Attachment!.Contains(testEvent.Id), Is.True);

            var eventReceivedData = bas.ReadAync(result, CancellationToken.None).Result;

            Assert.That(eventReceivedData, Is.Not.Null);
            Assert.That(eventReceivedData.ToString(), Is.EqualTo(eventData));

            _bcc!.DeleteBlob($"{testEvent.Id}.json");
        }

        [Test]
        public void BlobSasAttachmentStorage_SuccessfulSendToStorage()
        {
            var testEvent = new EventData { Id = Guid.NewGuid().ToString() };
            var eventData = "{ \"id\": \"1234\" }";
            var attachmentData = new BinaryData(eventData);
            var bas = new BlobSasAttachmentStorage(_bcc!);

            var result = bas.WriteAsync(testEvent, attachmentData, CancellationToken.None).Result;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Attachment, Is.Not.Null);
            Assert.That(result.Attachment!.Contains(testEvent.Id), Is.True);

            var eventReceivedData = bas.ReadAync(result, CancellationToken.None).Result;

            Assert.That(eventReceivedData, Is.Not.Null);
            Assert.That(eventReceivedData.ToString(), Is.EqualTo(eventData));

            _bcc!.DeleteBlob($"{testEvent.Id}.json");
        }

        [Test]
        public void BlobAttachmentStorage_WithTenantId_SetsProperContainer()
        {
            var testTenantId = "112233";
            var testEvent = new EventData { Id = Guid.NewGuid().ToString(), TenantId = testTenantId };
            var eventData = "{ \"id\": \"1234\" }";
            var attachmentData = new BinaryData(eventData);
            var bas = new BlobAttachmentStorage(_bcc!);

            var result = bas.WriteAsync(testEvent, attachmentData, CancellationToken.None).Result;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Attachment, Is.Not.Null);
            Assert.That(result.Attachment!.Contains($"{testTenantId}/{testEvent.Id}"), Is.True);
        }

        [Test]
        public void BlobSasAttachmentStorage_WithTenantId_SetsProperContainer()
        {
            var testTenantId = "112233";
            var testEvent = new EventData { Id = Guid.NewGuid().ToString(), TenantId = testTenantId };
            var eventData = "{ \"id\": \"1234\" }";
            var attachmentData = new BinaryData(eventData);
            var bas = new BlobSasAttachmentStorage(_bcc!);

            var result = bas.WriteAsync(testEvent, attachmentData, CancellationToken.None).Result;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Attachment, Is.Not.Null);
            Assert.That(result.Attachment!.Contains($"{testTenantId}/{testEvent.Id}"), Is.True);
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            // Cleanup
            _bcc?.Delete();
        }
    }
}