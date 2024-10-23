using CoreEx.Http;
using NUnit.Framework;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        [Test]
        public async Task Messages()
        {
            var r = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            r.Headers.Add("x-messages", """[{"type":"Warning","text":"Please renew licence."}]""");

            var hr = await HttpResult.CreateAsync(r);
            Assert.That(hr, Is.Not.Null);
            Assert.That(hr.Messages, Has.Count.EqualTo(1));
            Assert.That(hr.Messages[0].Type, Is.EqualTo(CoreEx.Entities.MessageType.Warning));
            Assert.That(hr.Messages[0].Text, Is.EqualTo("Please renew licence."));
        }
    }
}