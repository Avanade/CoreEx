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
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create("root", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("root.a", null, null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create("root.a.b", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("other", null, null)), Is.False);
            });

            esa = new EventSubscriberAttribute("root.*.*");
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create("root", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("root.a", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("ROOT.A.B", null, null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create("root.a.b.c", null, null)), Is.False);
            });

            esa = new EventSubscriberAttribute("root.*.**");
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create("root", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("root.a", null, null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create("root.a.b", null, null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create("root.a.b.c", null, null)), Is.True);
            });
        }

        [Test]
        public void Match_Type_Only()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute() { Type = "root.*" };
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create(null, "root", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a", null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a.b", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "other", null)), Is.False);
            });

            esa = new EventSubscriberAttribute() { Type = "root.*.*" };
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create(null, "root", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "ROOT.A.B", null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a.b.c", null)), Is.False);
            });

            esa = new EventSubscriberAttribute() { Type = "root.*.**" };
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create(null, "root", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a", null)), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a.b", null)), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, "root.a.b.c", null)), Is.True);
            });
        }

        [Test]
        public void Match_Actions_Only()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute(null, "a*", "b");
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create(null, null, "a")), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, null, "AA")), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, null, "b")), Is.True);
                Assert.That(esa.IsMatch(edf, Create(null, null, "BB")), Is.False);
                Assert.That(esa.IsMatch(edf, Create(null, null, "c")), Is.False);
            });
        }

        [Test]
        public void Match_Multi()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute("root.*", "a*", "b");
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create("root", null, "aa")), Is.False);
                Assert.That(esa.IsMatch(edf, Create("root.a", null, "b")), Is.True);
                Assert.That(esa.IsMatch(edf, Create("root.a.b", null, "b")), Is.False);
            });
        }

        [Test]
        public void Match_CaseSensitive()
        {
            var edf = new EventDataFormatter();
            var esa = new EventSubscriberAttribute("root.*", "a*", "b");

            Assert.That(esa.IsMatch(edf, Create("ROOT.A", null, "b")), Is.True);

            esa.IgnoreCase = false;
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, Create("root.a", null, "b")), Is.True);
                Assert.That(esa.IsMatch(edf, Create("ROOT.A", null, "b")), Is.False);
            });
        }

        [Test]
        public void Match_Source()
        {
            var edf = new EventDataFormatter();

            var esa = new EventSubscriberAttribute(new Uri("*", UriKind.Relative));
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("test", UriKind.Relative) }), Is.True);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://test", UriKind.Absolute) }), Is.True);
            });

            esa = new EventSubscriberAttribute(new Uri("test/*", UriKind.Relative));
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("test", UriKind.Relative) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("test/abc", UriKind.Relative) }), Is.True);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("/test/abc", UriKind.Relative) }), Is.True);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test", UriKind.Absolute) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test/abc", UriKind.Absolute) }), Is.True);
            });

            esa = new EventSubscriberAttribute(new Uri("http://host/test/*", UriKind.Absolute));
            Assert.Multiple(() =>
            {
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("test/abc", UriKind.Relative) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test", UriKind.Absolute) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://host/test/xyz", UriKind.Absolute) }), Is.True);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://tsoh/test/xyz", UriKind.Absolute) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("https://host/test/xyz", UriKind.Absolute) }), Is.False);
                Assert.That(esa.IsMatch(edf, new EventData { Source = new Uri("http://host:5050/test/xyz", UriKind.Absolute) }), Is.False);
            });
        }

        private EventData Create(string? subject, string? type, string? action) => new() { Subject = subject, Type = type, Action = action };
    }
}