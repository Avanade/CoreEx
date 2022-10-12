// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Enables a validation context for a property.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public interface IPropertyContext<TEntity, in TProperty> : IPropertyContext where TEntity : class { }
}