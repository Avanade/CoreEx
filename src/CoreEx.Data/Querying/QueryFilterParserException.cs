// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Localization;
using System;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a <see cref="QueryFilterParser"/> <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public sealed class QueryFilterParserException(string message) 
        : ValidationException(MessageItem.CreateErrorMessage(HttpConsts.QueryArgsFilterQueryStringName, message), new LText(typeof(QueryFilterParserException).FullName, FallbackMessage))
    {
        /// <summary>
        /// Gets the <see cref="LText.FallbackText"/> <see cref="Exception.Message"/>
        /// </summary>
        internal const string FallbackMessage = "A query validation error occurred.";
    }
}