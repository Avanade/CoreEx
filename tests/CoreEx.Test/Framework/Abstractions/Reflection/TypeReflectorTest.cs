using CoreEx.Abstractions.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreEx.Test.Framework.Abstractions.Reflection
{
    [TestFixture]
    public class TypeReflectorTest
    {
        [Test]
        public void Create()
        {
            var type = typeof(Test);

            var cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Name))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.Object, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(string), cr.ItemType);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Age))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.Object, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(int), cr.ItemType);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.Array, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(string), cr.ItemType);
            Assert.IsNull(cr.AddMethod);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.ICollection, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(decimal?), cr.ItemType);
            Assert.IsNotNull(cr.AddMethod);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.IEnumerable, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(DateTime), cr.ItemType);
            Assert.IsNull(cr.AddMethod);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.IDictionary, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(Test), cr.ItemType);
            Assert.AreEqual(typeof(string), cr.DictKeyType);
            Assert.AreEqual(typeof(KeyValuePair<string, Test>), cr.DictKeyValuePairType);
            Assert.IsNotNull(cr.AddMethod);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Parent))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(CollectionReflectorType.Object, cr.ComplexTypeCode);
            Assert.AreEqual(typeof(Test), cr.ItemType);
        }

        [Test]
        public void GetItemType()
        {
            Assert.AreEqual(typeof(string), CollectionReflector.GetItemType(typeof(string)));
            Assert.AreEqual(typeof(int), CollectionReflector.GetItemType(typeof(int)));
            Assert.AreEqual(typeof(string), CollectionReflector.GetItemType(new string?[] { "blah" }.GetType()));
            Assert.AreEqual(typeof(decimal?), CollectionReflector.GetItemType(new List<decimal?>().GetType()));
            Assert.AreEqual(typeof(Test), CollectionReflector.GetItemType(new Dictionary<string, Test>().GetType()));
            Assert.AreEqual(typeof(Test), CollectionReflector.GetItemType(new Test().GetType()));
        }

        [Test]
        public void CreateValue()
        {
            var type = typeof(Test);

            var tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Name))!);
            Assert.AreEqual(null, tr.CreateValue());

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Age))!);
            Assert.AreEqual(0, tr.CreateValue());

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            var v = tr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<string[]>(v);
            Assert.AreEqual(0, ((string[])v!).Length);

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            v = tr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(0, ((List<decimal?>)v!).Count);

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            v = tr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(0, ((DateTime[])v!).Length);

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            v = tr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Dictionary<string, Test>>(v);
            Assert.AreEqual(0, ((Dictionary<string, Test>)v!).Count);

            tr = CollectionReflector.Create(type.GetProperty(nameof(Test.Parent))!);
            Assert.AreEqual(null, tr.CreateValue());
        }

        [Test]
        public void CreateValue2()
        {
            var type = typeof(Test2);

            var cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Amounts))!);
            var v = cr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(0, ((List<decimal?>)v!).Count);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Dates))!);
            v = cr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(0, ((DateTime[])v!).Length);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Dict))!);
            v = cr.CreateValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Dictionary<string, Test>>(v);
            Assert.AreEqual(0, ((Dictionary<string, Test>)v!).Count);
        }

        [Test]
        public void CreateValue_IEnumerableParam()
        {
            var type = typeof(Test2);

            var cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Amounts))!);
            var v = cr.CreateValue(new decimal?[] { 0m, null, 4.99m });
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(3, ((List<decimal?>)v!).Count);
            Assert.AreEqual(0, ((List<decimal?>)v!)[0]);
            Assert.AreEqual(null, ((List<decimal?>)v!)[1]);
            Assert.AreEqual(4.99m, ((List<decimal?>)v!)[2]);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Dates))!);
            v = cr.CreateValue(new DateTime[] { new DateTime(1999, 1, 1), new DateTime(2000, 2, 2) });
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(2, ((DateTime[])v!).Length);
            Assert.AreEqual(new DateTime(1999, 1, 1), ((DateTime[])v!)[0]);
            Assert.AreEqual(new DateTime(2000, 2, 2), ((DateTime[])v!)[1]);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test2.Dict))!);
            var dv = new Dictionary<string, Test> { { "a", new Test() }, { "b", new Test() } };
            v = cr.CreateValue(dv);
            Assert.IsNotNull(v);
            Assert.AreSame(dv, v);
        }

        [Test]
        public void CreateItemValue()
        {
            var type = typeof(Test);

            var cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Name))!);
            var v = cr.CreateItemValue();
            Assert.AreEqual(null, v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Age))!);
            v = cr.CreateItemValue();
            Assert.AreEqual(0, v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            v = cr.CreateItemValue();
            Assert.AreEqual(null, v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            v = cr.CreateItemValue();
            Assert.AreEqual(null, v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            v = cr.CreateItemValue();
            Assert.AreEqual(DateTime.MinValue, v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            v = cr.CreateItemValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Test>(v);

            cr = CollectionReflector.Create(type.GetProperty(nameof(Test.Parent))!);
            v = cr.CreateItemValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Test>(v);
        }

        private class Test
        {
            public string? Name { get; set; }
            public int Age { get; set; }
            public string[]? NickNames { get; set; }
            public List<decimal?>? Amounts { get; set; }
            public IEnumerable<DateTime>? Dates { get; set; }
            public Dictionary<string, Test>? Dict { get; set; }
            public Test? Parent { get; set; }
        }

        private class Test2
        {
            public ICollection<decimal?>? Amounts { get; set; }
            public IEnumerable<DateTime>? Dates { get; set; }
            public IDictionary<string, Test>? Dict { get; set; }
        }
    }
}