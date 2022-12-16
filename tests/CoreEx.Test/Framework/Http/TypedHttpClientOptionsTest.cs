using CoreEx.Configuration;
using CoreEx.Http;
using CoreEx.Http.Extended;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class TypedHttpClientOptionsTest
    {
        [Test]
        public void Initialize()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            AssertIsInitial(o);
        }

        private void AssertIsInitial(TypedHttpClientOptions o)
        {
            Assert.IsNotNull(o);
            Assert.IsNull(o.CustomRetryPolicy);
            Assert.IsNull(o.RetryCount);
            Assert.IsNull(o.RetrySeconds);
            Assert.IsFalse(o.ShouldThrowTransientException);
            Assert.IsFalse(o.ShouldThrowKnownException);
            Assert.IsFalse(o.ShouldThrowKnownUseContentAsMessage);
            Assert.IsNotNull(o.IsTransientPredicate);
            Assert.IsFalse(o.ShouldEnsureSuccess);
            Assert.IsNull(o.ExpectedStatusCodes);
            Assert.IsNull(o.MaxRetryDelay);
            Assert.IsNull(o.BeforeRequest);
        }

        [Test]
        public void WithRetry()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithRetry();
            Assert.AreEqual(3, o.RetryCount);
            Assert.AreEqual(1.8d, o.RetrySeconds);

            o.WithRetry(5);
            Assert.AreEqual(5, o.RetryCount);
            Assert.AreEqual(1.8d, o.RetrySeconds);

            o.WithRetry(null, 2.7d);
            Assert.AreEqual(3, o.RetryCount);
            Assert.AreEqual(2.7, o.RetrySeconds);

            o.WithRetry(2, 1.5d);
            Assert.AreEqual(2, o.RetryCount);
            Assert.AreEqual(1.5, o.RetrySeconds);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.AreEqual(2, o2.RetryCount);
            Assert.AreEqual(1.5, o2.RetrySeconds);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void ThrowTransientException()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.ThrowTransientException();
            Assert.IsTrue(o.ShouldThrowTransientException);
            Assert.IsNotNull(o.IsTransientPredicate);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.IsTrue(o2.ShouldThrowTransientException);
            Assert.IsNotNull(o2.IsTransientPredicate);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void ThrowKnownException()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.ThrowKnownException();
            Assert.IsTrue(o.ShouldThrowKnownException);
            Assert.IsFalse(o.ShouldThrowKnownUseContentAsMessage);

            o.ThrowKnownException(true);
            Assert.IsTrue(o.ShouldThrowKnownException);
            Assert.IsTrue(o.ShouldThrowKnownUseContentAsMessage);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.IsTrue(o2.ShouldThrowKnownException);
            Assert.IsTrue(o2.ShouldThrowKnownUseContentAsMessage);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void EnsureSuccess()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.EnsureSuccess();
            Assert.IsTrue(o.ShouldEnsureSuccess);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.IsTrue(o2.ShouldEnsureSuccess);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void Ensure()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.EnsureOK();
            o.EnsureAccepted();
            o.EnsureCreated();
            o.EnsureNoContent();
            Assert.AreEqual(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent }, o.ExpectedStatusCodes.ToArray());

            o.Ensure(HttpStatusCode.Continue, HttpStatusCode.Conflict);
            Assert.AreEqual(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }, o.ExpectedStatusCodes.ToArray());

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.AreEqual(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }, o2.ExpectedStatusCodes.ToArray());

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void WithTimeout()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithTimeout(System.TimeSpan.FromMinutes(1));
            Assert.AreEqual(System.TimeSpan.FromMinutes(1), o.Timeout);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.AreEqual(System.TimeSpan.FromMinutes(1), o2.Timeout);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void WithMaxRetryDelay()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithMaxRetryDelay(System.TimeSpan.FromMinutes(2));
            Assert.AreEqual(System.TimeSpan.FromMinutes(2), o.MaxRetryDelay);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.AreEqual(System.TimeSpan.FromMinutes(2), o2.MaxRetryDelay);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void OnBeforeRequest()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.OnBeforeRequest((r, ct) => Task.CompletedTask);
            Assert.NotNull(o.BeforeRequest);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.NotNull(o2.BeforeRequest);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void TypedHttpClient_Default_And_SendOptions()
        {
            var thc = new TypedHttpClient(new System.Net.Http.HttpClient());
            AssertIsInitial(thc.DefaultOptions);
            AssertIsInitial(thc.SendOptions);
            thc.Reset();

            thc.DefaultOptions.WithRetry(2, 3);
            Assert.AreEqual(2, thc.DefaultOptions.RetryCount);
            Assert.AreEqual(3d, thc.DefaultOptions.RetrySeconds);
            Assert.AreEqual(2, thc.SendOptions.RetryCount);
            Assert.AreEqual(3d, thc.SendOptions.RetrySeconds);

            thc.WithRetry(5, 7);
            Assert.AreEqual(2, thc.DefaultOptions.RetryCount);
            Assert.AreEqual(3d, thc.DefaultOptions.RetrySeconds);
            Assert.AreEqual(5, thc.SendOptions.RetryCount);
            Assert.AreEqual(7d, thc.SendOptions.RetrySeconds);

            thc.Reset();
            Assert.AreEqual(2, thc.DefaultOptions.RetryCount);
            Assert.AreEqual(3d, thc.DefaultOptions.RetrySeconds);
            Assert.AreEqual(2, thc.SendOptions.RetryCount);
            Assert.AreEqual(3d, thc.SendOptions.RetrySeconds);
        }

        [Test]
        public void TypedHttpClient_Default_And_SendOptions_InvalidOperationException()
        {
            var thc = new TypedHttpClient(new System.Net.Http.HttpClient());
            thc.WithRetry(5, 7);

            Assert.Throws<InvalidOperationException>(() => _ = thc.DefaultOptions);
            thc.Reset();
            _ = thc.DefaultOptions;

            thc = new TypedHttpClient(new System.Net.Http.HttpClient());
            thc.DefaultOptions.WithRetry(2, 4);
            thc.WithRetry(5, 7);

            _ = thc.DefaultOptions;
            Assert.Throws<InvalidOperationException>(() => thc.DefaultOptions.ThrowTransientException());
            thc.Reset();
            thc.DefaultOptions.ThrowTransientException();
        }
    }
}