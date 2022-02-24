// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx
{
    /// <summary>
    /// Represents a composite key (one or more values).
    /// </summary>
    //[System.Diagnostics.DebuggerStepThrough]
    [System.Diagnostics.DebuggerDisplay("Key = {ToString()}")]
    public struct CompositeKey : IEquatable<CompositeKey>
    {
        /// <summary>
        /// Represents an empty <see cref="CompositeKey"/>.
        /// </summary>
        public static readonly CompositeKey Empty;

        /// <summary>
        /// Initializes a new <see cref="CompositeKey"/> structure with one or more values that represent the composite key.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        public CompositeKey(params object?[] args) => Args = args ?? new object?[] { null };

        /// <summary>
        /// Gets the argument values for the key.
        /// </summary>
        public object?[] Args { get; }

        /// <summary>
        /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="other">The other <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        /// <remarks>Uses the <see cref="CompositeKeyComparer.Equals(CompositeKey, CompositeKey)"/>.</remarks>
        public bool Equals(CompositeKey other) => new CompositeKeyComparer().Equals(this, other);

        /// <summary>
        /// Determines whether the current <see cref="CompositeKey"/> is equal to another <see cref="Object"/>.
        /// </summary>
        /// <param name="obj">The other <see cref="object"/>.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj) => obj is CompositeKey key && Equals(key);

        /// <summary>
        /// Returns a hash code for the <see cref="CompositeKey"/>.
        /// </summary>
        /// <returns>A hash code for the <see cref="CompositeKey"/>.</returns>
        /// <remarks>Uses the <see cref="CompositeKeyComparer.GetHashCode(CompositeKey)"/>.</remarks>
        public override int GetHashCode() => new CompositeKeyComparer().GetHashCode(this);

        /// <summary>
        /// Compares two <see cref="CompositeKey"/> types for equality.
        /// </summary>
        /// <param name="left">The left <see cref="CompositeKey"/>.</param>
        /// <param name="right">The right <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(CompositeKey left, CompositeKey right) => left.Equals(right);

        /// <summary>
        /// Compares two <see cref="CompositeKey"/> types for non-equality.
        /// </summary>
        /// <param name="left">The left <see cref="CompositeKey"/>.</param>
        /// <param name="right">The right <see cref="CompositeKey"/>.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(CompositeKey left, CompositeKey right) => !(left == right);

        /// <summary>
        /// Determines whether the <see cref="CompositeKey"/> is considered initial; i.e. all <see cref="Args"/> have their default value.
        /// </summary>
        /// <returns><c>true</c> indicates that the <see cref="CompositeKey"/> is initial; otherwise, <c>false</c>.</returns>
        public bool IsInitial
        {
            get
            {
                if (Args == null || Args.Length == 0)
                    return true;

                foreach (var arg in Args)
                {
                    if (arg != null && !arg.Equals(GetDefaultValue(arg.GetType())))
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Gets the default value for a specified <paramref name="type"/>.
        /// </summary>
        private static object? GetDefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;

        /// <summary>
        /// Returns the <see cref="CompositeKey"/> as a comma-separated <see cref="Args"/> value <see cref="string"/>.
        /// </summary>
        /// <returns>The composite key as a <see cref="string"/>.</returns>
        public override string ToString() => Args == null || Args.Length == 0 ? "<none>" : string.Join(',', Args);
    }
}