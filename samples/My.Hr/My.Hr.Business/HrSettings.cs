namespace My.Hr.Business;

public class HrSettings : SettingsBase
{
    /// <summary>
    /// Gets the setting prefixes in order of precedence.
    /// </summary>
    public static string[] Prefixes { get; } = { "Hr/", "Common/" };

    /// <summary>
    /// Initializes a new instance of the <see cref="HrSettings"/> class.
    /// </summary>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    public HrSettings(IConfiguration configuration) : base(configuration, Prefixes) { }

    public string AgifyApiEndpointUri => GetValue<string>();

    public string NationalizeApiClientApiEndpointUri => GetValue<string>();

    public string GenderizeApiClientApiEndpointUri => GetValue<string>();

    public string VerificationQueueName => GetValue<string>();

    public string VerificationResultsQueueName => GetValue<string>();

    /// <summary>
    /// The Azure Service Bus connection string used for <b>Publishing</b> in <see cref="Messaging.ServiceBusPublisher"/>.
    /// </summary>
    /// <remarks>It defaults to managed identity connection string used by triggers 'ServiceBusConnection__fullyQualifiedNamespace'</remarks>
    public string ServiceBusConnection => GetValue<string>(defaultValue: ServiceBusConnection__fullyQualifiedNamespace);

    /// <summary>
    /// The Azure Service Bus connection string used by <b>Triggers</b> using managed identity.
    /// </summary>
    /// <remarks> <b>Caution</b> this key is used implicitly by function triggers when 'ServiceBusConnection' is not set. </remarks>
    /// <remarks> Underscores in environment variables are replaced by semicolon ':' in configuration object, hence lookup also replaces '__' with ':'</remarks>
    public string ServiceBusConnection__fullyQualifiedNamespace => GetValue<string>();

    /// <summary>
    /// SQL Server connection string used by the app (depending on the value it may use managed identity or username/password)
    /// </summary>
    /// <remarks> Underscores in environment variables are replaced by semicolon ':' in configuration object, hence lookup also replaces '__' with ':'</remarks>
    public string ConnectionStrings__Database => GetValue<string>();
}