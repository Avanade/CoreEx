namespace My.Hr.Business.External.Contracts;

public class GenderizeResponse
{
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public float Probability { get; set; }
}