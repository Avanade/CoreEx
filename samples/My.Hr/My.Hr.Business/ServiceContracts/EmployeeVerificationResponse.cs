namespace My.Hr.Business.ServiceContracts;

public class EmployeeVerificationResponse
{
    public EmployeeVerificationResponse(string name, int age, string gender)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Gender = gender ?? throw new ArgumentNullException(nameof(name));
        Age = age;
    }

    public string Name { get; private set; }
    
    public int Age { get; private set; }

    public string Gender { get; private set; }

    public float GenderProbability { get; set; }

    public List<NationalizeResponse.CountryResponse> Country { get; private set; } = new List<NationalizeResponse.CountryResponse>();

    public List<string> VerificationMessages { get; private set; } = new List<string>();
}