// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a change log audit.
    /// </summary>
    public class ChangeLog : EntityBase<ChangeLog>
    {
        private DateTime? _createdDate;
        private string? _createdBy;
        private DateTime? _updatedDate;
        private string? _updatedBy;

        /// <summary>
        /// Gets or sets the created <see cref="DateTime"/>.
        /// </summary>
        [JsonPropertyName("createdDate")]
        public DateTime? CreatedDate { get => _createdDate; set => SetValue(ref _createdDate, value); }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        [JsonPropertyName("createdBy")]
        public string? CreatedBy { get => _createdBy; set => SetValue(ref _createdBy, value); }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        [JsonPropertyName("updatedDate")]
        public DateTime? UpdatedDate { get => _updatedDate; set => SetValue(ref _updatedDate, value); }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        [JsonPropertyName("updatedBy")]
        public string? UpdatedBy { get => _updatedBy; set => SetValue(ref _updatedBy, value); }

        /// <summary>
        /// Determines whether the other <see cref="ChangeLog"/> is equal to the current <see cref="ChangeLog"/> by comparing all property values.
        /// </summary>
        /// <param name="other">The other object to compare to.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(ChangeLog? other) => ReferenceEquals(this, other) || (other != null && base.Equals(other)
            && Equals(CreatedDate, other!.CreatedDate)
            && Equals(CreatedBy, other.CreatedBy)
            && Equals(UpdatedDate, other.UpdatedDate)
            && Equals(UpdatedBy, other.UpdatedBy));

        /// <summary>
        /// Returns a hash code for the <see cref="ChangeLog"/>.
        /// </summary>
        /// <returns>A hash code for the <see cref="ChangeLog"/>.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(CreatedDate);
            hash.Add(CreatedBy);
            hash.Add(UpdatedDate);
            hash.Add(UpdatedBy);
            return base.GetHashCode() ^ hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override object Clone() => CreateClone(this);

        /// <summary>
        /// Performs a copy from another <see cref="ChangeLog"/> updating this instance.
        /// </summary>
        /// <param name="from">The <see cref="ChangeLog"/> to copy from.</param>
        public override void CopyFrom(ChangeLog from)
        {
            base.CopyFrom(from);
            CreatedDate = from.CreatedDate;
            CreatedBy = from.CreatedBy;
            UpdatedDate = from.UpdatedDate;
            UpdatedBy = from.UpdatedBy;
        }

        /// <inheritdoc/>
        protected override void OnApplyAction(EntityAction action)
        {
            base.OnApplyAction(action);
            CreatedDate = ApplyAction(CreatedDate, action);
            CreatedBy = ApplyAction(CreatedBy, action);
            UpdatedDate = ApplyAction(UpdatedDate, action);
            UpdatedBy = ApplyAction(UpdatedBy, action);
        }

        /// <inheritdoc/>
        public override bool IsInitial => base.IsInitial
            && Cleaner.IsDefault(CreatedDate)
            && Cleaner.IsDefault(CreatedBy)
            && Cleaner.IsDefault(UpdatedDate)
            && Cleaner.IsDefault(UpdatedBy);
    }
}