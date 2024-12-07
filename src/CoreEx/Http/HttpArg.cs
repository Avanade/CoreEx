// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions.Reflection;
using CoreEx.Json;
using CoreEx.RefData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
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
    /// <param name="name">The argument <see cref="IHttpArgTypeArg.Name"/>.</param>
    /// <param name="value">The argument <see cref="Value"/>.</param>
    /// <param name="argType">The <see cref="IHttpArgTypeArg.ArgType"/>.</param>
    public class HttpArg<T>(string name, T value, HttpArgType argType = HttpArgType.FromUri) : IHttpArgTypeArg
    {
        private bool _isUsed = false;

        /// <summary>
        /// Gets the argument name.
        /// </summary>
        public string Name { get; } = name.ThrowIfNull(nameof(name));

        /// <summary>
        /// Gets the <see cref="HttpArgType"/> that determines how the argument is applied.
        /// </summary>
        public HttpArgType ArgType { get; } = argType;

        /// <summary>
        /// Gets the argument value.
        /// </summary>
        public T Value { get; } = value;

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
            else if (Value is DateTimeOffset dto)
                str = dto.ToString("o", CultureInfo.InvariantCulture);
            else if (Value is bool b)
                str = b.ToString().ToLowerInvariant();
            else if (Value is IFormattable fmt)
                str = fmt.ToString(null, CultureInfo.InvariantCulture);
            else
                str = Value.ToString();

            return str == null ? null : Uri.EscapeDataString(str);
        }

        /// <inheritdoc/>
        public void AddToQueryString(NameValueCollection queryString, IJsonSerializer? jsonSerializer = null)
        {
            if (_isUsed || ArgType == HttpArgType.FromBody || Comparer<T>.Default.Compare(Value, default!) == 0)
                return;

            if (AddNameValue(queryString, Name, Value))
                return;

            if (Value is IEnumerable enumerable)
            {
                foreach (var v in enumerable)
                {
                    if (v != null)
                    {
                        if (!AddNameValue(queryString, Name, v) && (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix))
                            AddComplexType(queryString, Value, jsonSerializer);
                    }
                }

                return;
            }

            if (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix)
                AddComplexType(queryString, Value, jsonSerializer);
        }

        /// <summary>
        /// Adds the named value.
        /// </summary>
        private static bool AddNameValue(NameValueCollection queryString, string name, object? value)
        {
            if (value == null)
                return true;

            if (value is string str)
                return AddNameValue(queryString, name, str);

            if (value is char ch)
                return AddNameValue(queryString, name, ch.ToString());

            if (value is DateTime dt)
                return AddNameValue(queryString, name, dt.ToString("o", CultureInfo.InvariantCulture));

            if (value is DateTimeOffset dto)
                return AddNameValue(queryString, name, dto.ToString("o", CultureInfo.InvariantCulture));

            if (value is IReferenceData rd && rd.Code is not null)
                return AddNameValue(queryString, name, rd.Code);

            if (value is Enum en)
                return AddNameValue(queryString, name, en.ToString());

            if (value is bool bo)
                return AddNameValue(queryString, name, bo.ToString().ToLowerInvariant());

            if (value is IFormattable fmt)
                return AddNameValue(queryString, name, fmt.ToString(null, CultureInfo.InvariantCulture));

            return false;
        }

        /// <summary>
        /// Adds the name and value
        /// </summary>
        private static bool AddNameValue(NameValueCollection queryString, string name, string value)
        {
            queryString.Add(name, value);
            return true;
        }

        /// <summary>
        /// Adds the complex type to the query string.
        /// </summary>
        private void AddComplexType(NameValueCollection queryString, object? value, IJsonSerializer? jsonSerializer)
        {
            if (value == null)
                return;

            var tr = TypeReflector.GetReflector(new TypeReflectorArgs(jsonSerializer), value.GetType());
            foreach (var pr in tr.GetProperties())
            {
                var pv = pr.PropertyInfo.GetValue(value, null);
                var name = $"{(ArgType == HttpArgType.FromUriUsePropertiesAndPrefix ? $"{Name}." : "")}{pr.JsonName ?? pr.Name}";
                if (pv is not string && pv is IEnumerable ie)
                {
                    foreach (var iv in ie)
                    {
                        if (!AddNameValue(queryString, name, iv))
                            throw new InvalidOperationException($"Type '{tr.Type.Name}' cannot be serialized to a URI; Type should be passed using Request Body [FromBody] given complexity.");
                    }
                }
                else
                {
                    if (!AddNameValue(queryString, name, pv))
                        throw new InvalidOperationException($"Type '{tr.Type.Name}' cannot be serialized to a URI; Type should be passed using Request Body [FromBody] given complexity.");
                }
            }
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