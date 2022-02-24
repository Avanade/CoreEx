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
        /// Gets the <see cref="BackendHttpClient"/> endpoint URI.
        /// </summary>
        public Uri BackendEndpoint
        {
            get
            {
                var uri = GetRequiredValue<string>(nameof(BackendEndpoint));
                if (Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                    return new Uri(uri, UriKind.Absolute);
                else
                    throw new InvalidOperationException($"Configuration key '{nameof(BackendEndpoint)}' is not valid URI: {uri}");
            }
        }
    }
}