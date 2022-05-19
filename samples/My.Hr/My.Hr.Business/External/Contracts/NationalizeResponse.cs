namespace My.Hr.Business.External.Contracts;

public class NationalizeResponse
{
    public string? Name { get; set; }

    public List<CountryResponse>? Country { get; set; }

    public class CountryResponse
    {
        public string? Country_Id { get; set; }
        public float Probability { get; set; }
    }
}