using CoreEx.Results;
using CoreEx.Validation;

namespace CoreEx.Test2.Framework.Validation
{
    [TestFixture]
    public class ValidationExtensionsTest
    {
        [Test]
        public async Task Validation_Success_Other_String()
        {
            string? email = "a@b";
            var r = await Result.Go(email).ValidatesAsync(email, v => v.Email());
            Assert.That(r.IsSuccess, Is.True);
        }

        [Test]
        public async Task Validation_Success_Other_String2()
        {
            string? email = "a@b";
            var r = await ValidateAsync(email);
            Assert.That(r.IsSuccess, Is.True);
        }

        private static async Task<Result<string>> ValidateAsync(string? email2)
        {
            return await Result.Go().ValidatesAsync(email2, v =>
            {
                var v2 = v;
                var v3 = v2.Email();
            });
        }
    }
}