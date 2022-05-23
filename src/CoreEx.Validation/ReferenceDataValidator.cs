// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;

namespace CoreEx.Validation
{
    /// <summary>
    /// Represents the base <see cref="IReferenceData"/> validator.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    public class ReferenceDataValidator<TEntity> : Validator<TEntity> where TEntity : class, IReferenceData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataValidator{TEntity}"/> class.
        /// </summary>
        public ReferenceDataValidator()
        {
            Property(x => x.Id).Mandatory();
            Property(x => x.Code).Mandatory().String(ReferenceDataValidation.MaxCodeLength);
            Property(x => x.Text).Mandatory().String(ReferenceDataValidation.MaxTextLength);
            Property(x => x.Description).String(ReferenceDataValidation.MaxDescriptionLength);
            Property(x => x.EndDate).When(x => x.StartDate.HasValue && x.EndDate.HasValue).CompareProperty(CompareOperator.GreaterThanEqual, x => x.StartDate);
        }
    }
}