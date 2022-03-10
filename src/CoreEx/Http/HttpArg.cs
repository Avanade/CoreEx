// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a base argument for an <see cref="HttpRequestMessage"/> where updating <see cref="HttpRequestMessage.RequestUri"/> or <see cref="HttpRequestMessage.Content"/> (body).
    /// </summary>
    public abstract class HttpArg
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpArg"/> class.
        /// </summary>
        /// <param name="name">The argument <see cref="Name"/>.</param>
        /// <param name="argType">The <see cref="HttpArgType"/>.</param>
        protected HttpArg(string name, HttpArgType argType = HttpArgType.FromUri)
        {
            Name = name;
            ArgType = argType;
            if (ArgType == HttpArgType.FromBody)
                IsUsed = true;
        }

        /// <summary>
        /// Gets the argument name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the <see cref="HttpArgType"/>.
        /// </summary>
        public HttpArgType ArgType { get; private set; }

        /// <summary>
        /// Indicates whether the value has been used/referenced.
        /// </summary>
        public bool IsUsed { get; internal set; }

        /// <summary>
        /// Indicates whether the value is null or default and therefore should be ignored.
        /// </summary>
        public abstract bool IsDefault { get; }

        /// <summary>
        /// Returns a <see cref="Uri.EscapeDataString"/> representation of just the <see cref="GetValue"/> itself.
        /// </summary>
        /// <returns>The escaped data string.</returns>
        public abstract string? ToEscapeDataString();

        /// <summary>
        /// Adds the <see cref="HttpArg"/> to the <see cref="QueryString"/>.
        /// </summary>
        /// <returns>The updated <see cref="QueryString"/>.</returns>
        public abstract QueryString AddToQueryString(QueryString queryString, IJsonSerializer? jsonSerializer = null);

        /// <summary>
        /// Gets the underlying value.
        /// </summary>
        /// <returns>The underlying value.</returns>
        public abstract object? GetValue();
    }
}