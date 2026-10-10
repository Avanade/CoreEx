await WireMockConsole.RunAsync(settings =>
{
    // Stubbed request/response logging is verbose under load; raise to LogLevel.Information when diagnosing mock matching.
    settings.RequestResponseLogLevel = LogLevel.None;
    return WireMockServer.Start(settings);
});
