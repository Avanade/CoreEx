// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stj = System.Text.Json;

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

            (queryString, var isAdded) = AddNameValue(queryString, Name, Value);
            if (isAdded)
                return queryString;

            if (Value is IEnumerable enumerable)
            {
                foreach (var v in enumerable)
                {
                    if (v != null)
                    {
                        (queryString, isAdded) = AddNameValue(queryString, Name, v);
                        if (!isAdded && (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix))
                            queryString = AddNameSerializedValue(queryString, Name, v, jsonSerializer ??= new CoreEx.Text.Json.JsonSerializer());
                    }
                }

                return queryString;
            }

            if (ArgType == HttpArgType.FromUriUseProperties || ArgType == HttpArgType.FromUriUsePropertiesAndPrefix)
                return AddNameSerializedValue(queryString, Name, Value, jsonSerializer ?? new CoreEx.Text.Json.JsonSerializer());

            return queryString;
        }

        /// <summary>
        /// Adds the named value.
        /// </summary>
        private static (QueryString, bool) AddNameValue(QueryString queryString, string name, object? value)
        {
            if (value == null)
                return (queryString, true);

            if (value is string str)
                return (queryString.Add(name, str), true);

            if (value is char ch)
                return (queryString.Add(name, ch.ToString()), true);

            if (value is DateTime dt)
                return (queryString.Add(name, dt.ToString("o", CultureInfo.InvariantCulture)), true);

            if (value is IReferenceData rd)
                return (queryString.Add(name, rd.Code), true);

            if (value is IFormattable fmt)
                return (queryString.Add(name, fmt.ToString(null, CultureInfo.InvariantCulture)), true);

            if (value is Enum en)
                return (queryString.Add(name, en.ToString()), true);

            return (queryString, false);
        }

        /// <summary>
        /// Adds the named value.
        /// </summary>
        private QueryString AddNameSerializedValue(QueryString queryString, string name, object? value, IJsonSerializer jsonSerializer)
        {
            // Use serializer to get a System.Text.Json.JsonDocument for the value and add the first level of properties to the query string.
            if (jsonSerializer is Text.Json.JsonSerializer)
                return AddJsonElement(queryString, Stj.JsonSerializer.SerializeToDocument(value, (Stj.JsonSerializerOptions)jsonSerializer.Options).RootElement, name);

            var binary = jsonSerializer!.SerializeToBinaryData(Value, JsonWriteFormat.None);
            return AddJsonElement(queryString, Stj.JsonDocument.Parse(binary).RootElement, name);
        }

        /// <summary>
        /// Adds the properties found within the JsonDocument.
        /// </summary>
        private QueryString AddJsonElement(QueryString queryString, Stj.JsonElement json, string name)
        {
            if (json.ValueKind == Stj.JsonValueKind.Object)
            {
                foreach (var jp in json.EnumerateObject())
                {
                    var qn = ArgType == HttpArgType.FromUriUseProperties ? jp.Name : $"{name}.{jp.Name}";
                    switch (jp.Value.ValueKind)
                    {
                        case Stj.JsonValueKind.String:
                        case Stj.JsonValueKind.Number:
                        case Stj.JsonValueKind.True:
                        case Stj.JsonValueKind.False:
                            queryString = queryString.Add(qn, jp.Value.GetRawText());
                            break;

                        case Stj.JsonValueKind.Null:
                            break;

                        case Stj.JsonValueKind.Array:
                            foreach (var jae in jp.Value.EnumerateArray().Where(x => x.ValueKind == Stj.JsonValueKind.String || x.ValueKind == Stj.JsonValueKind.Number || x.ValueKind == Stj.JsonValueKind.True || x.ValueKind == Stj.JsonValueKind.False))
                            {
                                queryString = queryString.Add(qn, jae.ToString());
                            }

                            break;

                        default: break;
                    }
                }
            }
            else if (json.ValueKind == Stj.JsonValueKind.Array)
            {
                foreach (var jp in json.EnumerateArray())
                {
                    if (jp.ValueKind == Stj.JsonValueKind.String || jp.ValueKind == Stj.JsonValueKind.Number)
                        queryString = queryString.Add(name, jp.GetRawText());
                    else
                        throw new InvalidOperationException($"Type '{Value?.GetType().Name}' cannot be serialized to a URI; Type should be passed using Request Body [FromBody] given complexity.");
                }
            }

            return queryString;
        }

        /// <inheritdoc/>
        public Task ModifyHttpRequestAsync(HttpRequestMessage request, IJsonSerializer jsonSerializer, CancellationToken cancellationToken = default)
        {
            if (request.Content == null || ArgType != HttpArgType.FromBody || Value == null)
                return Task.CompletedTask;

            request.Content = new StringContent(jsonSerializer.Serialize(Value, JsonWriteFormat.None), Encoding.UTF8, MediaTypeNames.Application.Json);
            return Task.CompletedTask;
        }
    }
}