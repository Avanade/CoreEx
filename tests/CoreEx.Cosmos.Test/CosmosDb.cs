using Microsoft.Azure.Cosmos;

namespace CoreEx.Cosmos.Test
{
    public class CosmosDb : CoreEx.Cosmos.CosmosDb
    {
        private readonly bool _partitioning;

        public CosmosDb(bool auth, bool partitioning = false) : base(TestSetUp.CosmosDatabase!, TestSetUp.Mapper!) 
        {
            if (auth)
            {
                UseAuthorizeFilter<Person1>("Persons1", q => ((IQueryable<Person1>)q).Where(x => x.Locked == false));
                UseAuthorizeFilter<Person2>("Persons2", q => ((IQueryable<Person2>)q).Where(x => x.Locked == false));
                UseAuthorizeFilter<Person3>("Persons3", q => ((IQueryable<CosmosDbValue<Person3>>)q).Where(x => x.Value.Locked == false));
            }

            _partitioning = partitioning;
        }

        public CosmosDbContainer<Person1, Person1> Persons1 => Container<Person1, Person1>("Persons1").UsePartitionKey(_partitioning ? v => new PartitionKey(v.Filter) : null!);

        public CosmosDbContainer<Person2, Person2> Persons2 => Container<Person2, Person2>("Persons2").UsePartitionKey(_partitioning ? v => new PartitionKey(v.Filter) : null!);

        public CosmosDbValueContainer<Person3, Person3> Persons3 => ValueContainer<Person3, Person3>("Persons3").UsePartitionKey(_partitioning ? v => new PartitionKey(v.Filter) : null!);
    }
}