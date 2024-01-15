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

        private static void AssertIsInitial(TypedHttpClientOptions o)
        {
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o.CustomRetryPolicy, Is.Null);
                Assert.That(o.RetryCount, Is.Null);
                Assert.That(o.RetrySeconds, Is.Null);
                Assert.That(o.ShouldThrowTransientException, Is.False);
                Assert.That(o.ShouldThrowKnownException, Is.False);
                Assert.That(o.ShouldThrowKnownUseContentAsMessage, Is.False);
                Assert.That(o.IsTransientPredicate, Is.Not.Null);
                Assert.That(o.ShouldEnsureSuccess, Is.False);
                Assert.That(o.ExpectedStatusCodes, Is.Null);
                Assert.That(o.MaxRetryDelay, Is.Null);
                Assert.That(o.BeforeRequest, Is.Null);
            });
        }

        [Test]
        public void WithRetry()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithRetry();
            Assert.Multiple(() =>
            {
                Assert.That(o.RetryCount, Is.EqualTo(3));
                Assert.That(o.RetrySeconds, Is.EqualTo(1.8d));
            });

            o.WithRetry(5);
            Assert.Multiple(() =>
            {
                Assert.That(o.RetryCount, Is.EqualTo(5));
                Assert.That(o.RetrySeconds, Is.EqualTo(1.8d));
            });

            o.WithRetry(null, 2.7d);
            Assert.Multiple(() =>
            {
                Assert.That(o.RetryCount, Is.EqualTo(3));
                Assert.That(o.RetrySeconds, Is.EqualTo(2.7));
            });

            o.WithRetry(2, 1.5d);
            Assert.Multiple(() =>
            {
                Assert.That(o.RetryCount, Is.EqualTo(2));
                Assert.That(o.RetrySeconds, Is.EqualTo(1.5));
            });

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.Multiple(() =>
            {
                Assert.That(o2.RetryCount, Is.EqualTo(2));
                Assert.That(o2.RetrySeconds, Is.EqualTo(1.5));
            });

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void ThrowTransientException()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.ThrowTransientException();
            Assert.Multiple(() =>
            {
                Assert.That(o.ShouldThrowTransientException, Is.True);
                Assert.That(o.IsTransientPredicate, Is.Not.Null);
            });

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.Multiple(() =>
            {
                Assert.That(o2.ShouldThrowTransientException, Is.True);
                Assert.That(o2.IsTransientPredicate, Is.Not.Null);
            });

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void ThrowKnownException()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.ThrowKnownException();
            Assert.Multiple(() =>
            {
                Assert.That(o.ShouldThrowKnownException, Is.True);
                Assert.That(o.ShouldThrowKnownUseContentAsMessage, Is.False);
            });

            o.ThrowKnownException(true);
            Assert.Multiple(() =>
            {
                Assert.That(o.ShouldThrowKnownException, Is.True);
                Assert.That(o.ShouldThrowKnownUseContentAsMessage, Is.True);
            });

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.Multiple(() =>
            {
                Assert.That(o2.ShouldThrowKnownException, Is.True);
                Assert.That(o2.ShouldThrowKnownUseContentAsMessage, Is.True);
            });

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void EnsureSuccess()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.EnsureSuccess();
            Assert.That(o.ShouldEnsureSuccess, Is.True);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.That(o2.ShouldEnsureSuccess, Is.True);

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
            Assert.That(o.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent }));

            o.Ensure(HttpStatusCode.Continue, HttpStatusCode.Conflict);
            Assert.That(o.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }));

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.That(o2.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }));

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void WithTimeout()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithTimeout(System.TimeSpan.FromMinutes(1));
            Assert.That(o.Timeout, Is.EqualTo(System.TimeSpan.FromMinutes(1)));

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.That(o2.Timeout, Is.EqualTo(System.TimeSpan.FromMinutes(1)));

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void WithMaxRetryDelay()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.WithMaxRetryDelay(System.TimeSpan.FromMinutes(2));
            Assert.That(o.MaxRetryDelay, Is.EqualTo(System.TimeSpan.FromMinutes(2)));

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.That(o2.MaxRetryDelay, Is.EqualTo(System.TimeSpan.FromMinutes(2)));

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void OnBeforeRequest()
        {
            var o = new TypedHttpClientOptions(new DefaultSettings());
            o.OnBeforeRequest((r, ct) => Task.CompletedTask);
            Assert.That(o.BeforeRequest, Is.Not.Null);

            var o2 = new TypedHttpClientOptions(new DefaultSettings(), o);
            Assert.That(o2.BeforeRequest, Is.Not.Null);

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
            Assert.Multiple(() =>
            {
                Assert.That(thc.DefaultOptions.RetryCount, Is.EqualTo(2));
                Assert.That(thc.DefaultOptions.RetrySeconds, Is.EqualTo(3d));
                Assert.That(thc.SendOptions.RetryCount, Is.EqualTo(2));
                Assert.That(thc.SendOptions.RetrySeconds, Is.EqualTo(3d));
            });

            thc.WithRetry(5, 7);
            Assert.Multiple(() =>
            {
                Assert.That(thc.DefaultOptions.RetryCount, Is.EqualTo(2));
                Assert.That(thc.DefaultOptions.RetrySeconds, Is.EqualTo(3d));
                Assert.That(thc.SendOptions.RetryCount, Is.EqualTo(5));
                Assert.That(thc.SendOptions.RetrySeconds, Is.EqualTo(7d));
            });

            thc.Reset();
            Assert.Multiple(() =>
            {
                Assert.That(thc.DefaultOptions.RetryCount, Is.EqualTo(2));
                Assert.That(thc.DefaultOptions.RetrySeconds, Is.EqualTo(3d));
                Assert.That(thc.SendOptions.RetryCount, Is.EqualTo(2));
                Assert.That(thc.SendOptions.RetrySeconds, Is.EqualTo(3d));
            });
        }

        [Test]
        public void TypedHttpClient_Default_And_SendOptions_InvalidOperationException()
        {
            var thc = new TypedHttpClient(new System.Net.Http.HttpClient());
            thc.WithRetry(5, 7);

            Assert.Throws<InvalidOperationException>(() => thc.DefaultOptions.WithRetry(3, 6));
            thc.Reset();
            thc.DefaultOptions.WithRetry(2, 8);

            thc = new TypedHttpClient(new System.Net.Http.HttpClient());
            thc.DefaultOptions.WithRetry(2, 4);
            thc.WithRetry(5, 7);

            Assert.Throws<InvalidOperationException>(() => thc.DefaultOptions.ThrowTransientException());
            thc.Reset();
            thc.DefaultOptions.ThrowTransientException();
        }
    }
}