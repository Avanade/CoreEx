namespace My.Hr.Business.ServiceContracts;

public class GenderizeResponse
{
    public string Name { get; set; }
    public string Gender { get; set; }
    public float Probability { get; set; }
}