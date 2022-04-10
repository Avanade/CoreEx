// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;
using System;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides a <i>default</i> <see cref="SettingsBase"/> implementation with no <i>prefixes</i> defined.
    /// </summary>
    /// <remarks>This is essentially just a light-weight wrapper over <see cref="IConfiguration"/>.</remarks>
    public class DefaultSettings : SettingsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSettings"/> class.
        /// </summary>
        /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
        public DefaultSettings(IConfiguration configuration) : base(configuration, prefixes: Array.Empty<string>()) { }
    }
}