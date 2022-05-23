// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the base <see cref="IReferenceData"/> validator with a <see cref="Default"/> instance.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="ReferenceDataValidatorBase{TEntity, TValidator}"/> <see cref="Type"/>.</typeparam>
    public abstract class ReferenceDataValidatorBase<TEntity, TSelf> : ReferenceDataValidator<TEntity>
        where TEntity : class, IReferenceData
        where TSelf : ReferenceDataValidatorBase<TEntity, TSelf>, new()
    {
        private static readonly TSelf _default = new();

#pragma warning disable CA1000 // Do not declare static members on generic types; by-design, results in a consistent static defined default instance without the need to specify generic type to consume.
        /// <summary>
        /// Gets the current instance of the validator.
        /// </summary>
        public static TSelf Default
#pragma warning restore CA1000 
        {
            get
            {
                if (_default == null)
                    throw new InvalidOperationException("An instance of this Validator cannot be referenced as it is still being constructed; beware that you may have a circular reference within the constructor.");

                return _default;
            }
        }
    }
}