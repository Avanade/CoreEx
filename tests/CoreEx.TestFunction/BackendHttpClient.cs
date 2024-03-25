using CoreEx.Http;
using CoreEx.Json;
using System.Net.Http;

namespace CoreEx.TestFunction
{
    public class BackendHttpClient(HttpClient client, IJsonSerializer jsonSerializer, ExecutionContext executionContext) : TypedHttpClientCore<BackendHttpClient>(client, jsonSerializer, executionContext) { }
}