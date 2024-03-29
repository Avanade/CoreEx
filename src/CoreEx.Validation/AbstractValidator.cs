// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the base entity validator using <see href="https://docs.fluentvalidation.net/en/latest/">FluentValidation</see> syntax.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <remarks>This is a synonym for the <see cref="Validator{TEntity}"/>.</remarks>
    public abstract class AbstractValidator<TEntity> : Validator<TEntity> where TEntity : class { }
}