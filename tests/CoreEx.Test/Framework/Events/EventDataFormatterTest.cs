using CoreEx.Entities;
using CoreEx.Events;
using CoreEx.TestFunction.Models;
using NUnit.Framework;
using System;
using UnitTestEx.NUnit;

namespace CoreEx.Test.Framework.Events
{
    [TestFixture]
    public class EventDataFormatterTest
    {
        [Test]
        public void PropertySelection()
        {
            var ed = CloudEventSerializerTest.CreateProductEvent1();
            var ed2 = new EventData { Id = ed.Id, Timestamp = ed.Timestamp, Value = ed.Value, CorrelationId = null };
            var ef = new EventDataFormatter { PropertySelection = EventDataProperty.None };
            ef.Format(ed);
            ObjectComparer.Assert(ed2, ed);
        }

        [Test]
        public void TypeCasing()
        {
            var ed = new EventData { Type = "AbCd" };
            var ef = new EventDataFormatter { TypeCasing = CoreEx.Globalization.TextInfoCasing.None };
            ef.Format(ed);
            Assert.AreEqual("AbCd", ed.Type);

            ed = new EventData { Type = "AbCd" };
            ef = new EventDataFormatter { TypeCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.AreEqual("abcd", ed.Type);

            ed = new EventData { Type = "AbCd" };
            ef = new EventDataFormatter { TypeCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.AreEqual("ABCD", ed.Type);
        }

        [Test]
        public void TypeDefaultToValueTypeName()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter { TypeDefaultToValueTypeName = false };
            ef.Format(ed);
            Assert.IsNull(ed.Type);

            ef.TypeDefaultToValueTypeName = true;
            ef.Format(ed);
            Assert.IsNull(ed.Type);

            ed.Value = new Product();
            ef.Format(ed);
            Assert.AreEqual("coreex.testfunction.models.product", ed.Type);

            ed.Type = null;
            ef.TypeSeparatorCharacter = '/';
            ef.Format(ed);
            Assert.AreEqual("coreex/testfunction/models/product", ed.Type);
        }

        [Test]
        public void TypeAppendIdOrPrimaryKey()
        {
            var ed = new EventData { Type = "product", Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { TypeAppendIdOrPrimaryKey = true };
            ef.Format(ed);
            Assert.AreEqual("product.abc", ed.Type);

            ed = new EventData { Type = "product", Value = new BackendProduct { Code = "xyz" } };
            ef.Format(ed);
            Assert.AreEqual("product.xyz", ed.Type);
        }

        [Test]
        public void SubjectCasing()
        {
            var ed = new EventData { Subject = "AbCd" };
            var ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.None };
            ef.Format(ed);
            Assert.AreEqual("AbCd", ed.Subject);

            ed = new EventData { Subject = "AbCd" };
            ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.AreEqual("abcd", ed.Subject);

            ed = new EventData { Subject = "AbCd" };
            ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.AreEqual("ABCD", ed.Subject);
        }

        [Test]
        public void SubjectDefaultToValueSubjectName()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter { SubjectDefaultToValueTypeName = false };
            ef.Format(ed);
            Assert.IsNull(ed.Subject);

            ef.SubjectDefaultToValueTypeName = true;
            ef.Format(ed);
            Assert.IsNull(ed.Subject);

            ed.Value = new Product();
            ef.Format(ed);
            Assert.AreEqual("coreex.testfunction.models.product", ed.Subject);

            ed.Subject = null;
            ef.SubjectSeparatorCharacter = '/';
            ef.Format(ed);
            Assert.AreEqual("coreex/testfunction/models/product", ed.Subject);
        }

        [Test]
        public void SubjectAppendIdOrPrimaryKey()
        {
            var ed = new EventData { Subject = "product", Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { SubjectAppendIdOrPrimaryKey = true };
            ef.Format(ed);
            Assert.AreEqual("product.abc", ed.Subject);

            ed = new EventData { Subject = "product", Value = new BackendProduct { Code = "xyz" } };
            ef.Format(ed);
            Assert.AreEqual("product.xyz", ed.Subject);
        }

        [Test]
        public void ActionCasing()
        {
            var ed = new EventData { Action = "AbCd" };
            var ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.None };
            ef.Format(ed);
            Assert.AreEqual("AbCd", ed.Action);

            ed = new EventData { Action = "AbCd" };
            ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.AreEqual("abcd", ed.Action);

            ed = new EventData { Action = "AbCd" };
            ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.AreEqual("ABCD", ed.Action);
        }

        [Test]
        public void SourceDefault()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter();
            ef.Format(ed);
            Assert.AreEqual(new Uri("null", UriKind.Relative), ed.Source);
        }

        [Test]
        public void ETagDefaultFromValue()
        {
            var ed = new EventData { Value = new Person { ETag = "xxx" } };
            var ef = new EventDataFormatter { ETagDefaultFromValue = true };
            ef.Format(ed);
            Assert.AreEqual("xxx", ed.ETag);
        }

        [Test]
        public void ETagDefaultGenerated()
        {
            var ed = new EventData { Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { ETagDefaultGenerated = true };
            Assert.Throws<InvalidOperationException>(() => ef.Format(ed));

            ef.JsonSerializer = new CoreEx.Text.Json.JsonSerializer();
            ef.Format(ed);
            Assert.AreEqual("0rk/Eu4Si62XCw/qDYxqLh9fhNR/4rrAijmAigS0NDM=", ed.ETag);
        }

        internal class Person : IETag
        {
            public string? Name { get; set; }

            public string? ETag { get; set; }
        }
    }
}