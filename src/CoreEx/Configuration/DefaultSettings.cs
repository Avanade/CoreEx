// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Configuration;

namespace CoreEx.Configuration
{
    /// <summary>
    /// Provides a <i>default</i> <see cref="SettingsBase"/> implementation with no <i>prefixes</i> defined.
    /// </summary>
    /// <remarks>This is essentially just a light-weight wrapper over <see cref="IConfiguration"/>.</remarks>
    /// <param name="configuration">The <see cref="IConfiguration"/>.</param>
    public class DefaultSettings(IConfiguration? configuration = null) : SettingsBase(configuration) { }
}