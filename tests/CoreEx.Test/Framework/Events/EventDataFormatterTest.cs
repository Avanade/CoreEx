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
            Assert.That(ed.Type, Is.EqualTo("AbCd"));

            ed = new EventData { Type = "AbCd" };
            ef = new EventDataFormatter { TypeCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("abcd"));

            ed = new EventData { Type = "AbCd" };
            ef = new EventDataFormatter { TypeCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("ABCD"));
        }

        [Test]
        public void TypeDefaultToValueTypeName()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter { TypeDefaultToValueTypeName = false };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo(null));

            ed = new EventData();
            ef.TypeDefaultToValueTypeName = true;
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("none"));

            ed = new EventData { Value = new Product() };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("coreex.testfunction.models.product"));

            ed = new EventData { Value = new Product() };
            ef.TypeSeparatorCharacter = '/';
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("coreex/testfunction/models/product"));
        }

        [Test]
        public void TypeAppendIdOrPrimaryKey()
        {
            var ed = new EventData { Type = "product", Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { TypeAppendEntityKey = true };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("product.abc"));

            ed = new EventData { Type = "product", Value = new BackendProduct { Code = "xyz" } };
            ef.Format(ed);
            Assert.That(ed.Type, Is.EqualTo("product.xyz"));
        }

        [Test]
        public void SubjectCasing()
        {
            var ed = new EventData { Subject = "AbCd" };
            var ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.None };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("AbCd"));

            ed = new EventData { Subject = "AbCd" };
            ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("abcd"));

            ed = new EventData { Subject = "AbCd" };
            ef = new EventDataFormatter { SubjectCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("ABCD"));
        }

        [Test]
        public void SubjectDefaultToValueSubjectName()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter { SubjectDefaultToValueTypeName = false };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.Null);

            ef.SubjectDefaultToValueTypeName = true;
            ef.Format(ed);
            Assert.That(ed.Subject, Is.Null);

            ed.Value = new Product();
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("coreex.testfunction.models.product"));

            ed.Subject = null;
            ef.SubjectSeparatorCharacter = '/';
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("coreex/testfunction/models/product"));
        }

        [Test]
        public void SubjectAppendIdOrPrimaryKey()
        {
            var ed = new EventData { Subject = "product", Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { SubjectAppendEntityKey = true };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("product.abc"));

            ed = new EventData { Subject = "product", Value = new BackendProduct { Code = "xyz" } };
            ef.Format(ed);
            Assert.That(ed.Subject, Is.EqualTo("product.xyz"));
        }

        [Test]
        public void ActionCasing()
        {
            var ed = new EventData { Action = "AbCd" };
            var ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.None };
            ef.Format(ed);
            Assert.That(ed.Action, Is.EqualTo("AbCd"));

            ed = new EventData { Action = "AbCd" };
            ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.Lower };
            ef.Format(ed);
            Assert.That(ed.Action, Is.EqualTo("abcd"));

            ed = new EventData { Action = "AbCd" };
            ef = new EventDataFormatter { ActionCasing = CoreEx.Globalization.TextInfoCasing.Upper };
            ef.Format(ed);
            Assert.That(ed.Action, Is.EqualTo("ABCD"));
        }

        [Test]
        public void SourceDefault()
        {
            var ed = new EventData();
            var ef = new EventDataFormatter { SourceDefault = _ => new Uri("null", UriKind.Relative) };
            ef.Format(ed);
            Assert.That(ed.Source, Is.EqualTo(new Uri("null", UriKind.Relative)));
        }

        [Test]
        public void ETagDefaultFromValue()
        {
            var ed = new EventData { Value = new Person { ETag = "xxx" } };
            var ef = new EventDataFormatter { ETagDefaultFromValue = true };
            ef.Format(ed);
            Assert.That(ed.ETag, Is.EqualTo("xxx"));
        }

        [Test]
        public void ETagDefaultGenerated()
        {
            var ed = new EventData { Value = new Product { Id = "abc" } };
            var ef = new EventDataFormatter { ETagDefaultGenerated = true };
            Assert.Throws<InvalidOperationException>(() => ef.Format(ed));

            ef.JsonSerializer = new CoreEx.Text.Json.JsonSerializer();
            ef.Format(ed);
            Assert.That(ed.ETag, Is.EqualTo("0rk/Eu4Si62XCw/qDYxqLh9fhNR/4rrAijmAigS0NDM="));
        }

        internal class Person : IETag
        {
            public string? Name { get; set; }

            public string? ETag { get; set; }
        }
    }
}