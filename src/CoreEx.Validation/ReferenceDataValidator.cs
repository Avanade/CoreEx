// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.RefData;
using System;
using System.Threading.Tasks;

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
            Property(x => x.Id).Mandatory().Custom(ValidateIdAsync);
            Property(x => x.Code).Mandatory().String(ReferenceDataValidation.MaxCodeLength);
            Property(x => x.Text).Mandatory().String(ReferenceDataValidation.MaxTextLength);
            Property(x => x.Description).String(ReferenceDataValidation.MaxDescriptionLength);
            Property(x => x.EndDate).When(x => x.StartDate.HasValue && x.EndDate.HasValue).CompareProperty(CompareOperator.GreaterThanEqual, x => x.StartDate);
        }

        /// <summary>
        /// Perform more complex mandatory check based on the ReferenceData base ID type.
        /// </summary>
        private void ValidateIdAsync(PropertyContext<TEntity, object?> context)
        {
            if (context.Value != null)
            {
                if (context.Value is int iid && iid != 0)
                    return;

                if (context.Value is long lid && lid != 0)
                    return;

                if (context.Value is Guid gid && gid != Guid.Empty)
                    return;
            }

            context.CreateErrorMessage(ValidatorStrings.MandatoryFormat);
        }
    }
}