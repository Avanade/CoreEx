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
            Assert.IsFalse(pr.IsEntityOrCollection);
            Assert.IsNull(pr.EntityCollectionReflector);
            Assert.AreEqual(null, pr.GetEntityReflector());
            Assert.AreEqual(null, pr.GetItemEntityReflector());

            var p = new Person { Id = 88 };

            Assert.IsFalse(pr.SetValue(p, 88));
            Assert.IsTrue(pr.SetValue(p, 99));
            Assert.AreEqual(99, p.Id);

            Assert.IsTrue(pr.SetValue(p, null!));
            Assert.AreEqual(0, p.Id);

            Assert.IsFalse(pr.SetValue(p, null!));
            Assert.AreEqual(0, p.Id);
        }

        [Test]
        public void GetReflector_PropertyReflector_Gender()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetJsonProperty("gender");
            Assert.NotNull(pr);
            Assert.AreEqual("GenderSid", pr!.PropertyName);

            pr = er.GetProperty("Gender");
            Assert.IsTrue(pr.IsEntityOrCollection);
            Assert.NotNull(pr.EntityCollectionReflector);
            Assert.AreEqual("Gender", pr.GetEntityReflector()!.Type.Name);
            Assert.AreEqual(null, pr.GetItemEntityReflector());

            var g = new Gender { Code = "F" };
            var p = new Person { Gender = g };

            Assert.IsFalse(pr.SetValue(p, g));
            Assert.IsTrue(pr.SetValue(p, new Gender { Code = "M" }));
            Assert.AreEqual("M", p.Gender.Code);

            Assert.IsTrue(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Gender);

            Assert.IsFalse(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Gender);
        }

        [Test]
        public void GetReflector_PropertyReflector_Addresses()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Addresses");
            Assert.IsTrue(pr.IsEntityOrCollection);
            Assert.NotNull(pr.EntityCollectionReflector);
            Assert.AreEqual(null, pr.GetEntityReflector());
            Assert.AreEqual(typeof(Address), pr.GetItemEntityReflector()!.Type);

            var a = new List<Address> { new Address { Street = "s", City = "c" } };
            var p = new Person { Addresses = a };

            Assert.IsFalse(pr.SetValue(p, a));
            Assert.IsTrue(pr.SetValue(p, new List<Address> { new Address { Street = "s2", City = "c2" } }));
            Assert.AreEqual("s2", p.Addresses[0].Street);

            Assert.IsTrue(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Addresses);

            Assert.IsFalse(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Addresses);
        }

        [Test]
        public void GetReflector_PropertyReflector_NickNames()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("NickNames");
            Assert.IsTrue(pr.IsEntityOrCollection);
            Assert.NotNull(pr.EntityCollectionReflector);
            Assert.AreEqual(null, pr.GetEntityReflector());
            Assert.AreEqual(null, pr.GetItemEntityReflector());

            var n = new string[] { "baz" };
            var p = new Person { NickNames = n };

            Assert.IsFalse(pr.SetValue(p, n));
            Assert.IsTrue(pr.SetValue(p, new string[] { "gaz" }));
            Assert.AreEqual("gaz", p.NickNames[0]);

            Assert.IsTrue(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.NickNames);

            Assert.IsFalse(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.NickNames);
        }

        [Test]
        public void GetReflector_PropertyReflector_Salary()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var pr = er.GetProperty("Salary");
            Assert.IsFalse(pr.IsEntityOrCollection);
            Assert.IsNull(pr.EntityCollectionReflector);
            Assert.AreEqual(null, pr.GetEntityReflector());
            Assert.AreEqual(null, pr.GetItemEntityReflector());

            var p = new Person { Salary = 1m };

            Assert.IsFalse(pr.SetValue(p, 1m));
            Assert.IsTrue(pr.SetValue(p, 2m));
            Assert.AreEqual(2m, p.Salary);

            Assert.IsTrue(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Salary);

            Assert.IsFalse(pr.SetValue(p, null!));
            Assert.AreEqual(null, p.Salary);
        }

        [Test]
        public void GetReflector_PropertyReflector_NewValue()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            Assert.AreEqual(0, er.GetProperty("Id").NewValue());
            Assert.AreEqual(null, er.GetProperty("Name").NewValue());
            Assert.NotNull(er.GetProperty("Gender").NewValue());
            Assert.NotNull(er.GetProperty("Addresses").NewValue());
            Assert.NotNull(er.GetProperty("ChangeLog").NewValue());
            Assert.NotNull(er.GetProperty("NickNames").NewValue());
            Assert.IsNull(er.GetProperty("Salary").NewValue());
        }

        [Test]
        public void GetReflector_PropertyReflector_NewValueSet()
        {
            var er = EntityReflector.GetReflector<Person>(new EntityReflectorArgs());
            var p = new Person { Id = 88, Name = "Ng", Gender = new Gender { Code = "F" }, Addresses = new List<Address> { new Address { City = "a" } }, NickNames = new string[] { "johnno" }, Salary = 1.5m };

            Assert.IsTrue(er.GetProperty("Id").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("Name").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("Gender").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("Addresses").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("ChangeLog").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("NickNames").NewValue(p).changed);
            Assert.IsTrue(er.GetProperty("Salary").NewValue(p).changed);

            Assert.AreEqual(0, p.Id);
            Assert.AreEqual(null, p.Name);
            Assert.AreEqual(null, p.Gender.Code);
            Assert.AreEqual(0, p.Addresses.Count);
            Assert.IsNotNull(p.ChangeLog);
            Assert.AreEqual(0, p.NickNames.Length);
            Assert.AreEqual(null, p.Salary);
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