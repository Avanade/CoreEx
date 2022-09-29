using FluentValidation;

namespace Company.AppName.Business.Validators;

public class EmployeeVerificationValidator : AbstractValidator<EmployeeVerificationRequest>
{
    public EmployeeVerificationValidator()
    {
        RuleFor(x => x.Name).NotNull().MaximumLength(100);
        RuleFor(x => x.Gender).NotNull().MaximumLength(50); // todo: validate if reference data exists
        RuleFor(x => x.Age).NotNull().GreaterThanOrEqualTo(18).LessThanOrEqualTo(120).WithMessage("Age has to be between 18 and 120");
    }
}