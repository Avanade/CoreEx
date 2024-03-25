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
        private static void AssertIsInitial(TypedHttpClientOptions o)
        {
            Assert.That(o, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(o.ShouldThrowTransientException, Is.False);
                Assert.That(o.ShouldThrowKnownException, Is.False);
                Assert.That(o.ShouldThrowKnownUseContentAsMessage, Is.False);
                Assert.That(o.IsTransientPredicate, Is.Not.Null);
                Assert.That(o.ShouldEnsureSuccess, Is.False);
                Assert.That(o.ExpectedStatusCodes, Is.Null);
                Assert.That(o.BeforeRequest, Is.Null);
            });
        }

        [Test]
        public void ThrowTransientException()
        {
            var o = new TypedHttpClientOptions();
            o.ThrowTransientException();
            Assert.Multiple(() =>
            {
                Assert.That(o.ShouldThrowTransientException, Is.True);
                Assert.That(o.IsTransientPredicate, Is.Not.Null);
            });

            var o2 = new TypedHttpClientOptions(o);
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
            var o = new TypedHttpClientOptions();
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

            var o2 = new TypedHttpClientOptions(o);
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
            var o = new TypedHttpClientOptions();
            o.EnsureSuccess();
            Assert.That(o.ShouldEnsureSuccess, Is.True);

            var o2 = new TypedHttpClientOptions(o);
            Assert.That(o2.ShouldEnsureSuccess, Is.True);

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void Ensure()
        {
            var o = new TypedHttpClientOptions();
            o.EnsureOK();
            o.EnsureAccepted();
            o.EnsureCreated();
            o.EnsureNoContent();
            Assert.That(o.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent }));

            o.Ensure(HttpStatusCode.Continue, HttpStatusCode.Conflict);
            Assert.That(o.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }));

            var o2 = new TypedHttpClientOptions(o);
            Assert.That(o2.ExpectedStatusCodes!.ToArray(), Is.EqualTo(new HttpStatusCode[] { HttpStatusCode.OK, HttpStatusCode.Accepted, HttpStatusCode.Created, HttpStatusCode.NoContent, HttpStatusCode.Continue, HttpStatusCode.Conflict }));

            o.Reset();
            AssertIsInitial(o);
        }

        [Test]
        public void OnBeforeRequest()
        {
            var o = new TypedHttpClientOptions();
            o.OnBeforeRequest((r, ct) => Task.CompletedTask);
            Assert.That(o.BeforeRequest, Is.Not.Null);

            var o2 = new TypedHttpClientOptions(o);
            Assert.That(o2.BeforeRequest, Is.Not.Null);

            o.Reset();
            AssertIsInitial(o);
        }
    }
}