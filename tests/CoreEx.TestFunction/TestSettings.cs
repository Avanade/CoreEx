using CoreEx.Configuration;
using Microsoft.Extensions.Configuration;
using System;

namespace CoreEx.TestFunction
{
    public class TestSettings : SettingsBase
    {
        /// <summary>
        /// Gets the setting prefixes in order of precedence.
        /// </summary>
        public static string[] Prefixes { get; } = { "Test/", "Common/" };

        /// <summary>
        /// Initializes a new instance of the <see cref="TestSettings"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        public TestSettings(IConfiguration configuration) : base(configuration, Prefixes) { }

        /// <summary>
        /// Gets the <see cref="BackendHttpClient"/> base endpoint/address URI.
        /// </summary>
        public Uri BackendBaseAddress
        {
            get
            {
                var uri = GetRequiredValue<string>(nameof(BackendBaseAddress));
                if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    return new Uri(uri, UriKind.Absolute);
                else
                    throw new InvalidOperationException($"Configuration key '{nameof(BackendBaseAddress)}' is not a valid URI: {uri}");
            }
        }

        /// <summary>
        /// The Azure Service Bus connection string used for <b>Publishing</b> when service bus is used.
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
        /// Name of the queue used for function trigger
        /// </summary>
        public string QueueName { get; set; }
    }
}