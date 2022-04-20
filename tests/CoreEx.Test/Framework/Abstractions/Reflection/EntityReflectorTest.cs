using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.RefData.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Framework.Abstractions.Reflection
{
    [TestFixture]
    public class EntityReflectorTest
    {
        [Test]
        public void GetReflector()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            Assert.NotNull(er.GetProperty("Id"));
            Assert.NotNull(er.GetProperty("Name"));
            Assert.NotNull(er.GetProperty("GenderSid"));
            Assert.NotNull(er.GetProperty("Gender"));
            Assert.NotNull(er.GetProperty("Addresses"));
            Assert.NotNull(er.GetProperty("ChangeLog"));
            Assert.NotNull(er.GetProperty("Secret"));
            Assert.NotNull(er.GetProperty("NickNames"));
            Assert.IsNull(er.GetProperty("Bananas"));

            Assert.NotNull(er.GetJsonProperty("id"));
            Assert.IsNull(er.GetJsonProperty("Id"));
            Assert.NotNull(er.GetJsonProperty("name"));
            Assert.IsNull(er.GetJsonProperty("genderSid"));
            Assert.NotNull(er.GetJsonProperty("gender"));
            Assert.NotNull(er.GetJsonProperty("addresses"));
            Assert.NotNull(er.GetJsonProperty("changeLog"));
            Assert.IsNull(er.GetJsonProperty("secret"));
            Assert.NotNull(er.GetJsonProperty("nickNames"));
            Assert.IsNull(er.GetJsonProperty("bananas"));
        }

        [Test]
        public void GetReflector_PropertyReflector_Id()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Id");
            Assert.AreEqual(null, pr.GetEntityReflector());

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetJsonProperty("gender");
            Assert.NotNull(pr);
            Assert.AreEqual("GenderSid", pr!.Name);

            pr = er.GetProperty("Gender");
            Assert.AreEqual("Gender", pr.GetEntityReflector()!.Type.Name);

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Addresses");
            Assert.IsNotNull(pr.GetEntityReflector());

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("NickNames");
            Assert.IsNotNull(pr.GetEntityReflector());

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Salary");
            Assert.AreEqual(null, pr.GetEntityReflector());

            var p = new Person { Salary = 1m };

            pr.PropertyExpression.SetValue(p, 2m);
            Assert.AreEqual(2m, p.Salary);

            pr.PropertyExpression.SetValue(p, null!);
            Assert.AreEqual(null, p.Salary);
        }

        [Test]
        public void GetReflector_Compare()
        {
            var er = EntityReflector.GetReflector<int[]>(new EntityReflectorArgs());
            Assert.IsTrue(er.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }));
            Assert.IsFalse(er.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 4 }));
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Int()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Id");

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Salary");

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("NickNames");

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
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Addresses");

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
            Assert.AreEqual(TypeReflectorTypeCode.Simple, EntityReflector.GetReflector<string>(new EntityReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Simple, EntityReflector.GetReflector<int>(new EntityReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Array, EntityReflector.GetReflector<string?[]>(new EntityReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.ICollection, EntityReflector.GetReflector<List<decimal?>>(new EntityReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.IDictionary, EntityReflector.GetReflector<Dictionary<string, Person>>(new EntityReflectorArgs()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Complex, EntityReflector.GetReflector<Person>(new EntityReflectorArgs()).TypeCode);

            Assert.AreEqual(null, EntityReflector.GetReflector<string>(new EntityReflectorArgs()).ItemType);
            Assert.AreEqual(null, EntityReflector.GetReflector<int>(new EntityReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(string), EntityReflector.GetReflector<string?[]>(new EntityReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(decimal?), EntityReflector.GetReflector<List<decimal?>>(new EntityReflectorArgs()).ItemType);
            Assert.AreEqual(typeof(Person), EntityReflector.GetReflector<Dictionary<string, Person>>(new EntityReflectorArgs()).ItemType);
            Assert.AreEqual(null, EntityReflector.GetReflector<Person>(new EntityReflectorArgs()).ItemType);
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