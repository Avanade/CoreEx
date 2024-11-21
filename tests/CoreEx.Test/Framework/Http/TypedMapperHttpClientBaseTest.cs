using CoreEx.Http.Extended;
using CoreEx.Mapping;
using Moq;
using NUnit.Framework;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using UnitTestEx;

namespace CoreEx.Test.Framework.Http
{
    [TestFixture]
    public class TypedMapperHttpClientBaseTest
    {
        [Test]
        public async Task MapSuccess()
        {
            var m = new Mapper();
            m.Register(new CustomerMapper());
            m.Register(new BackendMapper());

            var mcf = MockHttpClientFactory.Create();
            mcf.CreateDefaultClient().Request(HttpMethod.Post, "test").WithJsonBody(new Backend { First = "John", Last = "Doe" }).Respond.WithJson(new Backend { First = "John", Last = "Doe" });

            var mc = new TypedMappedHttpClient(mcf.GetHttpClient()!, m);
            var hr = await mc.PostMappedAsync<Customer, Backend, Customer, Backend>("test", new Customer { FirstName = "John", LastName = "Doe" });

            Assert.Multiple(() =>
            {
                Assert.That(hr.IsSuccess, Is.True);
                Assert.That(hr.Value, Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(hr.Value.FirstName, Is.EqualTo("John"));
                Assert.That(hr.Value.LastName, Is.EqualTo("Doe"));
            });

            var r = hr.ToResult();
            Assert.That(r.IsSuccess, Is.True);
        }

        [Test]
        public async Task MapServerError()
        {
            var m = new Mapper();
            m.Register(new CustomerMapper());
            m.Register(new BackendMapper());

            var mcf = MockHttpClientFactory.Create();
            mcf.CreateDefaultClient().Request(HttpMethod.Post, "test").WithJsonBody(new Backend { First = "John", Last = "Doe" }).Respond.With(HttpStatusCode.InternalServerError);

            var mc = new TypedMappedHttpClient(mcf.GetHttpClient()!, m);
            var hr = await mc.PostMappedAsync<Customer, Backend, Customer, Backend>("test", new Customer { FirstName = "John", LastName = "Doe" });

            Assert.Multiple(() =>
            {
                Assert.That(hr.IsSuccess, Is.False);
                Assert.That(hr.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
            });

            var r = hr.ToResult();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.Error, Is.TypeOf<HttpRequestException>());
            });
        }

        [Test]
        public async Task MapJsonError()
        {
            var m = new Mapper();
            m.Register(new CustomerMapper());
            m.Register(new BackendMapper());

            var mcf = MockHttpClientFactory.Create();
            mcf.CreateDefaultClient().Request(HttpMethod.Post, "test").WithJsonBody(new Backend { First = "John", Last = "Doe" }).Respond.WithJson("{\"first\":\"Dave\",\"age\":\"ten\"}");

            var mc = new TypedMappedHttpClient(mcf.GetHttpClient()!, m);
            var hr = await mc.PostMappedAsync<Customer, Backend, Customer, Backend>("test", new Customer { FirstName = "John", LastName = "Doe" });

            Assert.Multiple(() =>
            {
                Assert.That(hr.IsSuccess, Is.False);
                Assert.That(hr.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));
                Assert.That(hr.Exception, Is.TypeOf<InvalidOperationException>());
            });

            var r = hr.ToResult();
            Assert.Multiple(() =>
            {
                Assert.That(r.IsSuccess, Is.False);
                Assert.That(r.Error, Is.TypeOf<InvalidOperationException>());
            });
        }
    }

    public class Customer
    { 
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
    }

    public class Backend
    {
        public string? First { get; set; }
        public string? Last { get; set; }
        public int? Age { get; set; }
    }

    public class CustomerMapper : CoreEx.Mapping.Mapper<Customer, Backend>
    {
        protected override Backend? OnMap(Customer? source, Backend? destination, OperationTypes operationType)
        {
            destination ??= new Backend();
            destination.First = source?.FirstName;
            destination.Last = source?.LastName;
            return destination;
        }
    }

    public class BackendMapper : Mapper<Backend, Customer>
    {
        protected override Customer? OnMap(Backend? source, Customer? destination, OperationTypes operationType)
        {
            destination ??= new Customer();
            destination.FirstName = source?.First;
            destination.LastName = source?.Last;
            return destination;
        }
    }
}