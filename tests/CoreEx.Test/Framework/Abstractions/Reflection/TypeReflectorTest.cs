using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.RefData;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Framework.Abstractions.Reflection
{
    [TestFixture]
    public class TypeReflectorTest
    {
        [Test]
        public void GetReflector()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            Assert.NotNull(tr.GetProperty("Id"));
            Assert.NotNull(tr.GetProperty("Name"));
            Assert.NotNull(tr.GetProperty("GenderSid"));
            Assert.NotNull(tr.GetProperty("Gender"));
            Assert.NotNull(tr.GetProperty("Addresses"));
            Assert.NotNull(tr.GetProperty("ChangeLog"));
            Assert.NotNull(tr.GetProperty("Secret"));
            Assert.NotNull(tr.GetProperty("NickNames"));
            Assert.IsNull(tr.GetProperty("Bananas"));

            Assert.NotNull(tr.GetJsonProperty("id"));
            Assert.IsNull(tr.GetJsonProperty("Id"));
            Assert.NotNull(tr.GetJsonProperty("name"));
            Assert.IsNull(tr.GetJsonProperty("genderSid"));
            Assert.NotNull(tr.GetJsonProperty("gender"));
            Assert.NotNull(tr.GetJsonProperty("addresses"));
            Assert.NotNull(tr.GetJsonProperty("changeLog"));
            Assert.IsNull(tr.GetJsonProperty("secret"));
            Assert.NotNull(tr.GetJsonProperty("nickNames"));
            Assert.IsNull(tr.GetJsonProperty("bananas"));
        }

        [Test]
        public void GetReflector_PropertyReflector_Id()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Id");
            Assert.IsNotNull(pr.GetTypeReflector());

            var p = new Person { Id = 88 };
            Assert.AreEqual(88, pr.PropertyExpression.GetValue(p));

            pr.PropertyExpression.SetValue(p, 99);
            Assert.AreEqual(99, p.Id);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(0, p.Id);
        }

        [Test]
        public void GetReflector_PropertyReflector_Gender()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetJsonProperty("gender");
            Assert.NotNull(pr);
            Assert.AreEqual("GenderSid", pr!.Name);

            pr = tr.GetProperty("Gender");
            Assert.AreEqual("Gender", pr.GetTypeReflector()!.Type.Name);

            var g = new Gender { Code = "F" };
            var p = new Person { Gender = g };

            var g2 = (Gender)pr.PropertyExpression.GetValue(p)!;
            Assert.AreEqual("F", g2.Code);

            pr.PropertyExpression.SetValue(p, new Gender { Code = "M" });
            Assert.AreEqual("M", p.Gender.Code);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(null, p.Gender);
        }

        [Test]
        public void GetReflector_PropertyReflector_Addresses()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Addresses");
            Assert.IsNotNull(pr.GetTypeReflector());

            var a = new List<Address> { new Address { Street = "s", City = "c" } };
            var p = new Person { Addresses = a };

            var a2 = (List<Address>)pr.PropertyExpression.GetValue(p)!;
            Assert.AreEqual("s", a2[0].Street);

            pr.PropertyExpression.SetValue(p, new List<Address> { new Address { Street = "s2", City = "c2" } });
            Assert.AreEqual("s2", p.Addresses[0].Street);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(null, p.Addresses);
        }

        [Test]
        public void GetReflector_PropertyReflector_NickNames()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("NickNames");
            Assert.IsNotNull(pr.GetTypeReflector());

            var n = new string[] { "baz" };
            var p = new Person { NickNames = n };

            var a2 = (string[])pr.PropertyExpression.GetValue(p)!;
            Assert.AreEqual("baz", a2[0]);

            pr.PropertyExpression.SetValue(p, new string[] { "gaz" });
            Assert.AreEqual("gaz", p.NickNames[0]);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(null, p.NickNames);
        }

        [Test]
        public void GetReflector_PropertyReflector_Salary()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Salary");
            Assert.IsNotNull(pr.GetTypeReflector());

            var p = new Person { Salary = 1m };

            pr.PropertyExpression.SetValue(p, 2m);
            Assert.AreEqual(2m, p.Salary);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(null, p.Salary);
        }

        [Test]
        public void GetReflector_Compare()
        {
            var tr = TypeReflector.GetReflector<int[]>(new TypeReflectorArgs());
            Assert.IsTrue(tr.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }));
            Assert.IsFalse(tr.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 4 }));
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Int()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Id");

            Assert.AreEqual(TypeReflectorTypeCode.Simple, pr.TypeCode);
            Assert.IsTrue(pr.Compare(null, null));
            Assert.IsFalse(pr.Compare(1, null));
            Assert.IsFalse(pr.Compare(null, 2));
            Assert.IsFalse(pr.Compare(1, 2));
            Assert.IsTrue(pr.Compare(1, 1));
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Nullable()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Salary");

            Assert.AreEqual(TypeReflectorTypeCode.Simple, pr.TypeCode);
            Assert.IsTrue(pr.Compare(null, null));
            Assert.IsFalse(pr.Compare(1m, null));
            Assert.IsFalse(pr.Compare(null, 2m));
            Assert.IsFalse(pr.Compare(1m, 2m));
            Assert.IsTrue(pr.Compare(1m, 1m));
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Array()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("NickNames");

            Assert.AreEqual(TypeReflectorTypeCode.Array, pr.TypeCode);
            Assert.IsTrue(pr.Compare(null, null));
            Assert.IsFalse(pr.Compare(new string[] { "a", "b" }, null));
            Assert.IsFalse(pr.Compare(null, new string[] { "y", "z" }));
            Assert.IsFalse(pr.Compare(new string[] { "a", "b" }, new string[] { "y", "z" }));
            Assert.IsFalse(pr.Compare(new string[] { "a", "b" }, new string[] { "a", "b", "c" }));
            Assert.IsTrue(pr.Compare(new string[] { "a", "b" }, new string[] { "a", "b" }));
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Collection()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Addresses");

            Assert.AreEqual(TypeReflectorTypeCode.ICollection, pr.TypeCode);
            Assert.IsTrue(pr.Compare(null, null));
            Assert.IsTrue(pr.Compare(new List<Address>(), new List<Address>()));

            // No equality check for Address, so will all fail.
            Assert.IsFalse(pr.Compare(new List<Address> { new Address() }, new List<Address> { new Address() }));
            Assert.IsFalse(pr.Compare(null, new List<Address> { new Address() }));
            Assert.IsFalse(pr.Compare(new List<Address> { new Address() }, null));
            Assert.IsFalse(pr.Compare(new List<Address> { new Address() }, new List<Address> { new Address(), new Address() }));
        }

        [Test]
        public void GetReflector_TypeCode_And_ItemType()
        {
            Assert.AreEqual(TypeReflectorTypeCode.Simple, TypeReflector.GetReflector<string>(new TypeReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Simple, TypeReflector.GetReflector<int>(new TypeReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Array, TypeReflector.GetReflector<string?[]>(new TypeReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.ICollection, TypeReflector.GetReflector<List<decimal?>>(new TypeReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.IDictionary, TypeReflector.GetReflector<Dictionary<string, Person>>(new TypeReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Complex, TypeReflector.GetReflector<Person>(new TypeReflectorArgs()).TypeCode);

            Assert.AreEqual(null, TypeReflector.GetReflector<string>(new TypeReflectorArgs()).ItemType);
            Assert.AreEqual(null, TypeReflector.GetReflector<int>(new TypeReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(string), TypeReflector.GetReflector<string?[]>(new TypeReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(decimal?), TypeReflector.GetReflector<List<decimal?>>(new TypeReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(Person), TypeReflector.GetReflector<Dictionary<string, Person>>(new TypeReflectorArgs()).ItemType);
            Assert.AreEqual(null, TypeReflector.GetReflector<Person>(new TypeReflectorArgs()).ItemType);
        }
    }

    public class Person
    {
        public int Id { get; set; }
        [Display(Name = "Fullname")]
        public string? Name { get; set; }
        [JsonPropertyName("gender")]
        public string? GenderSid { get; set; }
        [JsonIgnore]
        public Gender? Gender { get; set; }
        public List<Address>? Addresses { get; set; }
        public ChangeLog? ChangeLog { get; set; }
        [JsonIgnore]
        public string? Secret { get; set; }
        public string[]? NickNames { get; set; }
        public decimal? Salary { get; set; }
    }

    public class Address
    {
        public string? Street { get; set; }
        public string? City { get; set; }
    }

    public class Gender : ReferenceDataBase<int> { }
}