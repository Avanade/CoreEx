// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Provides extension methods for the extended entities capabilities.
    /// </summary>
    public static class ExtendedExtensions
    {
        /// <summary>
        /// Creates a clone of <see cref="Type"/> <typeparamref name="T"/> by instantiating a new instance and performing a <see cref="ICopyFrom.CopyFrom(object?)"/> from the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <param name="from">The from value.</param>
        /// <returns>The new cloned instance.</returns>
        public static T Clone<T>(this T from) where T : ICopyFrom, new()
        {
            var clone = new T();
            clone.CopyFrom(from);
            return clone;
        }

        /// <summary>
        /// Creates (attempts even where default contstructor status is unknown) a clone of <see cref="Type"/> <typeparamref name="T"/> by instantiating a new instance and performing a <see cref="ICopyFrom.CopyFrom(object?)"/> from the <paramref name="from"/> value.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <param name="from">The from value.</param>
        /// <returns>The new cloned instance.</returns>
        internal static T ForceClone<T>(this T from) where T : EntityBase
        {
            var clone = Activator.CreateInstance<T>();
            clone.CopyFrom(from);
            return clone;
        }

        /// <summary>
        /// Creates a new <typeparamref name="T"/> instance and performs a <see cref="ICopyFrom.CopyFrom(object?)"/> using the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The entity <see cref="Type"/>.</typeparam>
        /// <param name="value">The from value.</param>
        /// <returns>The new copied instance.</returns>
        public static T CopyFromAs<T>(this EntityBase value) where T : EntityBase, new() => new T().Adjust(v => v.CopyFrom(value));
    }
}