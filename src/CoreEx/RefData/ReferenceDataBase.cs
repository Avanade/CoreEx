// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Entities.Extended;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

namespace CoreEx.RefData
{
    /// <summary>
    /// Represents the base <see cref="IReferenceData"/> base implementation.
    /// </summary>
    /// <remarks>The <see cref="Id"/> can only be of type <see cref="int"/>, <see cref="long"/>, <see cref="string"/> and <see cref="Guid"/>.</remarks>
    [DebuggerDisplay("Id = {Id}, Code = {Code}, Text = {Text}, IsActivem = {IsActive}")]
    public class ReferenceDataBase<TId, TSelf> : EntityBase<TSelf>, IReferenceData<TId> where TId : IComparable<TId>, IEquatable<TId> where TSelf : ReferenceDataBase<TId, TSelf>, new()
    {
        private TId? _id;
        private string? _code;
        private string? _text;
        private string? _description;
        private int _sortOrder;
        private bool _isActive = true;
        private bool _isInvalid;
        private DateTime? _startDate;
        private DateTime? _endDate;
        private string? _etag;
        private Dictionary<string, object?>? _mappings;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataBase{TId, TSelf}"/> class.
        /// </summary>
        public ReferenceDataBase()
        {
            if (_id != null && _id is not int && _id is not long && _id is not string && _id is not Guid)
                throw new InvalidOperationException($"A Reference Data {nameof(Id)} can only be of type {nameof(Int32)}, {nameof(Int64)}, {nameof(String)} or {nameof(Guid)}.");
        }

        /// <inheritdoc/>
        Type IIdentifier.IdType { get => typeof(TId); }

        /// <inheritdoc/>
        object? IIdentifier.Id { get => Id; set => Id = (TId)value!; }

        /// <inheritdoc/>
        public TId? Id { get => _id; set => SetValue(ref _id, value, immutable: true); }

        /// <inheritdoc/>
        public string? Code { get => _code; set => SetValue(ref _code, value, StringTrim.Both, immutable: true); }

        /// <inheritdoc/>
        public string? Text { get => _text; set => SetValue(ref _text, value); }

        /// <inheritdoc/>
        public string? Description { get => _description; set => SetValue(ref _description, value); }

        /// <inheritdoc/>
        public int SortOrder { get => _sortOrder; set => SetValue(ref _sortOrder, value); }

        /// <inheritdoc/>
        /// <remarks>Note to classes that override: the base <see cref="IsActive"/> should be called as it verifies <see cref="IsActive"/>, and that the <see cref="StartDate"/> and <see cref="EndDate"/> are not outside of the 
        /// <see cref="IReferenceDataContext"/> <see cref="IReferenceDataContext.Date"/> where configured (otherwise <see cref="DateTime.UtcNow"/>). This is accessed via <see cref="ExecutionContext.Current"/> 
        /// <see cref="ExecutionContext.ReferenceDataContext"/> where <see cref="ExecutionContext.HasCurrent"/>. The <see cref="IsActive"/> will always return <c>false</c> when not <see cref="IsValid"/>.</remarks>
        public virtual bool IsActive
        {
            get
            {
                if (!IsValid || !_isActive)
                    return false;

                if (StartDate != null || EndDate != null)
                {
                    var date = ExecutionContext.HasCurrent ? ExecutionContext.Current.ReferenceDataContext[GetType()] : Cleaner.Clean(DateTime.UtcNow, DateTimeTransform.DateOnly);
                    if (StartDate != null && date < StartDate)
                        return false;

                    if (EndDate != null && date > EndDate)
                        return false;
                }

                return _isActive;
            }

            set => SetValue(ref _isActive, value);
        }

        /// <inheritdoc/>
        public DateTime? StartDate { get => _startDate; set => SetValue(ref _startDate, value, DateTimeTransform.DateOnly); }

        /// <inheritdoc/>
        public DateTime? EndDate { get => _endDate; set => SetValue(ref _endDate, value, DateTimeTransform.DateOnly); }

        /// <inheritdoc/>
        public string? ETag { get => _etag; set => SetValue(ref _etag, value); }

        /// <inheritdoc/>
        [JsonIgnore]
        public bool IsValid => !_isInvalid;

        /// <inheritdoc/>
        void IReferenceData.SetInvalid() => _isInvalid = true;

        /// <inheritdoc/>
        public bool HasMappings { get => _mappings != null && _mappings.Count > 0; }

        /// <inheritdoc/>
        Dictionary<string, object?> IReferenceData.Mappings => Mappings;

        /// <summary>
        /// Gets (creates) the mappings dictionary.
        /// </summary>
        private Dictionary<string, object?> Mappings => _mappings ??= new Dictionary<string, object?>();

        /// <inheritdoc/>
        public override string ToString() => Text ?? Code ?? Id?.ToString() ?? base.ToString();

        /// <inheritdoc/>
        public void SetMapping<T>(string name, T value) where T : IComparable<T>, IEquatable<T>
        {
            if (Comparer<T>.Default.Compare(value, default!) == 0)
                return;

            if (Mappings.ContainsKey(name))
                throw new InvalidOperationException(ValueIsImmutableMessage);

            if (IsReadOnly)
                throw new InvalidOperationException(EntityIsReadOnlyMessage);

            Mappings.Add(name, value);
        }

        /// <inheritdoc/>
        public T GetMapping<T>(string name) where T : IComparable<T>, IEquatable<T>
        {
            if (!HasMappings || !Mappings.TryGetValue(name, out var value))
                return default!;

            return (T)value!;
        }

