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
            Assert.AreEqual("Id", pe1.Name);
            Assert.AreEqual("id", pe1.JsonName);
            Assert.AreEqual("Id", (string)pe1.Text);
            Assert.IsTrue(pe1.IsJsonSerializable);

            var pe2 = PropertyExpression.Create<Person, string?>(p => p.Name);
            Assert.AreEqual("Name", pe2.Name);
            Assert.AreEqual("name", pe2.JsonName);
            Assert.AreEqual("Fullname", (string)pe2.Text);
            Assert.IsTrue(pe2.IsJsonSerializable);

            var pe3 = PropertyExpression.Create<Person, Gender?>(p => p.Gender);
            Assert.AreEqual("Gender", pe3.Name);
            Assert.AreEqual("gender", pe3.JsonName);
            Assert.AreEqual("Gender", (string)pe3.Text);
            Assert.IsFalse(pe3.IsJsonSerializable);

            var pe4 = PropertyExpression.Create<Person, ChangeLog?>(p => p.ChangeLog);
            Assert.AreEqual("ChangeLog", pe4.Name);
            Assert.AreEqual("changeLog", pe4.JsonName);
            Assert.AreEqual("Change Log", (string)pe4.Text);
            Assert.IsTrue(pe4.IsJsonSerializable);

            var pe5 = PropertyExpression.Create<Person, string?>(p => p.Secret);
            Assert.AreEqual("Secret", pe5.Name);
            Assert.AreEqual(null, pe5.JsonName);
            Assert.AreEqual("Secret", (string)pe5.Text);
            Assert.IsFalse(pe5.IsJsonSerializable);
        }

        [Test]
        public void GetValue()
        {
            var pe1 = PropertyExpression.Create<Person, int>(p => p.Id);
            var pe3 = PropertyExpression.Create<Person, Gender?>(p => p.Gender);

            var p = new Person { Id = 88, Gender = new Gender { Code = "M" } };

            Assert.AreEqual(88, pe1.GetValue(p));
            Assert.AreEqual("M", pe3.GetValue(p)!.Code);
        }
    }
}