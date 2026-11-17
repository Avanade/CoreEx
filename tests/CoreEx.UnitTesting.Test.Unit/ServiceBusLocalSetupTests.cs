namespace CoreEx.UnitTesting.Test.Unit;

public class ServiceBusLocalSetupTests
{
    [TestCase("Endpoint=sb://remote.example;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;")]
    [TestCase("Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=false;")]
    [TestCase("Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;EntityPath=queue;UseDevelopmentEmulator=true;")]
    public void ResetLocalAzureServiceBusAsync_RejectsUnsafeConnectionWithoutLeakingCredentials(string connectionString)
    {
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await UnitTestExExtensions.ResetLocalAzureServiceBusAsync(connectionString));

        exception.Should().NotBeNull();
        exception!.Message.Should().NotContain("SAS_KEY_VALUE");
    }

    [Test]
    public void ResetLocalAzureServiceBusAsync_RejectsMalformedConnectionWithoutLeakingCredentials()
    {
        var connectionString = "not-a-connection-string;SharedAccessKey=SAS_KEY_VALUE";
        var exception = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await UnitTestExExtensions.ResetLocalAzureServiceBusAsync(connectionString));

        exception.Should().NotBeNull();
        exception!.Message.Should().NotContain("SAS_KEY_VALUE");
    }

    [Test]
    public void ResetLocalAzureServiceBusAsync_RejectsNullConfiguration()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            UnitTestExExtensions.ResetLocalAzureServiceBusAsync((Microsoft.Extensions.Configuration.IConfiguration)null!));

        exception.ParamName.Should().Be("configuration");
    }
}
