using FluentValidation;

namespace My.Hr.Business.Validators;

public class EmployeeValidator : AbstractValidator<Employee>
{
    public EmployeeValidator()
    {
        RuleFor(x => x.Email).NotNull().EmailAddress();
        RuleFor(x => x.FirstName).NotNull().MaximumLength(100);
        RuleFor(x => x.LastName).NotNull().MaximumLength(100);
        RuleFor(x => x.Gender).NotNull().ReferenceData().TypeOf<Gender>();
        RuleFor(x => x.Birthday).NotNull().LessThanOrEqualTo(DateTime.UtcNow.AddYears(-18)).WithMessage("Birthday is invalid as the Employee must be at least 18 years of age.");
        RuleFor(x => x.StartDate).NotNull().GreaterThanOrEqualTo(new DateTime(1999, 01, 01, 0, 0, 0, DateTimeKind.Utc)).WithMessage("January 1, 1999");
        RuleFor(x => x.PhoneNo).NotNull().MaximumLength(50);
    }
}