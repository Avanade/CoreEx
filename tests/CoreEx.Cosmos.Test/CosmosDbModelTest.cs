using CoreEx.Cosmos.Model;
using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.Test
{
    [TestFixture]
    [Category("WithCosmos")]
    public class CosmosDbModelTest
    {
        private CosmosDb _db;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            await TestSetUp.SetUpAsync().ConfigureAwait(false);
            _db = new CosmosDb(auth: false);
        }

        [Test]
        public async Task SelectMultiSetWithResultAsync()
        {
            PersonX1[] people = Array.Empty<PersonX1>();
            var hasPerson = false;

            var result = await _db.PersonsX.Model.SelectMultiSetWithResultAsync(PartitionKey.None,
                new MultiSetModelCollArgs<PersonX1>(r => people = r.ToArray()),
                new MultiSetModelSingleArgs<PersonX2>(r => hasPerson = true));

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(people, Has.Length.EqualTo(2));
                Assert.That(hasPerson, Is.True);
            });
        }
    }
}