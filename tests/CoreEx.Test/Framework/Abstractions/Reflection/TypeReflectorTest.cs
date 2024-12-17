using CoreEx.Abstractions.Reflection;
using CoreEx.Entities;
using CoreEx.RefData;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
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
            Assert.Multiple(() =>
            {
                Assert.That(tr.GetProperty("Id"), Is.Not.Null);
                Assert.That(tr.GetProperty("Name"), Is.Not.Null);
                Assert.That(tr.GetProperty("GenderSid"), Is.Not.Null);
                Assert.That(tr.GetProperty("Gender"), Is.Not.Null);
                Assert.That(tr.GetProperty("Addresses"), Is.Not.Null);
                Assert.That(tr.GetProperty("ChangeLog"), Is.Not.Null);
                Assert.That(tr.GetProperty("Secret"), Is.Not.Null);
                Assert.That(tr.GetProperty("NickNames"), Is.Not.Null);
                Assert.That(tr.TryGetProperty("Bananas", out var _), Is.False);

                Assert.That(tr.GetJsonProperty("id"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("Id"), Is.Null);
                Assert.That(tr.GetJsonProperty("name"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("genderSid"), Is.Null);
                Assert.That(tr.GetJsonProperty("gender"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("addresses"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("changeLog"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("secret"), Is.Null);
                Assert.That(tr.GetJsonProperty("nickNames"), Is.Not.Null);
                Assert.That(tr.GetJsonProperty("bananas"), Is.Null);
            });
        }

        [Test]
        public void GetReflector_PropertyReflector_Id()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Id");
            Assert.That(pr.GetTypeReflector(), Is.Not.Null);

            var p = new Person { Id = 88 };
            Assert.That(pr.PropertyExpression.GetValue(p), Is.EqualTo(88));

            pr.PropertyExpression.SetValue(p, 99);
            Assert.That(p.Id, Is.EqualTo(99));

            pr.PropertyExpression.SetValue(p, null!);
            Assert.That(p.Id, Is.EqualTo(0));
        }

        [Test]
        public void GetReflector_PropertyReflector_Gender()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetJsonProperty("gender");
            Assert.That(pr, Is.Not.Null);
            Assert.That(pr!.Name, Is.EqualTo("GenderSid"));

            pr = tr.GetProperty("Gender");
            Assert.That(pr.GetTypeReflector()!.Type.Name, Is.EqualTo("Gender"));

            var g = new Gender { Code = "F" };
            var p = new Person { Gender = g };

            var g2 = (Gender)pr.PropertyExpression.GetValue(p)!;
            Assert.That(g2.Code, Is.EqualTo("F"));

            pr.PropertyExpression.SetValue(p, new Gender { Code = "M" });
            Assert.That(p.Gender.Code, Is.EqualTo("M"));

            pr.PropertyExpression.SetValue(p, null!);
            Assert.That(p.Gender, Is.EqualTo(null));
        }

        [Test]
        public void GetReflector_PropertyReflector_Addresses()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Addresses");
            Assert.That(pr.GetTypeReflector(), Is.Not.Null);

            var a = new List<Address> { new() { Street = "s", City = "c" } };
            var p = new Person { Addresses = a };

            var a2 = (List<Address>)pr.PropertyExpression.GetValue(p)!;
            Assert.That(a2[0].Street, Is.EqualTo("s"));

            pr.PropertyExpression.SetValue(p, new List<Address> { new() { Street = "s2", City = "c2" } });
            Assert.That(p.Addresses[0].Street, Is.EqualTo("s2"));

            pr.PropertyExpression.SetValue(p, null!);
            Assert.That(p.Addresses, Is.EqualTo(null));
        }

        [Test]
        public void GetReflector_PropertyReflector_NickNames()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("NickNames");
            Assert.That(pr.GetTypeReflector(), Is.Not.Null);

            var n = new string[] { "baz" };
            var p = new Person { NickNames = n };

            var a2 = (string[])pr.PropertyExpression.GetValue(p)!;
            Assert.That(a2[0], Is.EqualTo("baz"));

            pr.PropertyExpression.SetValue(p, new string[] { "gaz" });
            Assert.That(p.NickNames[0], Is.EqualTo("gaz"));

            pr.PropertyExpression.SetValue(p, null!);
            Assert.That(p.NickNames, Is.EqualTo(null));
        }

        [Test]
        public void GetReflector_PropertyReflector_Salary()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Salary");
            Assert.That(pr.GetTypeReflector(), Is.Not.Null);

            var p = new Person { Salary = 1m };

            pr.PropertyExpression.SetValue(p, 2m);
            Assert.That(p.Salary, Is.EqualTo(2m));

            pr.PropertyExpression.SetValue(p, null!);
            Assert.That(p.Salary, Is.EqualTo(null));
        }

        [Test]
        public void GetReflector_Compare_Int()
        {
            var tr = TypeReflector.GetReflector<int[]>(new TypeReflectorArgs());
            Assert.Multiple(() =>
            {
                Assert.That(tr.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }), Is.True);
                Assert.That(tr.Compare(new int[] { 1, 2, 3 }, new int[] { 1, 2, 4 }), Is.False);
            });
        }

        [Test]
        public void GetReflector_Compare_String()
        {
            var tr = TypeReflector.GetReflector<string[]>(new TypeReflectorArgs());
            Assert.Multiple(() =>
            {
                Assert.That(tr.Compare(["a", "aa"], ["a", "aa"]), Is.True);
                Assert.That(tr.Compare(["a", "aa"], ["b", "bb"]), Is.False);
            });
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Int()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Id");

            Assert.Multiple(() =>
            {
                Assert.That(pr.TypeCode, Is.EqualTo(TypeReflectorTypeCode.Simple));
                Assert.That(pr.Compare(null, null), Is.True);
                Assert.That(pr.Compare(1, null), Is.False);
                Assert.That(pr.Compare(null, 2), Is.False);
                Assert.That(pr.Compare(1, 2), Is.False);
                Assert.That(pr.Compare(1, 1), Is.True);
            });
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Nullable()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Salary");

            Assert.Multiple(() =>
            {
                Assert.That(pr.TypeCode, Is.EqualTo(TypeReflectorTypeCode.Simple));
                Assert.That(pr.Compare(null, null), Is.True);
                Assert.That(pr.Compare(1m, null), Is.False);
                Assert.That(pr.Compare(null, 2m), Is.False);
                Assert.That(pr.Compare(1m, 2m), Is.False);
                Assert.That(pr.Compare(1m, 1m), Is.True);
            });
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Array()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("NickNames");

            Assert.Multiple(() =>
            {
                Assert.That(pr.TypeCode, Is.EqualTo(TypeReflectorTypeCode.Array));
                Assert.That(pr.Compare(null, null), Is.True);
                Assert.That(pr.Compare(new string[] { "a", "b" }, null), Is.False);
                Assert.That(pr.Compare(null, new string[] { "y", "z" }), Is.False);
                Assert.That(pr.Compare(new string[] { "a", "b" }, new string[] { "y", "z" }), Is.False);
                Assert.That(pr.Compare(new string[] { "a", "b" }, new string[] { "a", "b", "c" }), Is.False);
                Assert.That(pr.Compare(new string[] { "a", "b" }, new string[] { "a", "b" }), Is.True);
            });
        }

        [Test]
        public void GetReflector_PropertyReflector_Compare_Collection()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var pr = tr.GetProperty("Addresses");

            Assert.Multiple(() =>
            {
                Assert.That(pr.TypeCode, Is.EqualTo(TypeReflectorTypeCode.ICollection));
                Assert.That(pr.Compare(null, null), Is.True);
                Assert.That(pr.Compare(new List<Address>(), new List<Address>()), Is.True);

                // No equality check for Address, so will all fail.
                Assert.That(pr.Compare(new List<Address> { new() }, new List<Address> { new() }), Is.False);
                Assert.That(pr.Compare(null, new List<Address> { new() }), Is.False);
                Assert.That(pr.Compare(new List<Address> { new() }, null), Is.False);
                Assert.That(pr.Compare(new List<Address> { new() }, new List<Address> { new(), new() }), Is.False);
            });
        }

        [Test]
        public void GetReflector_TypeCode_And_ItemType()
        {
            Assert.Multiple(() =>
            {
                Assert.That(TypeReflector.GetReflector<string>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.Simple));
                Assert.That(TypeReflector.GetReflector<int>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.Simple));
                Assert.That(TypeReflector.GetReflector<string?[]>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.Array));
                Assert.That(TypeReflector.GetReflector<List<decimal?>>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.ICollection));
                Assert.That(TypeReflector.GetReflector<Dictionary<string, Person>>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.IDictionary));
                Assert.That(TypeReflector.GetReflector<Person>(new TypeReflectorArgs()).TypeCode, Is.EqualTo(TypeReflectorTypeCode.Complex));

                Assert.That(TypeReflector.GetReflector<string>(new TypeReflectorArgs()).ItemType, Is.EqualTo(null));
                Assert.That(TypeReflector.GetReflector<int>(new TypeReflectorArgs()).ItemType, Is.EqualTo(null));
                Assert.That(TypeReflector.GetReflector<string?[]>(new TypeReflectorArgs()).ItemType, Is.EqualTo(typeof(string)));
                Assert.That(TypeReflector.GetReflector<List<decimal?>>(new TypeReflectorArgs()).ItemType, Is.EqualTo(typeof(decimal?)));
                Assert.That(TypeReflector.GetReflector<Dictionary<string, Person>>(new TypeReflectorArgs()).ItemType, Is.EqualTo(typeof(Person)));
                Assert.That(TypeReflector.GetReflector<Person>(new TypeReflectorArgs()).ItemType, Is.EqualTo(null));
            });
        }

        [Test]
        public void SetValues()
        {
            var tr = TypeReflector.GetReflector<Person>(new TypeReflectorArgs());
            var p = new Person();
            tr.GetProperty("Id").PropertyExpression.SetValue(p, 88);
            tr.GetProperty("Name").PropertyExpression.SetValue(p, "foo");

            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                _ = new Person
                {
                    Id = 88,
                    Name = "foo"
                };
            }

            sw.Stop();
            System.Console.WriteLine($"Raw 100K validations - elapsed: {sw.Elapsed.TotalMilliseconds}ms (per {sw.Elapsed.TotalMilliseconds / 100000}ms)");

            sw = Stopwatch.StartNew();
            for (int i = 0; i < 100000; i++)
            {
                p = new Person();
                tr.GetProperty("Id").PropertyExpression.SetValue(p, 88);
                tr.GetProperty("Name").PropertyExpression.SetValue(p, "foo");
            }

            sw.Stop();
            System.Console.WriteLine($"Expression 100K validations - elapsed: {sw.Elapsed.TotalMilliseconds}ms (per {sw.Elapsed.TotalMilliseconds / 100000}ms)");
        }
    }

    public abstract class PersonBase
    {
        public abstract int Id { get; set; }
    }

    public class Person : PersonBase
    {
        public override int Id { get; set; }
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