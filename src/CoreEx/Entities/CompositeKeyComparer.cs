// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a comparer of equality for a <see cref="CompositeKey"/>.
    /// </summary>
    [System.Diagnostics.DebuggerStepThrough]
    public class CompositeKeyComparer : IEqualityComparer<CompositeKey>
    {
        private static readonly object _nullObject = new();

        /// <summary>
        /// Determines whether the specified <see cref="CompositeKey"/> values are equal.
        /// </summary>
        /// <param name="x">The first <see cref="CompositeKey"/> to compare.</param>
        /// <param name="y">The second <see cref="CompositeKey"/> to compare.</param>
        /// <returns><c>true</c> if the values are equal; otherwise, <c>false</c>.</returns>
        public bool Equals(CompositeKey x, CompositeKey y)
        {
            if (x.Args!.Length != y.Args!.Length)
                return false;

            for (int i = 0; i < x.Args.Length; i++)
            {
                if (!GetArgValue(x.Args[i]).Equals(GetArgValue(y.Args[i])))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code for the <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/> for which a hash code is to be returned.</param>
        /// <returns>A hash code for the <see cref="CompositeKey"/>.</returns>
        public int GetHashCode(CompositeKey key)
        {
            if (key.Args.Length == 0)
                return 0;

            int hashCode = 0;
            for (int i = 0; i < key.Args.Length; i++)
                hashCode ^= GetArgValue(key.Args[i]).GetHashCode();

            return hashCode;
        }

        /// <summary>
        /// Gets the argument value (handles a null value).
        /// </summary>
        private static object GetArgValue(object? arg) => arg ?? _nullObject;
    }
}