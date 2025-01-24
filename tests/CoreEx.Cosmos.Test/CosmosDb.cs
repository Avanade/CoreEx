using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    public class CosmosDb : Cosmos.CosmosDb
    {
        private readonly Lazy<CosmosDbContainer<Person1, Person1>> _persons1;
        private readonly Lazy<CosmosDbContainer<Person2, Person2>> _persons2;
        private readonly Lazy<CosmosDbValueContainer<Person3, Person3>> _persons3;
        private readonly Lazy<CosmosDbValueContainer<Person1, Person1>> _persons3X;

        public CosmosDb(bool auth, bool partitioning = false) : base(TestSetUp.CosmosDatabase!, TestSetUp.Mapper!) 
        {
            _persons1 = new(() => Container("Persons1").AsTyped<Person1, Person1>().UsePartitionKey(partitioning ? v => new PartitionKey(v.Filter) : null).UseAuthorizeFilter(q => auth ? q.Where(x => x.Locked == false) : q));
            _persons2 = new(() => Container<Person2, Person2>("Persons2").UsePartitionKey(partitioning ? v => new PartitionKey(v.Filter) : null!).UseAuthorizeFilter(q => auth ? q.Where(x => x.Locked == false) : q));
            _persons3 = new(() => this["Persons3"].UseValuePartitionKey<Person3>(partitioning ? v => new PartitionKey(v.Value.Filter) : null!).AsValueTyped<Person3, Person3>().UseAuthorizeFilter(q => auth ? q.Where(x => x.Value.Locked == false) : q));
            _persons3X = new(() => ValueContainer<Person1, Person1>("Persons3").UsePartitionKey(partitioning ? v => new PartitionKey(v.Value.Filter) : null!));
        }

        public CosmosDbContainer<Person1, Person1> Persons1 => _persons1.Value;

        public CosmosDbContainer<Person2, Person2> Persons2 => _persons2.Value;

        public CosmosDbValueContainer<Person3, Person3> Persons3 => _persons3.Value;

        public CosmosDbValueContainer<Person1, Person1> Persons3X => _persons3X.Value;

        public CosmosDbContainer PersonsX => Container("PersonsX"); // Lazy not required as there is no one-off configuration.
    }
}