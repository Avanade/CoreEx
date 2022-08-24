using CoreEx.Entities;
using CoreEx.RefData.Models;
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
    }

    public class Person2 : Person1, IChangeLog, IETag
    {
        public ChangeLog? ChangeLog { get; set; }

        [JsonProperty("_etag")]
        public string? ETag { get; set; }
    }

    public class Person3 : IIdentifier<Guid>, IChangeLog, IETag
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime Birthday { get; set; }
        public decimal Salary { get; set; }
        public bool Locked { get; set; }
        public ChangeLog? ChangeLog { get; set; }
        public string? ETag { get; set; }
    }

    public class Gender : ReferenceDataBase<string> { }
}