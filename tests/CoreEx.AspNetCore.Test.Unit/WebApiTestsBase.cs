using CoreEx.Entities;

namespace CoreEx.AspNetCore.Test.Unit;

[Parallelizable]
public abstract partial class WebApiTestsBase<TWebApi, TResult> : WithApiTester<Api.Program> where TWebApi : Abstractions.WebApi<TResult>
{ 
    public class Person : IETag
    {
        public static Person GetPerson(string? etag = null) => new() { FirstName = "John", LastName = "Doe", Age = 30, ETag = etag };

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int? Age { get; set; }
        public string? ETag { get; set; }
    }

    public class Person2 : Person { }
}