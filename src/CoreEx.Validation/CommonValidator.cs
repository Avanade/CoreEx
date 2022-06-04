// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides access to the common validator capabilities.
    /// </summary>
    public static class CommonValidator
    {
        /// <summary>
        /// Creates a new instance of the <see cref="CommonValidator{T}"/>.
        /// </summary>
        /// <param name="validator">An action with the <see cref="CommonValidator{T}"/>.</param>
        /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
        public static CommonValidator<T> Create<T>(Action<CommonValidator<T>> validator)
        {
            var cv = new CommonValidator<T>();
            validator?.Invoke(cv);
            return cv;
        }
    }
}