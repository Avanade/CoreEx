// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents the base capabilities for a full featured <b>Entity</b> which supports (overrides) the <c>==</c> and <c>!=</c> operators, and <see cref="Clone"/>.
    /// </summary>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    /// <remarks>To leverage this base class correctly the <see cref="EntityBase.GetPropertyValues"/> <b>must</b> be overridden for all updateable properties. For an example, see the underlying 
    /// <see href="https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Entities/ChangeLog.cs">ChangeLog</see> implementation.
    /// <para>Note, that there is additional implementation work required where inheriting from a class that has inherited from <see cref="EntityBase{TSelf}"/> to enable correct equality functionality. For example, if inheriting from
    /// <see cref="ChangeLog"/> then the following needs to be overridden to enable the expected functionality.
    /// <code>
    /// public class ChangeLogEx : ChangeLog
    /// {
    ///     private string? _reason;
    /// 
    ///     public string? Reason { get => _reason; set => SetValue(ref _reason, value); }
    /// 
    ///     protected override IEnumerable&lt;IPropertyValue> GetPropertyValues()
    ///     {
    ///         foreach (var pv in base.GetPropertyValues())
    ///             yield return pv;
    /// 
    ///         yield return CreateProperty(Reason, v => Reason = v);
    ///     }
    /// 
    ///     public override bool Equals(object? other) => base.Equals(other);
    /// 
    ///     public static bool operator ==(ChangeLogEx? a, ChangeLogEx? b) => Equals(a, b);
    /// 
    ///     public static bool operator !=(ChangeLogEx? a, ChangeLogEx? b) => !Equals(a, b);
    /// 
    ///     public override int GetHashCode() => base.GetHashCode();
    /// 
    ///     public override object Clone() => CreateClone&lt;ChangeLogEx>(this);
    /// }
    /// </code></para></remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityBase<TSelf> : EntityBase, IEquatable<TSelf>, ICloneable where TSelf : EntityBase<TSelf>, new()
    {
        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object? other) => base.Equals(other);

        /// <inheritdoc/>
        public bool Equals(TSelf? other) => base.Equals(other);

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityBase<TSelf>? a, EntityBase<TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityBase<TSelf>? a, EntityBase<TSelf>? b) => !Equals(a, b);

        /// <inheritdoc/>
        public override object Clone()
        {
            var clone = new TSelf();
            clone.CopyFrom(this);
            return clone;
        }
    }
}