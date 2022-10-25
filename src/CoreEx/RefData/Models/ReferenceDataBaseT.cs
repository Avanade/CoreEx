// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Diagnostics;

namespace CoreEx.RefData.Models
{
    /// <summary>
    /// Represents the <see cref="IReferenceData{TId}"/> implementation.
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {Text}, IsActive = {IsActive}")]
    public class ReferenceDataBase<TId> : ReferenceDataBase, IReferenceData<TId> where TId : IComparable<TId>, IEquatable<TId>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataBase{TId}"/> class.
        /// </summary>
        /// <remarks>The <see cref="Id"/> can only be of type <see cref="int"/>, <see cref="long"/>, <see cref="string"/> and <see cref="Guid"/>.</remarks>
        public ReferenceDataBase()
        {
            if (Id != null && Id is not int && Id is not long && Id is not string && Id is not Guid)
                throw new InvalidOperationException($"A Reference Data {nameof(Id)} can only be of type {nameof(Int32)}, {nameof(Int64)}, {nameof(String)} or {nameof(Guid)}.");
        }

        /// <inheritdoc/>
        Type IIdentifier.IdType { get => typeof(TId); }

        /// <inheritdoc/>
        object? IIdentifier.Id { get => Id; set => Id = (TId)value!; }

        /// <inheritdoc/>
        public new TId? Id { get; set; } = default!;
    }
}