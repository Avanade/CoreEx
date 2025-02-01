using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    public class CosmosDb : Cosmos.CosmosDb
    {
        public CosmosDb(bool auth, bool partitioning = false) : base(TestSetUp.CosmosDatabase!, TestSetUp.Mapper!) 
        {
            // Apply the container configurations.
            Container("Persons1").UsePartitionKey<Person1>(partitioning ? v => new PartitionKey(v.Filter) : null).UseAuthorizeFilter<Person1>(q => auth ? q.Where(x => x.Locked == false) : q);
            Container("Persons2").UsePartitionKey<Person2>(partitioning ? v => new PartitionKey(v.Filter) : null!).UseAuthorizeFilter<Person2>(q => auth ? q.Where(x => x.Locked == false) : q);
            this["Persons3"].UseValuePartitionKey<Person3>(partitioning ? v => new PartitionKey(v.Value.Filter) : null!).UseValueAuthorizeFilter<Person3>(q => auth ? q.Where(x => x.Value.Locked == false) : q);
            Container("Persons3").UseValuePartitionKey<Person1>(partitioning ? v => new PartitionKey(v.Value.Filter) : null!);
        }

        public CosmosDbContainer<Person1, Person1> Persons1 => Container("Persons1").AsTyped<Person1, Person1>();

        public CosmosDbContainer<Person2, Person2> Persons2 => Container<Person2, Person2>("Persons2");

        public CosmosDbValueContainer<Person3, Person3> Persons3 => this["Persons3"].AsValueTyped<Person3, Person3>();

        public CosmosDbValueContainer<Person1, Person1> Persons3X => ValueContainer<Person1, Person1>("Persons3");

        public CosmosDbContainer PersonsX => Container("PersonsX");
    }
}