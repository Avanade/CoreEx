// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Cosmos
{
    /// <summary>
    /// Defines the <see cref="Type"/> property.
    /// </summary>
    public interface ICosmosDbType
    {
        /// <summary>
        /// Gets the model <see cref="System.Type"/> name.
        /// </summary>
        /// <remarks>Enables multiple models to be managed within a single container leveraging different types.</remarks>
        string Type { get; }
    }
}