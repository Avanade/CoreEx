// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Http
{
    /// <summary>
    /// Enables an argument with an <see cref="ArgType"/> and <see cref="Name"/> that also supports request URI template replacement via <see cref="ToEscapeDataString"/>.
    /// </summary>
    public interface IHttpArgTypeArg : IHttpArg
    {
        /// <summary>
        /// Gets the argument name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="HttpArgType"/> that determines how the argument is applied.
        /// </summary>
        public HttpArgType ArgType { get; }

        /// <summary>
        /// Returns a <see cref="Uri.EscapeDataString"/> representation of the value for URI template replacement use.
        /// </summary>
        /// <returns>The escaped data string.</returns>
        string? ToEscapeDataString();
    }
}