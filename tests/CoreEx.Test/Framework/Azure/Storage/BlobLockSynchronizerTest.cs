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

            // Acquire lease.
            Assert.IsTrue(bls.Enter<int>());

            // Try immediately.
            Assert.IsFalse(bls2.Enter<int>());

            // Try within the initial lease time.
            Thread.Sleep(12000);
            Assert.IsFalse(bls2.Enter<int>());

            // Try and should have been renewed.
            Thread.Sleep(12000);
            Assert.IsFalse(bls2.Enter<int>());

            // Release it.
            bls.Exit<int>();

            // And final quick go around again.
            Assert.IsTrue(bls.Enter<int>());
            Assert.IsFalse(bls2.Enter<int>());
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