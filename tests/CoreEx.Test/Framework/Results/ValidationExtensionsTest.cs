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
            var r = Result.Ok(1).Required();
            Assert.That(r.IsSuccess, Is.True);
        }

        [Test]
        public void Ensure_Success_DefaultValue()
        {
            var r = Result.Ok(0).Required();
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [value: Value is required.]"));
        }

        [Test]
        public void Ensure_Failure()
        {
            var r = Result<int>.Fail("bad").Required();
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("bad"));
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
        public async Task Validation_Success_Other_Value()
        {
            var r = await Result.Go().ValidatesAsync(1, v => v.Mandatory().CompareValue(CompareOperator.LessThanEqual, 10));
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_Failure_Other_Value()
        {
            var id = 88;
            var r = await Result.Go().ValidatesAsync(id, v => v.Mandatory().CompareValue(CompareOperator.LessThanEqual, 10));
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [id: Identifier must be less than or equal to 10.]"));
        }

        [Test]
        public async Task Validation_Simulate_Ensure_And_Validation()
        {
            var value = new Person { Name = "Tom", Age = 18 };
            var id = 88;

            var r = await Result.Go().Manager(this, InvokerArgs.Create).WithAsAsync(r =>
            {
                return Result.Go(value)
                    .Required()
                    .Then(v => v.Id = id)
                    .ThenAsync(v => v.Validate().Entity(_personValidator).ValidateAsync());
            });

            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_MultiValidator_Success()
        {
            var value = new Person { Name = "Tom", Age = 18 };
            var r = await Result.Go().ValidateAsync(() => MultiValidator.Create().Add(value.Validate().Mandatory().Entity(_personValidator)));
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_MultiValidator_Failure()
        {
            var value = new Person { Name = "Tom" };
            var r = await Result.Go().ValidateAsync(() => MultiValidator.Create().Add(value.Validate().Mandatory().Entity(_personValidator)));
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [value.Age: Age must be greater than 0.]"));
        }

        [Test]
        public async Task Validation_MultiValidator_Value_Success()
        {
            var value = new Person { Name = "Tom", Age = 18 };
            var r = await Result.Go(value).ValidateAsync(p => MultiValidator.Create().Add(p.Validate().Mandatory().Entity(_personValidator)));
            Assert.IsTrue(r.IsSuccess);
        }

        [Test]
        public async Task Validation_MultiValidator_Value_Failure()
        {
            var value = new Person { Name = "Tom" };
            var r = await Result.Go(value).ValidateAsync(_ => MultiValidator.Create().Add(value.Validate().Mandatory().Entity(_personValidator)));
            Assert.That(r.Error, Is.Not.Null.And.Message.EqualTo("A data validation error occurred. [value.Age: Age must be greater than 0.]"));
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