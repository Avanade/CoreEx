// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;

namespace CoreEx.Hosting.Work
{
    /// <summary>
    /// Represents the <see cref="WorkStateOrchestrator.CreateAsync"/> arguments.
    /// </summary>
    public class WorkStateArgs(string typeName, string? id = null) : IIdentifier<string>
    {
        /// <summary>
        /// Gets the underlying <see cref="Type"/> name for the specified <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
        /// <returns>The <see cref="Type.FullName"/>.</returns>
        public static string GetTypeName<T>() => typeof(T).FullName ?? typeof(T).Name;

        /// <summary>
        /// Creates the <see cref="WorkStateArgs"/> using the <typeparamref name="T"/> <see cref="Type.FullName"/> as the <see cref="TypeName"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
        /// <param name="id">The work identifier.</param>
        /// <returns>The newly instantiated <see cref="WorkStateArgs"/>.</returns>
        public static WorkStateArgs Create<T>(string? id = null) => new(GetTypeName<T>(), id);

        /// <summary>
        /// Gets or sets the <see cref="WorkStateOrchestrator"/> type name.
        /// </summary>
        /// <remarks>Enables separation between one or more <see cref="WorkState"/> types; see <see cref="WorkStateOrchestrator.GetAsync(string, string, System.Threading.CancellationToken)"/> to minimize cross-type access challenges.</remarks>
        public string TypeName { get; } = typeName.ThrowIfNullOrEmpty(nameof(typeName));

        /// <inheritdoc/>
        /// <remarks>The <see cref="WorkState.Id"/> will default to the <see cref="WorkStateOrchestrator.IdentifierGenerator"/> <see cref="IIdentifierGenerator.GenerateIdentifierAsync{TId, TFor}"/> value.</remarks>
        public string? Id { get; set; } = id;

        /// <summary>
        /// Gets or sets the related entity key where applicable.
        /// </summary>
        public string? Key { get; set; }

        /// <summary>
        /// Gets or sets the correlation identifier.
        /// </summary>
        /// <remarks>The <see cref="WorkState.CorrelationId"/> will default to the <see cref="ExecutionContext.CorrelationId"/> where not specified.</remarks>
        public string? CorrelationId { get; set; }

        /// <summary>
        /// Gets or sets the expiry <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>The <see cref="WorkState.Expiry"/> will default to the <see cref="WorkStateOrchestrator.ExpiryTimeSpan"/> where not specified.</remarks>
        public TimeSpan? Expiry { get; set; }
    }
}