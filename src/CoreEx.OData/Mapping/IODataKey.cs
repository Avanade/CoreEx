// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.OData.Mapping
{
    /// <summary>
    /// Provides the ability to get the OData primary key from the typed value.
    /// </summary>
    public interface IODataKey
    {
        /// <summary>
        /// Gets the ODATA primary key from the <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The key.</returns>
        object[] GetODataKey(object value);
    }
}