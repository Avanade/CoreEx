using CoreEx.Invokers;
using CoreEx.Results;
using CoreEx.Validation;
using NUnit.Framework;
using System.Threading.Tasks;

namespace CoreEx.Test.Framework.Results
{
    [TestFixture]
    public class ValidationExtensionsTest
    {
        [OneTimeSetUp]
        public void OneTimeSetUp() => CoreEx.Localization.TextProvider.SetTextProvider(new ValidationTextProvider());

        [Test]
        public void Ensure_Success_NonDefaultValue()
        {
            bool success = false;
            var r = Result.Ok(1).Required(v => success = true);
            Assert.That(success, Is.True);
        }

        [Test]
        public void Ensure_Success_DefaultValue()
        {
            bool success = false;
            var r = Result.Ok(0).Required(v => success = true);
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [value: Value is required.]"));
            Assert.That(success, Is.False);
        }

        [Test]
        public void Ensure_Failure()
        {
            bool success = false;
            var r = Result<int>.Fail("bad").Required(v => success = true);
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("bad"));
            Assert.That(success, Is.False);
        }

        [Test]
        public async Task Validation_Success_Entity_ValidationContext_Valid()
        {
            var r = await Result.Ok(new Person { Name = "Tom", Age = 18 }).ValidateAsync(() => _personValidator);
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_Success_Entity_ValidationContext_Invalid()
        {
            var r = await Result.Ok(new Person { Name = "Tom" }).ValidateAsync(_personValidator);
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [Age: Age must be greater than 0.]"));
        }

        [Test]
        public async Task Validation_Failure_Entity_ValidationContext_Invalid()
        {
            var r = await Result<Person>.Fail("bad").ValidateAsync(_personValidator);
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("bad"));
        }

        [Test]
        public async Task Validation_Success_Entity_IValidationResult_Valid()
        {
            var r = await Result.Ok(new Person { Name = "Tom", Age = 18 }).ValidateAsync(_personValidator);
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_Success_Entity_IValidationResult_InValid()
        {
            var r = await Result.Ok(new Person { Name = "Tom" }).ValidateAsync(v => v.Mandatory().Entity(_personValidator));
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [value.Age: Age must be greater than 0.]"));
        }

        [Test]
        public async Task Validation_Failure_Entity_IValidationResult_InValid()
        {
            var r = await Result<Person>.Fail("bad").ValidateAsync(v => v.Mandatory().Entity(_personValidator));
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("bad"));
        }

        [Test]
        public async Task Validation_Simulate_Ensure_And_Validation()
        {
            var value = new Person { Name = "Tom", Age = 18 };
            var id = 88;

            var r = await Result.Go().Manager(this, InvokerArgs.Create).WithAsAsync(r =>
            {
                return Result.Go(value)
                    .Required(v => v.Id = id)
                    .ThenAsync(v => v.Validate().Entity(_personValidator).ValidateAsync());
            });

            Assert.IsTrue(r.IsSuccess);
        }

        private static readonly Validator<Person> _personValidator = Validator.Create<Person>()
            .HasProperty(x => x.Name, p => p.Mandatory())
            .HasProperty(x => x.Age, p => p.CompareValue(CompareOperator.GreaterThan, 0));

        public class Person
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public int Age { get; set; }
        }
    }
}