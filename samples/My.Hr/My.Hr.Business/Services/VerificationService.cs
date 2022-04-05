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
    private readonly IEventPublisher _publisher;

    public VerificationService(AgifyApiClient agifyApiClient, GenderizeApiClient genderizeApiClient, NationalizeApiClient nationalizeApiClient, HrSettings settings, IEventPublisher publisher)
    {
        _agifyApiClient = agifyApiClient;
        _genderizeApiClient = genderizeApiClient;
        _nationalizeApiClient = nationalizeApiClient;
        _settings = settings;
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

    public async Task VerifyAndPublish(EmployeeVerificationRequest request)
    {
        var result = await VerifyAsync(request.Name!);

        var response = new EmployeeVerificationResponse(request)
        {
            Age = result.Item1.Age,
            Gender = result.Item2.Gender,
            GenderProbability = result.Item2.Probability
        };

        response.Country.AddRange(result.Item3.Country!);

        var nationality = response.Country.OrderByDescending(c => c.Probability).First();

        response.VerificationMessages.Add(
            @$"Performed verification for {request.Name}, {request.Gender} age {request.Age}. 
            Engine predicted age was {response.Age}. 
            Engine predicted gender was {response.Gender} with {ToPercents(response.GenderProbability)} probability.
            Most likely nationality of {request.Name} is {nationality.Country_Id} with {ToPercents(nationality.Probability)} probability"
        );

        // first check age
        if (Math.Abs(request.Age - response.Age) >= 10)
        {
            response.VerificationMessages.Add($"Employee age ({request.Age}) is not within range of 10 years of predicted age: {response.Age}");
        }

        if (response.GenderProbability > 0.5 && !response.Gender!.Equals(request.Gender, StringComparison.InvariantCultureIgnoreCase))
        {
            response.VerificationMessages.Add($"Employee gender ({request.Gender}) doesn't match predicted gender: {response.Gender}");
        }

        _publisher.Publish(_settings.VerificationResultsQueueName, new EventData { Value = response });
        await _publisher.SendAsync();
    }

    private static string ToPercents(float value) => (int)(value * 100) + "%";
}