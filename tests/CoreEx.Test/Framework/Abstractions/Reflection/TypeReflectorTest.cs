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

            var cr = TypeReflector.Create(type.GetProperty(nameof(Test.Name))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.Simple, cr.TypeCode);
            Assert.AreEqual(typeof(string), cr.CollectionItemType);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Age))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.Simple, cr.TypeCode);
            Assert.AreEqual(typeof(int), cr.CollectionItemType);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.Array, cr.TypeCode);
            Assert.AreEqual(typeof(string), cr.CollectionItemType);
            Assert.IsNull(cr.CollectionAddMethod);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.ICollection, cr.TypeCode);
            Assert.AreEqual(typeof(decimal?), cr.CollectionItemType);
            Assert.IsNotNull(cr.CollectionAddMethod);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.IEnumerable, cr.TypeCode);
            Assert.AreEqual(typeof(DateTime), cr.CollectionItemType);
            Assert.IsNull(cr.CollectionAddMethod);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.IDictionary, cr.TypeCode);
            Assert.AreEqual(typeof(Test), cr.CollectionItemType);
            Assert.AreEqual(typeof(string), cr.DictKeyType);
            Assert.AreEqual(typeof(KeyValuePair<string, Test>), cr.DictKeyValuePairType);
            Assert.IsNotNull(cr.CollectionAddMethod);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Parent))!);
            Assert.IsNotNull(cr);
            Assert.AreEqual(TypeReflectorTypeCode.Complex, cr.TypeCode);
            Assert.AreEqual(typeof(Test), cr.CollectionItemType);
        }

        [Test]
        public void GetCollectionItemType()
        {
            Assert.AreEqual(TypeReflectorTypeCode.Simple, TypeReflector.GetCollectionItemType(typeof(string)).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Simple, TypeReflector.GetCollectionItemType(typeof(int)).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Array, TypeReflector.GetCollectionItemType(new string?[] { "blah" }.GetType()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.ICollection, TypeReflector.GetCollectionItemType(new List<decimal?>().GetType()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.IDictionary, TypeReflector.GetCollectionItemType(new Dictionary<string, Test>().GetType()).TypeCode);
            Assert.AreEqual(TypeReflectorTypeCode.Complex, TypeReflector.GetCollectionItemType(new Test().GetType()).TypeCode);
            
            Assert.AreEqual(null, TypeReflector.GetCollectionItemType(typeof(string)).ItemType);
            Assert.AreEqual(null, TypeReflector.GetCollectionItemType(typeof(int)).ItemType);
            Assert.AreEqual(typeof(string), TypeReflector.GetCollectionItemType(new string?[] { "blah" }.GetType()).ItemType);
            Assert.AreEqual(typeof(decimal?), TypeReflector.GetCollectionItemType(new List<decimal?>().GetType()).ItemType);
            Assert.AreEqual(typeof(Test), TypeReflector.GetCollectionItemType(new Dictionary<string, Test>().GetType()).ItemType);
            Assert.AreEqual(null, TypeReflector.GetCollectionItemType(new Test().GetType()).ItemType);
        }

        [Test]
        public void CreateCollectionValue()
        {
            var type = typeof(Test);

            var tr = TypeReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            var v = tr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<string[]>(v);
            Assert.AreEqual(0, ((string[])v!).Length);

            tr = TypeReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            v = tr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(0, ((List<decimal?>)v!).Count);

            tr = TypeReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            v = tr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(0, ((DateTime[])v!).Length);

            tr = TypeReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            v = tr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Dictionary<string, Test>>(v);
            Assert.AreEqual(0, ((Dictionary<string, Test>)v!).Count);
        }

        [Test]
        public void CreateValue2()
        {
            var type = typeof(Test2);

            var cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Amounts))!);
            var v = cr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(0, ((List<decimal?>)v!).Count);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Dates))!);
            v = cr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(0, ((DateTime[])v!).Length);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Dict))!);
            v = cr.CreateCollectionValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Dictionary<string, Test>>(v);
            Assert.AreEqual(0, ((Dictionary<string, Test>)v!).Count);
        }

        [Test]
        public void CreateValue_IEnumerableParam()
        {
            var type = typeof(Test2);

            var cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Amounts))!);
            var v = cr.CreateCollectionValue(new decimal?[] { 0m, null, 4.99m });
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<List<decimal?>>(v);
            Assert.AreEqual(3, ((List<decimal?>)v!).Count);
            Assert.AreEqual(0, ((List<decimal?>)v!)[0]);
            Assert.AreEqual(null, ((List<decimal?>)v!)[1]);
            Assert.AreEqual(4.99m, ((List<decimal?>)v!)[2]);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Dates))!);
            v = cr.CreateCollectionValue(new DateTime[] { new DateTime(1999, 1, 1), new DateTime(2000, 2, 2) });
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<DateTime[]>(v);
            Assert.AreEqual(2, ((DateTime[])v!).Length);
            Assert.AreEqual(new DateTime(1999, 1, 1), ((DateTime[])v!)[0]);
            Assert.AreEqual(new DateTime(2000, 2, 2), ((DateTime[])v!)[1]);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test2.Dict))!);
            var dv = new Dictionary<string, Test> { { "a", new Test() }, { "b", new Test() } };
            v = cr.CreateCollectionValue(dv);
            Assert.IsNotNull(v);
            Assert.AreSame(dv, v);
        }

        [Test]
        public void CreateItemValue()
        {
            var type = typeof(Test);

            var cr = TypeReflector.Create(type.GetProperty(nameof(Test.Name))!);
            var v = cr.CreateCollectionItemValue();
            Assert.AreEqual(null, v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Age))!);
            v = cr.CreateCollectionItemValue();
            Assert.AreEqual(0, v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.NickNames))!);
            v = cr.CreateCollectionItemValue();
            Assert.AreEqual(null, v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Amounts))!);
            v = cr.CreateCollectionItemValue();
            Assert.AreEqual(null, v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Dates))!);
            v = cr.CreateCollectionItemValue();
            Assert.AreEqual(DateTime.MinValue, v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Dict))!);
            v = cr.CreateCollectionItemValue();
            Assert.IsNotNull(v);
            Assert.IsInstanceOf<Test>(v);

            cr = TypeReflector.Create(type.GetProperty(nameof(Test.Parent))!);
            v = cr.CreateCollectionItemValue();
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