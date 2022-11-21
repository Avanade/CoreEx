// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides the core <b>Reference Data</b> properties.
    /// </summary>
    public interface IReferenceData : IIdentifier, IETag
    {
        /// <summary>
        /// Gets or sets the unique code.
        /// </summary>
        string? Code { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        string? Text { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        string? Description { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        int SortOrder { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="IReferenceData"/> is active.
        /// </summary>
        /// <value><c>true</c> where active; otherwise, <c>false</c>.</value>
        bool IsActive { get; set; }

        /// <summary>
        /// Gets of sets the validity start date.
        /// </summary>
        DateTime? StartDate { get; set; }

        /// <summary>
        /// Gets of sets the validity end date.
        /// </summary>
        DateTime? EndDate { get; set; }

        /// <summary>
        /// Indicates whether the <see cref="IReferenceData"/> is known and in a valid state.
        /// </summary>
        [JsonIgnore]
        public bool IsValid => true;

        /// <summary>
        /// Overrides the standard <see cref="IsValid"/> check and flags the <see cref="ReferenceDataBaseEx{TId, TSelf}"/> as <b>Invalid</b>.
        /// </summary>
        /// <remarks>Will result in <see cref="IsActive"/> set to <c>false</c>. Once set to invalid it can not be changed; i.e. there is not an means to set back to valid.</remarks>
        void SetInvalid() { }

        /// <summary>
        /// Gets the underlying mapping dictionary.
        /// </summary>
        /// <remarks>The mapping dictionary property is intended for internal use only; generally speaking use <see cref="SetMapping{T}(string, T)"/>, <see cref="GetMapping{T}(string)"/> and <see cref="TryGetMapping{T}(string, out T)"/> 
        /// to access.</remarks>
        [JsonIgnore]
        Dictionary<string, object?>? Mappings => null!;

        /// <summary>
        /// Indicates whether any mapping values have been configured.
        /// </summary>
        [JsonIgnore]
        public bool HasMappings => false;

        /// <summary>
        /// Sets the mapping <paramref name="value"/> for the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <remarks>A <paramref name="value"/> with the default value will not be set; assumed in this case that no mapping exists.</remarks>
        public void SetMapping<T>(string name, T? value) where T : IComparable<T?>, IEquatable<T?> => throw new NotImplementedException();

        /// <summary>
        /// Gets a mapping value for the <see cref="ReferenceDataBaseEx{TId, TSelf}"/> for the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <returns>The mapping value where found; otherwise, the corresponding default value.</returns>
        public T? GetMapping<T>(string name) where T : IComparable<T?>, IEquatable<T?> => default!;

        /// <summary>
        /// Gets a mapping value for the <see cref="ReferenceDataBaseEx{TId, TSelf}"/> for the specified <paramref name="name"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="name">The mapping name.</param>
        /// <param name="value">The mapping value.</param>
        /// <returns><c>true</c> indicates that the name exists; otherwise, <c>false</c>.</returns>
        public bool TryGetMapping<T>(string name, [NotNullWhen(true)] out T? value) where T : IComparable<T?>, IEquatable<T?> { value = default!; return false; }
    }
}