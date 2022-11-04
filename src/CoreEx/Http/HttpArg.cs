// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Json;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents an <see cref="IHttpArgTypeArg"/> argument for an <see cref="HttpRequestMessage"/> where updating <see cref="HttpRequestMessage.RequestUri"/> or <see cref="HttpRequestMessage.Content"/> (body).
    /// </summary>
    /// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
    public class HttpArg<T> : IHttpArgTypeArg
    {
        private bool _isUsed = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpArg{Object}"/> class.
        /// </summary>
        /// <param name="name">The argument <see cref="IHttpArgTypeArg.Name"/>.</param>
        /// <param name="value">The argument <see cref="Value"/>.</param>
        /// <param name="argType">The <see cref="IHttpArgTypeArg.ArgType"/>.</param>
        public HttpArg(string name, T value, HttpArgType argType = HttpArgType.FromUri)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value;
            ArgType = argType;
        }

        /// <summary>
        /// Gets the argument name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the <see cref="HttpArgType"/> that determines how the argument is applied.
        /// </summary>
        public HttpArgType ArgType { get; }

        /// <summary>
        /// Gets the argument value.
        /// </summary>
        public T Value { get;}

        /// <inheritdoc/>
        public string? ToEscapeDataString()
        {
            if (ArgType != HttpArgType.FromUri)
                return null;

            _isUsed = true;
            string? str;
            if (Value == null)
                return null;
            else if (Value is IReferenceData rd)
                str = rd?.Code;
            else if (Value is DateTime dt)
                str = dt.ToString("o", CultureInfo.InvariantCulture);
            else if (Value is IFormattable fmt)
                str = fmt.ToString(null, CultureInfo.InvariantCulture);
            else
                str = Value.ToString();

            return str == null ? null : Uri.EscapeDataString(str);
        }

        /// <inheritdoc/>
        public QueryString AddToQueryString(QueryString queryString, IJsonSerializer? jsonSerializer = null)
        {
            if (_isUsed || ArgType == HttpArgType.FromBody || Comparer<T>.Default.Compare(Value, default!) == 0)
                return queryString;

            (queryString, var wasAdded) = AddNameValue(queryString, Name, Value);
            if (wasAdded)
                return queryString;

            if (Value is IEnumerable enumerable)
            {
                foreach (var v in enumerable)
                {
                    if (v != null)
                    {
                        (queryString, wasAdded) = AddNameValue(queryString, Name, v);
                        if (!wasAdded && (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix))
                            queryString = AddComplexType(queryString, Value, jsonSerializer);
                    }
                }

                return queryString;
            }

            if (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix)
                queryString = AddComplexType(queryString, Value, jsonSerializer);

            return queryString;
        }

        /// <summary>
        /// Adds the named value.
        /// </summary>
        private static (QueryString QueryString, bool WasAdded) AddNameValue(QueryString queryString, string name, object? value)
        {
            if (value == null)
                return (queryString, true);

            if (value is string str)
                return (queryString.Add(name, str), true);

            if (value is char ch)
                return (queryString.Add(name, ch.ToString()), true);

            if (value is DateTime dt)
                return (queryString.Add(name, dt.ToString("o", CultureInfo.InvariantCulture)), true);

            if (value is DateTimeOffset dto)
                return (queryString.Add(name, dto.ToString("o", CultureInfo.InvariantCulture)), true);

            if (value is IReferenceData rd)
                return (queryString.Add(name, rd.Code), true);

            if (value is Enum en)
                return (queryString.Add(name, en.ToString()), true);

            if (value is bool bo)
                return (queryString.Add(name, bo.ToString().ToLowerInvariant()), true);

            if (value is IFormattable fmt)
                return (queryString.Add(name, fmt.ToString(null, CultureInfo.InvariantCulture)), true);

            return (queryString, false);
        }

        /// <summary>
        /// Adds the complex type to the query string.
        /// </summary>
        private QueryString AddComplexType(QueryString queryString, object? value, IJsonSerializer? jsonSerializer)
        {
            if (value == null)
                return queryString;

            var tr = TypeReflector.GetReflector(new TypeReflectorArgs(jsonSerializer), value.GetType());
            foreach (var pr in tr.GetProperties())
            {
                var pv = pr.PropertyInfo.GetValue(value, null);
                var name = $"{(ArgType == HttpArgType.FromUriUsePropertiesAndPrefix ? $"{Name}." : "")}{pr.JsonName ?? pr.Name}";
                if (pv is not string && pv is IEnumerable ie)
                {
                    foreach (var iv in ie)
                    {
                        (queryString, var wasAdded) = AddNameValue(queryString, name, iv);
                        if (!wasAdded)
                            throw new InvalidOperationException($"Type '{tr.Type.Name}' cannot be serialized to a URI; Type should be passed using Request Body [FromBody] given complexity.");
                    }
                }
                else
                {
                    (queryString, var wasAdded) = AddNameValue(queryString, name, pv);
                    if (!wasAdded)
                        throw new InvalidOperationException($"Type '{tr.Type.Name}' cannot be serialized to a URI; Type should be passed using Request Body [FromBody] given complexity.");
                }
            }

            return queryString;
        }

        /// <inheritdoc/>
        public Task ModifyHttpRequestAsync(HttpRequestMessage request, IJsonSerializer jsonSerializer, CancellationToken cancellationToken = default)
        {
            if (request.Content != null || ArgType != HttpArgType.FromBody || Value == null)
                return Task.CompletedTask;

            request.Content = new StringContent(jsonSerializer.Serialize(Value, JsonWriteFormat.None), Encoding.UTF8, MediaTypeNames.Application.Json);
            return Task.CompletedTask;
        }
    }
}