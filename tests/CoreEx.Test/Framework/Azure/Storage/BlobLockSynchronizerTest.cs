using Azure.Storage.Blobs;
using CoreEx.Azure.Storage;
using NUnit.Framework;
using System;
using System.Threading;

namespace CoreEx.Test.Framework.Azure.Storage
{
    [TestFixture]
    public class BlobLockSynchronizerTest
    {
        /// <summary>
        /// If this test fails check in the Azure Portal to make sure the lease is available, or file does not exist.
        /// </summary>
        [Test]
        public void EnterAndExit_AutoRenew()
        {
            var csn = $"{nameof(BlobLeaseSynchronizer)}ConnectionString";
            var cs = Environment.GetEnvironmentVariable(csn);
            if (cs is null)
                Assert.Inconclusive($"Test cannot run as the environment variable '{csn}' is not defined.");

            var bcc = new BlobContainerClient(cs, "event-synchronizer");
            var bls = new TestBlobLockSynchronizer(bcc);
            var bls2 = new TestBlobLockSynchronizer(bcc);

            Assert.Multiple(() =>
            {
                // Acquire lease.
                Assert.That(bls.Enter<int>(), Is.True);

                // Try immediately.
                Assert.That(bls2.Enter<int>(), Is.False);
            });

            // Try within the initial lease time.
            Thread.Sleep(12000);
            Assert.That(bls2.Enter<int>(), Is.False);

            // Try and should have been renewed.
            Thread.Sleep(12000);
            Assert.That(bls2.Enter<int>(), Is.False);

            // Release it.
            bls.Exit<int>();

            Assert.Multiple(() =>
            {
                // And final quick go around again.
                Assert.That(bls.Enter<int>(), Is.True);
                Assert.That(bls2.Enter<int>(), Is.False);
            });
            bls.Exit<int>();
        }
    }

    public class TestBlobLockSynchronizer : BlobLeaseSynchronizer
    {
        public TestBlobLockSynchronizer(BlobContainerClient client) : base(client) { }

        public override TimeSpan LeaseDuration => TimeSpan.FromSeconds(20);

        public override TimeSpan AutoRenewLeaseDuration => TimeSpan.FromSeconds(15);
    }
}