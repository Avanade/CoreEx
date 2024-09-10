// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Localization;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a <see cref="QueryOrderByParser"/> <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="message">The error message.</param>
    public class QueryOrderByParserException(string message) 
        : ValidationException(MessageItem.CreateErrorMessage(HttpConsts.QueryArgsOrderByQueryStringName, message), new LText(typeof(QueryFilterParserException).FullName, QueryFilterParserException.FallbackMessage)) { }
}