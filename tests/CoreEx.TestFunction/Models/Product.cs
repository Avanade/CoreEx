using CoreEx.Entities;
using Nsj = Newtonsoft.Json;
using Stj = System.Text.Json.Serialization;

namespace CoreEx.TestFunction.Models
{
    public class Product : IIdentifier<string>
    {
        [Nsj.JsonProperty("id")]
        [Stj.JsonPropertyName("id")]
        public string Id { get; set; }

        [Nsj.JsonProperty("name")]
        [Stj.JsonPropertyName("name")]
        public string Name { get; set; }

        [Nsj.JsonProperty("price")]
        [Stj.JsonPropertyName("price")]
        public decimal Price { get; set; }
    }

    public class BackendProduct : IPrimaryKey
    {
        [Nsj.JsonProperty("code")]
        [Stj.JsonPropertyName("code")]
        public string Code { get; set; }

        [Nsj.JsonProperty("description")]
        [Stj.JsonPropertyName("description")]
        public string Description { get; set; }

        [Nsj.JsonProperty("retailPrice")]
        [Stj.JsonPropertyName("retailPrice")]
        public decimal RetailPrice { get; set; }

        [Nsj.JsonIgnore]
        [Stj.JsonIgnore]
        public string Secret { get; set; }

        [Nsj.JsonIgnore]
        [Stj.JsonIgnore]
        public CompositeKey PrimaryKey => new CompositeKey(Code);
    }
}