using CoreEx.Http;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class HttpResultTest
    {
        [Test]
        public async Task Create_InternalException()
        {
            var r = new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK) { Content = new System.Net.Http.StringContent("[]") };
            var hr = await HttpResult.CreateAsync<int>(r);

            Assert.That(hr.IsSuccess, Is.False);
            Assert.Throws<InvalidOperationException>(() => hr.ThrowOnError());
            Assert.Throws<InvalidOperationException>(() => _ = hr.Value);

            var rr = hr.ToResult();
            Assert.Multiple(() =>
            {
                Assert.That(rr.IsSuccess, Is.False);
                Assert.That(rr.Error, Is.TypeOf<InvalidOperationException>());
            });
        }
    }
}