using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Abstractions.Reflection
{
    [TestFixture]
    public class PropertyExpressionTest
    {
        [Test]
        public void Create()
        {
            var pe1 = PropertyExpression.Create<Person, int>(p => p.Id);
            Assert.Multiple(() =>
            {
                Assert.That(pe1.Name, Is.EqualTo("Id"));
                Assert.That(pe1.JsonName, Is.EqualTo("id"));
                Assert.That((string)pe1.Text, Is.EqualTo("Identifier"));
                Assert.That(pe1.IsJsonSerializable, Is.True);
            });

            var pe2 = PropertyExpression.Create<Person, string?>(p => p.Name);
            Assert.Multiple(() =>
            {
                Assert.That(pe2.Name, Is.EqualTo("Name"));
                Assert.That(pe2.JsonName, Is.EqualTo("name"));
                Assert.That((string)pe2.Text, Is.EqualTo("Fullname"));
                Assert.That(pe2.IsJsonSerializable, Is.True);
            });

            var pe3 = PropertyExpression.Create<Person, Gender?>(p => p.Gender);
            Assert.Multiple(() =>
            {
                Assert.That(pe3.Name, Is.EqualTo("Gender"));
                Assert.That(pe3.JsonName, Is.EqualTo("gender"));
                Assert.That((string)pe3.Text, Is.EqualTo("Gender"));
                Assert.That(pe3.IsJsonSerializable, Is.False);
            });

            var pe4 = PropertyExpression.Create<Person, ChangeLog?>(p => p.ChangeLog);
            Assert.Multiple(() =>
            {
                Assert.That(pe4.Name, Is.EqualTo("ChangeLog"));
                Assert.That(pe4.JsonName, Is.EqualTo("changeLog"));
                Assert.That((string)pe4.Text, Is.EqualTo("Change Log"));
                Assert.That(pe4.IsJsonSerializable, Is.True);
            });

            var pe5 = PropertyExpression.Create<Person, string?>(p => p.Secret);
            Assert.Multiple(() =>
            {
                Assert.That(pe5.Name, Is.EqualTo("Secret"));
                Assert.That(pe5.JsonName, Is.EqualTo(null));
                Assert.That((string)pe5.Text, Is.EqualTo("Secret"));
                Assert.That(pe5.IsJsonSerializable, Is.False);
            });
        }

        [Test]
        public void ToSentenceCase()
        {
            Assert.Multiple(() =>
            {
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase(null), Is.Null);
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase(string.Empty), Is.EqualTo(string.Empty));
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase("Id"), Is.EqualTo("Identifier"));
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase("id"), Is.EqualTo("Identifier"));
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase("FirstName"), Is.EqualTo("First Name"));
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase("firstName"), Is.EqualTo("First Name"));
                Assert.That(CoreEx.Text.SentenceCase.ToSentenceCase("EmployeeId"), Is.EqualTo("Employee"));
            });

            var w = CoreEx.Text.SentenceCase.SplitIntoWords("FirstXMLCode");
            Assert.Multiple(() =>
            {
                Assert.That(w, Has.Length.EqualTo(3));
                Assert.That(w[0], Is.EqualTo("First"));
                Assert.That(w[1], Is.EqualTo("XML"));
                Assert.That(w[2], Is.EqualTo("Code"));
            });
        }
    }
}