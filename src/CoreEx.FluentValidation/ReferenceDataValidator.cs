// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using FluentValidation;
using FluentValidation.Validators;
using System;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// Represents a <see cref="IReferenceData"/> validator.
    /// </summary>
    /// <typeparam name="T">The owning object <see cref="Type"/>.</typeparam>
    /// <typeparam name="TRef">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    public class ReferenceDataValidator<T, TRef> : PropertyValidator<T, string?> where TRef : IReferenceData
    {
        /// <inheritdoc/>
        public override string Name => nameof(ReferenceDataValidator<T, TRef>);

        /// <inheritdoc/>
        public override bool IsValid(ValidationContext<T> context, string? value) => 
            value == null || ReferenceDataOrchestrator.Current.GetByTypeRequired<TRef>().TryGetByCode(value, out var rd) && rd!.IsValid;

        /// <inheritdoc/>
        protected override string GetDefaultMessageTemplate(string errorCode) => "'{PropertyName}' is invalid.";
    }
}