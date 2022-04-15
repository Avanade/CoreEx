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
        public void Compare_Int()
        {
            var pe = PropertyExpression.Create<Person, int>(p => p.Id);
            var pei = (IPropertyExpression)pe;

            Assert.IsTrue(pei.Compare(null, null));
            Assert.IsFalse(pei.Compare(1, null));
            Assert.IsFalse(pei.Compare(null, 2));
            Assert.IsFalse(pei.Compare(1, 2));
            Assert.IsTrue(pei.Compare(1, 1));
        }

        [Test]
        public void Compare_Nullable()
        {
            var pe = PropertyExpression.Create<Person, decimal?>(p => p.Salary);
            var pei = (IPropertyExpression)pe;

            Assert.IsTrue(pei.Compare(null, null));
            Assert.IsFalse(pei.Compare(1m, null));
            Assert.IsFalse(pei.Compare(null, 2m));
            Assert.IsFalse(pei.Compare(1m, 2m));
            Assert.IsTrue(pei.Compare(1m, 1m));
        }

        [Test]
        public void Compare_Array()
        {
            var pe = PropertyExpression.Create<Person, string[]?>(p => p.NickNames);
            var pei = (IPropertyExpression)pe;

            Assert.IsTrue(pei.Compare(null, null));
            Assert.IsFalse(pei.Compare(new string[] { "a", "b" }, null));
            Assert.IsFalse(pei.Compare(null, new string[] { "y", "z" }));
            Assert.IsFalse(pei.Compare(new string[] { "a", "b" }, new string[] { "y", "z" }));
            Assert.IsFalse(pei.Compare(new string[] { "a", "b" }, new string[] { "a", "b", "c" }));
            Assert.IsTrue(pei.Compare(new string[] { "a", "b" }, new string[] { "a", "b" }));
        }

        [Test]
        public void Compare_Collection()
        {
            var pe = PropertyExpression.Create<Person, System.Collections.Generic.List<Address>?>(p => p.Addresses);
            var pei = (IPropertyExpression)pe;

            Assert.IsTrue(pei.Compare(null, null));
            Assert.IsTrue(pei.Compare(new System.Collections.Generic.List<Address>(), new System.Collections.Generic.List<Address>()));

            // No equality check for Address, so will all fail.
            Assert.IsFalse(pei.Compare(new System.Collections.Generic.List<Address> { new Address() }, new System.Collections.Generic.List<Address> { new Address() }));
            Assert.IsFalse(pei.Compare(null, new System.Collections.Generic.List<Address> { new Address() }));
            Assert.IsFalse(pei.Compare(new System.Collections.Generic.List<Address> { new Address() }, null));
            Assert.IsFalse(pei.Compare(new System.Collections.Generic.List<Address> { new Address() }, new System.Collections.Generic.List<Address> { new Address(), new Address() }));
        }
    }
}