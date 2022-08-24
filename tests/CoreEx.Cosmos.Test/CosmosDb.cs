namespace CoreEx.Cosmos.Test
{
    public class CosmosDb : CoreEx.Cosmos.CosmosDb
    {
        public CosmosDb() : base(TestSetUp.CosmosDatabase!, TestSetUp.Mapper!) { }

        public CosmosDbContainer<Person1, Person1> Persons1 => Container<Person1, Person1>("Persons1");

        public CosmosDbContainer<Person2, Person2> Persons2 => Container<Person2, Person2>("Persons2");

        public CosmosDbValueContainer<Person3, Person3> Persons3 => ValueContainer<Person3, Person3>("Persons3");
    }
}