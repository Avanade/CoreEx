// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System.Collections.Generic;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides HTTP configurable consts for Headers and QueryString.
    /// </summary>
    public static class HttpConsts
    {
        #region HeaderName

        /// <summary>
        /// Gets or sets the header name for the exception error type value.
        /// </summary>
        public static string ErrorTypeHeaderName { get; set; } = "x-error-type";

        /// <summary>
        /// Gets or sets the header name for the exception error code value.
        /// </summary>
        public static string ErrorCodeHeaderName { get; set; } = "x-error-code";

        /// <summary>
        ///  Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingArgs.Page"/>.
        /// </summary>
        public static string PagingPageNumberHeaderName { get; set; } = "x-paging-page-number";

        /// <summary>
        /// Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingArgs.Take"/>.
        /// </summary>
        public static string PagingPageSizeHeaderName { get; set; } = "x-paging-page-size";

        /// <summary>
        /// Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingArgs.Skip"/>.
        /// </summary>
        public static string PagingSkipHeaderName { get; set; } = "x-paging-skip";

        /// <summary>
        /// Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingArgs.Take"/>.
        /// </summary>
        public static string PagingTakeHeaderName { get; set; } = "x-paging-take";

        /// <summary>
        /// Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingResult.TotalCount"/>.
        /// </summary>
        public static string PagingTotalCountHeaderName { get; set; } = "x-paging-total-count";

        /// <summary>
        /// Gets or sets the header name for the <see cref="PagingResult"/> <see cref="PagingResult.TotalPages"/>.
        /// </summary>
        public static string PagingTotalPagesHeaderName { get; set; } = "x-paging-total-pages";

        /// <summary>
        /// Gets or sets the header name for the messages.
        /// </summary>
        public static string MessagesHeaderName { get; set; } = "x-messages";

        /// <summary>
        /// Gets or sets the header name for the <see cref="ExecutionContext.CorrelationId"/>.
        /// </summary>
        public static string CorrelationIdHeaderName { get; set; } = "x-correlation-id";

        #endregion

        #region QueryStringName

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.IncludeFields"/> query string name.
        /// </summary>
        public static string IncludeFieldsQueryStringName { get; set; } = "$fields";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.ExcludeFields"/> query string name.
        /// </summary>
        public static string ExcludeFieldsQueryStringName { get; set; } = "$exclude";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.Paging"/> <see cref="PagingArgs.Page"/> query string name.
        /// </summary>
        public static string PagingArgsPageQueryStringName { get; set; } = "$page";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.Paging"/> <see cref="PagingArgs.Size"/> query string name.
        /// </summary>
        public static string PagingArgsSizeQueryStringName { get; set; } = "$size";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.Paging"/> <see cref="PagingArgs.Skip"/> query string name.
        /// </summary>
        public static string PagingArgsSkipQueryStringName { get; set; } = "$skip";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.Paging"/> <see cref="PagingArgs.Take"/> query string name.
        /// </summary>
        public static string PagingArgsTakeQueryStringName { get; set; } = "$take";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.Paging"/> <see cref="PagingArgs.IsGetCount"/> query string name.
        /// </summary>
        public static string PagingArgsCountQueryStringName { get; set; } = "$count";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.IncludeText"/> query string name.
        /// </summary>
        /// <remarks>See <see cref="ExecutionContext.IsTextSerializationEnabled"/>.</remarks>
        public static string IncludeTextQueryStringName { get; set; } = "$text";

        /// <summary>
        /// Gets or sets the <see cref="HttpRequestOptions.IncludeInactive"/> query string name.
        /// </summary>
        public static string IncludeInactiveQueryStringName { get; set; } = "$inactive";

        #endregion

        #region QueryStringNames

        /// <summary>
        /// Gets or sets the list of possible <see cref="PagingArgs.Page"/> query string names.
        /// </summary>
        public static IEnumerable<string> PagingArgsPageQueryStringNames { get; set; } = new List<string>(new string[] { "$page", "$pageNumber", "paging-page" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="PagingArgs.Skip"/> query string names.
        /// </summary>
        public static IEnumerable<string> PagingArgsSkipQueryStringNames { get; set; } = new List<string>(new string[] { "$skip", "$offset", "paging-skip" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="PagingArgs.Take"/> query string names.
        /// </summary>
        public static IEnumerable<string> PagingArgsTakeQueryStringNames { get; set; } = new List<string>(new string[] { "$take", "$top", "$size", "$pageSize", "$limit", "paging-take", "paging-size" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="PagingArgs.IsGetCount"/> query string names.
        /// </summary>
        public static IEnumerable<string> PagingArgsCountQueryStringNames { get; set; } = new List<string>(new string[] { "$count", "$totalCount", "paging-count" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="HttpRequestOptions.IncludeFields"/> query string names.
        /// </summary>
        public static IEnumerable<string> IncludeFieldsQueryStringNames { get; set; } = new List<string>(new string[] { "$fields", "$includeFields", "$include", "include-fields" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="HttpRequestOptions.ExcludeFields"/> query string names.
        /// </summary>
        public static IEnumerable<string> ExcludeFieldsQueryStringNames { get; set; } = new List<string>(new string[] { "$excludeFields", "$exclude", "exclude-fields" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="HttpRequestOptions.IncludeText"/> query string names.
        /// </summary>
        public static IEnumerable<string> IncludeTextQueryStringNames { get; set; } = new List<string>(new string[] { "$text", "$includeText", "include-text" });

        /// <summary>
        /// Gets or sets the list of possible <see cref="HttpRequestOptions.IncludeInactive"/> query string names.
        /// </summary>
        public static IEnumerable<string> IncludeInactiveQueryStringNames { get; set; } = new List<string>(new string[] { "$inactive", "$includeInactive", "include-inactive" });

        #endregion

        #region MediaTypeName

        /// <summary>
        /// Gets the <see cref="HttpPatchOption.JsonPatch"/> media type name.
        /// </summary>
        public const string JsonPatchMediaTypeName = "application/json-patch+json";

        /// <summary>
        /// Gets the <see cref="HttpPatchOption.MergePatch"/> media type name.
        /// </summary>
        public const string MergePatchMediaTypeName = "application/merge-patch+json";

        #endregion
    }
}