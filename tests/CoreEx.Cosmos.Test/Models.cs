using CoreEx.Entities;
using CoreEx.RefData;
using Newtonsoft.Json;

namespace CoreEx.Cosmos.Test
{
    public class Person1 : IIdentifier<string>
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public decimal Salary { get; set; }
        public bool Locked { get; set; }
        public string? Filter { get; set; }
    }

    public class Person1Collection : List<Person1> { }

    public class Person1CollectionResult : CollectionResult<Person1Collection, Person1> { }

    public class Person2 : Person1, IChangeLog, IETag
    {
        public ChangeLog? ChangeLog { get; set; }

        [JsonProperty("_etag")]
        public string? ETag { get; set; }
    }

    public class Person2Collection : List<Person2> { }

    public class Person2CollectionResult : CollectionResult<Person2Collection, Person2> { }

    public class Person3 : IIdentifier<Guid>, IChangeLog, IETag
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public decimal Salary { get; set; }
        public bool Locked { get; set; }
        public string? Filter { get; set; }
        public ChangeLog? ChangeLog { get; set; }
        public string? ETag { get; set; }
    }

    public class Person3Collection : List<Person3> { }

    public class Person3CollectionResult : CollectionResult<Person3Collection, Person3> { }

    public class Gender : ReferenceDataBase<string> { }
}