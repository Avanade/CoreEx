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
    }
}