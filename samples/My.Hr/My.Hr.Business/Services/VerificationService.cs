using CoreEx.Events;
using Microsoft.Extensions.Logging;
using My.Hr.Business.ServiceContracts;

namespace My.Hr.Business.Services;

public class VerificationService
{
    private readonly AgifyApiClient _agifyApiClient;
    private readonly GenderizeApiClient _genderizeApiClient;
    private readonly NationalizeApiClient _nationalizeApiClient;
    private readonly HrSettings _settings;
    private readonly ILogger<VerificationService> _logger;
    private readonly IEventPublisher _publisher;

    public VerificationService(AgifyApiClient agifyApiClient, GenderizeApiClient genderizeApiClient, NationalizeApiClient nationalizeApiClient, 
        HrSettings settings, ILogger<VerificationService> logger, IEventPublisher publisher)
    {
        _agifyApiClient = agifyApiClient;
        _genderizeApiClient = genderizeApiClient;
        _nationalizeApiClient = nationalizeApiClient;
        _settings = settings;
        _logger = logger;
        _publisher = publisher;
    }

    public async Task<Tuple<AgifyResponse, GenderizeResponse, NationalizeResponse>> VerifyAsync(string name)
    {
        var agifyTask = _agifyApiClient.GetAgeAsync(name);
        var genderizeTask = _genderizeApiClient.GetGenderAsync(name);
        var nationalizeTask = _nationalizeApiClient.GetNationalityAsync(name);

        await Task.WhenAll(agifyTask, genderizeTask, nationalizeTask);

        return new Tuple<AgifyResponse, GenderizeResponse, NationalizeResponse>(agifyTask.Result, genderizeTask.Result, nationalizeTask.Result);
    }

    public async Task VerifyAndPublish(EmployeeVerificationRequest verificationRequest)
    {
        var result = await VerifyAsync(verificationRequest.Name);

        var response = new EmployeeVerificationResponse
        (
            name: verificationRequest.Name,
            age: result.Item1.Age,
            gender: result.Item2.Gender
        );
        response.Country.AddRange(result.Item3.Country);
        response.GenderProbability = result.Item2.Probability;

        // first check age
        if (Math.Abs(verificationRequest.Age - response.Age) >= 10)
        {
            response.VerificationMessages.Add($"Employee age ({verificationRequest.Age}) is not within range of 10 years of predicted age: {response.Age}");
        }

        if (response.GenderProbability > 50 && !response.Gender.Equals(verificationRequest.Gender, StringComparison.InvariantCultureIgnoreCase))
        {
            response.VerificationMessages.Add($"Employee gender ({verificationRequest.Gender}) doesn't match predicted gender: {response.Gender}");
        }

        await _publisher.SendAsync(_settings.VerificationResultsQueueName, new EventData { Value = response });
    }
}