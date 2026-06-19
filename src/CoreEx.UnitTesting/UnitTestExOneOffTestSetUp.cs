[assembly: UnitTestEx.Abstractions.OneOffTestSetUp(typeof(CoreEx.UnitTesting.UnitTestExOneOffTestSetUp))]

namespace CoreEx.UnitTesting;

/// <summary>
/// One-off test set-up for <b>UnitTestEx</b> to initialize <b>CoreEx</b>-specific capabilities.
/// </summary>
internal class UnitTestExOneOffTestSetUp : UnitTestEx.Abstractions.OneOffTestSetUpBase
{
    /// <inheritdoc/>
    public override void SetUp()
    {
        TestSetUp.Default.DefaultUserName = CoreEx.Security.AuthenticationUser.EnvironmentUser.UserName;
        TestSetUp.Default.JsonSerializer = new UnitTestEx.Json.JsonSerializer(CoreEx.Json.JsonDefaults.SerializerOptions);

        // Extend the AssertErrors functionality to support the ValidationException.
        AssertorBase.AddAssertErrorsExtension((assertor, errors) =>
        {
            if (assertor.Exception is ValidationException vex)
            {
                var actual = vex.Messages?.Where(x => x.Type == Entities.MessageType.Error).Select(x => new ApiError(x.Property, x.Text.ToString() ?? string.Empty)).ToArray() ?? [];
                if (!Assertor.TryAreErrorsMatched(errors, actual, out var errorMessage))
                    assertor.Owner.Implementor.AssertFail(errorMessage);

                return true;
            }

            return false;
        });
    }
}