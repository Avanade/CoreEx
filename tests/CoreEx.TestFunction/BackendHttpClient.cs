using CoreEx.Configuration;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System.Net.Http;

namespace CoreEx.TestFunction
{
    public class BackendHttpClient : TypedHttpClientCore<BackendHttpClient>
    {
        public BackendHttpClient(HttpClient client, IJsonSerializer jsonSerializer, SettingsBase settings, ILogger<TypedHttpClientCore<BackendHttpClient>> logger) 
            : base(client, jsonSerializer, settings, logger) { }
    }
}