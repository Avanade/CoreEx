namespace CoreEx.Json;

/// <summary>
/// Provides <see cref="System.Text.Json"/> defaults; such as the primary <see cref="Configuration"/> and runtime <see cref="SerializerOptions"/> accessor.
/// </summary>
public class JsonDefaults
{
    /// <summary>
    /// Gets the default <see cref="JsonDefaultConfiguration"/>.
    /// </summary>
    /// <remarks>Use this property to customize the default behaviors for JSON serialization throughout the application.</remarks>
    public static JsonDefaultConfiguration Configuration { get; } = new();

    /// <summary>
    /// Gets the current <see cref="JsonSerializerOptions"/> from the <see cref="ExecutionContext"/> where found; otherwise, the references the <see cref="Configuration"/> instance.
    /// </summary>
    /// <remarks>Do not make changes to the underlying <see cref="JsonSerializerOptions"/> via this method as the instance may vary at runtime; changes should be made via <see cref="Configuration"/>
    /// or during service registration at application startup.</remarks>
    public static JsonSerializerOptions SerializerOptions => ExecutionContext.GetService<JsonSerializerOptions>() ?? Configuration.SerializerOptions;

    /// <summary>
    /// Provides the default JSON configuration settings for the application, which can be customized as needed. 
    /// </summary>
    /// <remarks>This encapsulates the default <see cref="JsonSerializerOptions"/> and allows for centralized management of JSON serialization settings.</remarks>
    public sealed class JsonDefaultConfiguration
    {
        private readonly JsonSubstituteNamingPolicy _jsonSubstituteNamingPolicy = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonDefaultConfiguration"/> class with <see cref="CoreEx"/> default settings.
        /// </summary>
        public JsonDefaultConfiguration()
        {
            SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                WriteIndented = false, 
                PropertyNamingPolicy = _jsonSubstituteNamingPolicy,
                DictionaryKeyPolicy = _jsonSubstituteNamingPolicy,
                Converters = { new JsonStringEnumConverter(), new JsonReferenceDataConverter(), new JsonDataMapConverterFactory() }
            };
        }

        /// <summary>
        /// Gets or sets the default <see cref="JsonSerializerOptions"/> configuration.
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; set => field = value.ThrowIfNull(); }
    }
}