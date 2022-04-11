using System.Text.Json.Serialization;

namespace My.Hr.Business.ServiceContracts;

public class EmployeeVerificationResponse
{
    public EmployeeVerificationResponse(EmployeeVerificationRequest request)
    {
        Request = request;
    }

    public int Age { get; set; }

    public string? Gender { get; set; }

    public float GenderProbability { get; set; }

    public List<NationalizeResponse.CountryResponse> Country { get; set; } = new List<NationalizeResponse.CountryResponse>();

    public List<string> VerificationMessages { get; set; } = new List<string>();

    public EmployeeVerificationRequest Request { get; }
}