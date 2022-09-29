namespace Company.AppName.Business.External.Contracts;

public class EmployeeVerificationRequest
{
    public string? Name { get; set; }
    public int Age { get; set; }
    public string? Gender { get; set; }
}