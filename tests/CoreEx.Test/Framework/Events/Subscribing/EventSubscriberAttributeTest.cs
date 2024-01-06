using CoreEx.Events;
using CoreEx.Events.Subscribing;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Events.Subscribing
{
    [TestFixture]
    public class EventSubscriberAttributeTest
    {
        [Test]
        public void Match_Subject_Only()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute("root.*");
            Assert.IsFalse(esa.IsMatch(edf, Create("root", null, null)));
            Assert.IsTrue(esa.IsMatch(edf, Create("root.a", null, null)));
            Assert.IsFalse(esa.IsMatch(edf, Create("root.a.b", null, null)));
            Assert.IsFalse(esa.IsMatch(edf, Create("other", null, null)));

            esa = new EventSubscriberAttribute("root.*.*");
            Assert.IsFalse(esa.IsMatch(edf, Create("root", null, null)));
            Assert.IsFalse(esa.IsMatch(edf, Create("root.a", null, null)));
            Assert.IsTrue(esa.IsMatch(edf, Create("ROOT.A.B", null, null)));
            Assert.IsFalse(esa.IsMatch(edf, Create("root.a.b.c", null, null)));

            esa = new EventSubscriberAttribute("root.*.**");
            Assert.IsFalse(esa.IsMatch(edf, Create("root", null, null)));
            Assert.IsFalse(esa.IsMatch(edf, Create("root.a", null, null)));
            Assert.IsTrue(esa.IsMatch(edf, Create("root.a.b", null, null)));
            Assert.IsTrue(esa.IsMatch(edf, Create("root.a.b.c", null, null)));
        }

        [Test]
        public void Match_Type_Only()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute() { Type = "root.*" };
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root", null)));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, "root.a", null)));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root.a.b", null)));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "other", null)));

            esa = new EventSubscriberAttribute() { Type = "root.*.*" };
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root", null)));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root.a", null)));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, "ROOT.A.B", null)));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root.a.b.c", null)));

            esa = new EventSubscriberAttribute() { Type = "root.*.**" };
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root", null)));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, "root.a", null)));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, "root.a.b", null)));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, "root.a.b.c", null)));
        }

        [Test]
        public void Match_Actions_Only()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute(null, "a*", "b");
            Assert.IsTrue(esa.IsMatch(edf, Create(null, null, "a")));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, null, "AA")));
            Assert.IsTrue(esa.IsMatch(edf, Create(null, null, "b")));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, null, "BB")));
            Assert.IsFalse(esa.IsMatch(edf, Create(null, null, "c")));
        }

        [Test]
        public void Match_Multi()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute("root.*", "a*", "b");
            Assert.IsFalse(esa.IsMatch(edf, Create("root", null, "aa")));
            Assert.IsTrue(esa.IsMatch(edf, Create("root.a", null, "b")));
            Assert.IsFalse(esa.IsMatch(edf, Create("root.a.b", null, "b")));
        }

        [Test]
        public void Match_CaseSensitive()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute("root.*", "a*", "b");

            Assert.IsTrue(esa.IsMatch(edf, Create("ROOT.A", null, "b")));

            esa.IgnoreCase = false;
            Assert.IsTrue(esa.IsMatch(edf, Create("root.a", null, "b")));
            Assert.IsFalse(esa.IsMatch(edf, Create("ROOT.A", null, "b")));
        }

        [Test]
        public void Match_Source()
        {
            var edf = new EventDataFormatter();

            var esa = new EventSubscriberAttribute(new Uri("*", UriKind.Relative));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("test", UriKind.Relative) }));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("http://test", UriKind.Absolute) }));

            esa = new EventSubscriberAttribute(new Uri("test/*", UriKind.Relative));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("test", UriKind.Relative) }));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("test/abc", UriKind.Relative) }));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("/test/abc", UriKind.Relative) }));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test", UriKind.Absolute) }));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test/abc", UriKind.Absolute) }));

            esa = new EventSubscriberAttribute(new Uri("http://host/test/*", UriKind.Absolute));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("test/abc", UriKind.Relative) }));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test", UriKind.Absolute) }));
            Assert.IsTrue(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test/xyz", UriKind.Absolute) }));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("http://tsoh/test/xyz", UriKind.Absolute) }));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("https://host/test/xyz", UriKind.Absolute) }));
            Assert.IsFalse(esa.IsMatch(edf, new EventData { Source = new Uri("http://host:5050/test/xyz", UriKind.Absolute) }));
        }

        private EventData Create(string? subject, string? type, string? action) => new() { Subject = subject, Type = type, Action = action };
    }
}