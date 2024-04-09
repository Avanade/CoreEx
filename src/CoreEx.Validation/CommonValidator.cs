// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides access to the common value validator capabilities.
    /// </summary>
    public static class CommonValidator
    {
        /// <summary>
        /// Create a new instance of the <see cref="CommonValidator{T}"/>.
        /// </summary>
        /// <param name="configure">An action with the <see cref="CommonValidator{T}"/> to enable further configuration.</param>
        /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
        /// <remarks>This is a synonym for the <see cref="Validator.CreateFor{T}(Action{CommonValidator{T}})"/>.</remarks>
        public static CommonValidator<T> Create<T>(Action<CommonValidator<T>>? configure = null) => new CommonValidator<T>().Configure(configure);
    }
}