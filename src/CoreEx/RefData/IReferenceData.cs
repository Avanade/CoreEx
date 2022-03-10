// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the core <b>Reference Data</b> properties.
    /// </summary>
    public interface IReferenceData : IIdentifier
    {
        /// <summary>
        /// Gets or sets the unique code.
        /// </summary>
        string? Code { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        string? Text { get; set; }
    }
}