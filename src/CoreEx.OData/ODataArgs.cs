// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Net;
using Soc = Simple.OData.Client;

namespace CoreEx.OData
{
    /// <summary>
    /// Provides the <b>OData</b> arguments.
    /// </summary>
    public struct ODataArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataArgs"/> struct.
        /// </summary>
        public ODataArgs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataArgs"/> struct from a <paramref name="template"/>.
        /// </summary>
        /// <param name="template">The template <see cref="ODataArgs"/> to copy from.</param>
        public ODataArgs(ODataArgs template)
        {
            NullOnNotFound = template.NullOnNotFound;
            CleanUpResult = template.CleanUpResult;
            PreReadOnUpdate = template.PreReadOnUpdate;
            PreReadOnDelete = template.PreReadOnDelete;
            IsPagingGetCountSupported = template.IsPagingGetCountSupported;
        }

        /// <summary>
        /// Indicates that a <c>null</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> on <b>Get</b>. Defaults to <c>true</c>.
        /// </summary>
        /// <remarks>Consider setting <see cref="Soc.ODataClientSettings.IgnoreResourceNotFoundException"/> to <c>true</c> which ensure a <c>null</c> is returned avoiding the cost of an unnecessary exception.</remarks>
        public bool NullOnNotFound { get; set; } = true;

        /// <summary>
        /// Indicates whether the result should be <see cref="Entities.Cleaner.Clean{T}(T)">cleaned up</see>.
        /// </summary>
        public bool CleanUpResult { get; set; } = false;

        /// <summary>
        /// Indicates whether a pre-read (<b>Get</b>) should be performed prior to an <b>Update</b> operation to ensure that the entity exists before attempting. Defaults to <c>false</c>.
        /// </summary>
        public bool PreReadOnUpdate { get; set; } = false;

        /// <summary>
        /// Indicates whether a pre-read (<b>Get</b>) should be performed prior to a <b>Delete</b> operation to ensure that the entity exists before attempting. Defaults to <c>false</c>.
        /// </summary>
        public bool PreReadOnDelete { get; set; } = false;

        /// <summary>
        /// Indicates whether <see cref="CoreEx.Entities.PagingArgs.IsGetCount"/> is supported; i.e. does the OData endpoint support <c>$count=true</c>. Defaults to <c>true</c>.
        /// </summary>
        public bool IsPagingGetCountSupported { get; set; } = true;
    }
}