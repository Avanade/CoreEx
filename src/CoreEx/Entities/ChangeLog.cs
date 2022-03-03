// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

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
        public DateTime? CreatedDate { get => _createdDate; set => SetValue(ref _createdDate, value); }

        /// <summary>
        /// Gets or sets the created by (username).
        /// </summary>
        public string? CreatedBy { get => _createdBy; set => SetValue(ref _createdBy, value); }

        /// <summary>
        /// Gets or sets the updated <see cref="DateTime"/>.
        /// </summary>
        public DateTime? UpdatedDate { get => _updatedDate; set => SetValue(ref _updatedDate, value); }

        /// <summary>
        /// Gets or sets the updated by (username).
        /// </summary>
        public string? UpdatedBy { get => _updatedBy; set => SetValue(ref _updatedBy, value); }

        #region IEquatable

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj is ChangeLog other) && Equals(other);

        /// <summary>
        /// Determines whether the other <see cref="ChangeLog"/> is equal to the current <see cref="ChangeLog"/> by comparing all property values.
        /// </summary>
        /// <param name="other">The other object to compare to.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(ChangeLog? other) => ReferenceEquals(this, other) || (other != null
              && (Equals(CreatedDate, other!.CreatedDate)
              && Equals(CreatedBy, other.CreatedBy)
              && Equals(UpdatedDate, other.UpdatedDate)
              && Equals(UpdatedBy, other.UpdatedBy)));

        /// <summary>
        /// Compares two <see cref="ChangeLog"/> types for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(ChangeLog? a, ChangeLog? b) => Equals(a, b);

        /// <summary>
        /// Compares two <see cref="ChangeLog"/> types for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(ChangeLog? a, ChangeLog? b) => !Equals(a, b);

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

        #endregion

        #region ICopyFrom

        /// <summary>
        /// Performs a copy from another <see cref="ChangeLog"/> updating this instance.
        /// </summary>
        /// <param name="from">The <see cref="ChangeLog"/> to copy from.</param>
        public override void CopyFrom(ChangeLog from)
        {
            base.CopyFrom((object?)from);
            CreatedDate = from.CreatedDate;
            CreatedBy = from.CreatedBy;
            UpdatedDate = from.UpdatedDate;
            UpdatedBy = from.UpdatedBy;
        }

        #endregion

        #region ICloneable

        /// <inheritdoc/>
        public override ChangeLog Clone()
        {
            ChangeLog clone = new();
            clone.CopyFrom(this);
            return clone;
        }

        #endregion

        #region ICleanUp

        /// <inheritdoc/>
        public override void CleanUp()
        {
            base.CleanUp();
            CreatedDate = Cleaner.Clean(CreatedDate);
            CreatedBy = Cleaner.Clean(CreatedBy);
            UpdatedDate = Cleaner.Clean(UpdatedDate);
            UpdatedBy = Cleaner.Clean(UpdatedBy);
        }

        /// <inheritdoc/>
        public override bool IsInitial
            => Cleaner.IsInitial(CreatedDate)
            && Cleaner.IsInitial(CreatedBy)
            && Cleaner.IsInitial(UpdatedDate)
            && Cleaner.IsInitial(UpdatedBy);

        #endregion
    }
}