        /// <inheritdoc/>
        public bool TryGetMapping<T>(string name, out T value) where T : IComparable<T>, IEquatable<T>
        {
            value = default!;
            if (!HasMappings || !Mappings.TryGetValue(name, out var val))
                return false;

            value = (T)val!;
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(TSelf? other) => ReferenceEquals(this, other) || (base.Equals(other)
            && Equals(Id, other!.Id)
            && Equals(Code, other.Code)
            && Equals(Text, other.Text)
            && Equals(Description, other!.Description)
            && Equals(SortOrder, other.SortOrder)
            && Equals(IsActive, other.IsActive)
            && Equals(StartDate, other.StartDate)
            && Equals(EndDate, other.EndDate))
            && MappingsEquals(other);

        /// <summary>
        /// Compare the mappings dictionaries.
        /// </summary>
        private bool MappingsEquals(TSelf other)
        {
            if (!HasMappings && !other.HasMappings)
                return true;

            if (Mappings.Count != other.Mappings.Count)
                return false;

            var el = ((IDictionary<string, object?>)Mappings!).GetEnumerator();
            var er = ((IDictionary<string, object?>)other.Mappings!).GetEnumerator();
            while (el.MoveNext())
            {
                if (!(er.MoveNext() && el.Current.Key != er.Current.Key || el.Current.Value != er.Current.Value))
                    return false;
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Id);
            hash.Add(Code);
            hash.Add(Text);
            hash.Add(Description);
            hash.Add(SortOrder);
            hash.Add(IsActive);
            hash.Add(StartDate);
            hash.Add(EndDate);
            if (HasMappings)
                Mappings.ForEach(i => hash.Add(HashCode.Combine(i.Key, i.Value)));

            return base.GetHashCode() ^ hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override void CopyFrom(TSelf from)
        {
            base.CopyFrom(from);
            Id = from.Id;
            Code = from.Code;
            Text = from.Text;
            Description = from.Description;
            SortOrder = from.SortOrder;
            IsActive = from.IsActive;
            StartDate = from.StartDate;
            EndDate = from.EndDate;

            if (from.HasMappings)
                from.Mappings.ForEach(i => Mappings.TryAdd(i.Key, i.Value));
        }

        /// <inheritdoc/>
        protected override void OnApplyAction(EntityAction action)
        {
            base.OnApplyAction(action);
            Id = ApplyAction(Id, action);
            Code = ApplyAction(Code, action);
            Text = ApplyAction(Text, action);
            Description = ApplyAction(Description, action);
            SortOrder = ApplyAction(SortOrder, action);
            IsActive = ApplyAction(IsActive, action);
            StartDate = ApplyAction(StartDate, action);
            Text = ApplyAction(Text, action);
        }

        /// <inheritdoc/>
        public override bool IsInitial => base.IsInitial
            && Cleaner.IsDefault(Id)
            && Cleaner.IsDefault(Code)
            && Cleaner.IsDefault(Text)
            && Cleaner.IsDefault(Description)
            && Cleaner.IsDefault(SortOrder)
            && Cleaner.IsDefault(IsActive, true)
            && Cleaner.IsDefault(StartDate)
            && Cleaner.IsDefault(EndDate)
            && !HasMappings;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to an <see cref="int"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="int"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator int(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is int id ? id : 0;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to a <see cref="Nullable{T}"/> <see cref="int"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="int"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator int?(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is int id ? id : null;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to a <see cref="long"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="long"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator long(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is long id ? id : 0;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to a <see cref="Nullable{T}"/> <see cref="long"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="long"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator long?(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is long id ? id : null;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to a <see cref="Guid"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="Guid"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator Guid(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is Guid id ? id : Guid.Empty;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to a <see cref="Nullable{T}"/> <see cref="Guid"/> where the <see cref="IIdentifier.Id"/> is of <see cref="Type"/> <see cref="Guid"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator Guid?(ReferenceDataBase<TId, TSelf>? item) => item != null && item.Id is Guid id ? id : null;

        /// <summary>
        /// An implicit cast from a <see cref="ReferenceDataBase{TId, TSelf}"/> to the <see cref="IReferenceData.Code"/> <see cref="string"/>.
        /// </summary>
        /// <param name="item">The <see cref="ReferenceDataBase{TId, TSelf}"/> value.</param>
        public static implicit operator string?(ReferenceDataBase<TId, TSelf>? item) => item?.Code;

        /// <summary>
        /// Performs a conversion from an <see cref="Id"/> to an instance of <typeparamref name="TSelf"/>.
        /// </summary>
        /// <param name="id">The <see cref="Id"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        public static TSelf ConvertFromId(TId id)
        {
            if (ExecutionContext.HasCurrent)
            {
                var rdc = ReferenceDataOrchestrator.Current[typeof(TSelf)];
                if (rdc != null && rdc.TryGetById(id, out var rd))
                    return (TSelf)rd!;
            }

            var rdx = new TSelf { Id = id };
            ((IReferenceData)rdx).SetInvalid();
            return rdx;
        }

        /// <summary>
        /// Performs a conversion from a <see cref="Code"/> to an instance of <typeparamref name="TSelf"/>.
        /// </summary>
        /// <param name="code">The <see cref="Code"/>.</param>
        /// <returns>The <typeparamref name="TSelf"/> instance.</returns>
        /// <remarks>Where the item (<see cref="IReferenceData"/>) is not found it will be created and <see cref="IReferenceData.SetInvalid"/> will be invoked.</remarks>
        public static TSelf ConvertFromCode(string? code)
        {
            if (code != null && ExecutionContext.HasCurrent)
            {
                var rdc = ReferenceDataOrchestrator.Current[typeof(TSelf)];
                if (rdc != null && rdc.TryGetByCode(code, out var rd))
                    return (TSelf)rd!;
            }

            var rdx = new TSelf { Code = code };
            ((IReferenceData)rdx).SetInvalid();
            return rdx;
        }
    }
}