using CoreEx.Entities;
using CoreEx.Events;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Events
{
    [TestFixture]
    public class EventDataPublisherTest
    {
        [Test]
        public void Create()
        {
            var ep = new NullEventPublisher();

            var ed = ep.CreateEvent(new System.Uri("http://blah"), "sub", "act");
            Assert.IsNotNull(ed);
            Assert.That(ed.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed.Subject, Is.EqualTo("sub"));
            Assert.That(ed.Action, Is.EqualTo("act"));
            Assert.That(ed.Key, Is.Null);

            ed = ep.CreateEvent(new System.Uri("http://blah"), "sub", "act", "x");
            Assert.IsNotNull(ed);
            Assert.That(ed.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed.Subject, Is.EqualTo("sub"));
            Assert.That(ed.Action, Is.EqualTo("act"));
            Assert.That(ed.Key, Is.EqualTo("x"));

            ed = ep.CreateEvent(new System.Uri("http://blah"), "sub", "act", "a", "b");
            Assert.IsNotNull(ed);
            Assert.That(ed.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed.Subject, Is.EqualTo("sub"));
            Assert.That(ed.Action, Is.EqualTo("act"));
            Assert.That(ed.Key, Is.EqualTo("a,b"));

            ed = ep.CreateEvent(new System.Uri("http://blah"), "sub", "act", new CoreEx.Entities.CompositeKey("c", "d"));
            Assert.IsNotNull(ed);
            Assert.That(ed.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed.Subject, Is.EqualTo("sub"));
            Assert.That(ed.Action, Is.EqualTo("act"));
            Assert.That(ed.Key, Is.EqualTo("c,d"));

            var ed2 = ep.CreateEvent("sub", "act");
            Assert.IsNotNull(ed2);
            Assert.That(ed2.Source, Is.Null);
            Assert.That(ed2.Subject, Is.EqualTo("sub"));
            Assert.That(ed2.Action, Is.EqualTo("act"));
            Assert.That(ed2.Key, Is.Null);

            ed2 = ep.CreateEvent("sub", "act", "x");
            Assert.IsNotNull(ed2);
            Assert.That(ed2.Source, Is.Null);
            Assert.That(ed2.Subject, Is.EqualTo("sub"));
            Assert.That(ed2.Action, Is.EqualTo("act"));
            Assert.That(ed2.Key, Is.EqualTo("x"));

            ed2 = ep.CreateEvent("sub", "act", "a", "b");
            Assert.IsNotNull(ed2);
            Assert.That(ed2.Source, Is.Null);
            Assert.That(ed2.Subject, Is.EqualTo("sub"));
            Assert.That(ed2.Action, Is.EqualTo("act"));
            Assert.That(ed2.Key, Is.EqualTo("a,b"));

            ed2 = ep.CreateEvent("sub", "act", new CoreEx.Entities.CompositeKey("c", "d"));
            Assert.IsNotNull(ed2);
            Assert.That(ed2.Source, Is.Null);
            Assert.That(ed2.Subject, Is.EqualTo("sub"));
            Assert.That(ed2.Action, Is.EqualTo("act"));
            Assert.That(ed2.Key, Is.EqualTo("c,d"));
        }

        [Test]
        public void CreateValue()
        {
            var ep = new NullEventPublisher();
            ep.EventDataFormatter.KeySeparatorCharacter = '|';

            var ed1 = ep.CreateValueEvent(new Person1 { Id = 88 }, new System.Uri("http://blah"), "sub", "act");
            Assert.IsNotNull(ed1);
            Assert.That(ed1.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed1.Subject, Is.EqualTo("sub"));
            Assert.That(ed1.Action, Is.EqualTo("act"));
            Assert.That(ed1.Key, Is.EqualTo("88"));

            var ed2 = ep.CreateValueEvent(new Person2 { PrimaryKey = new CompositeKey("a", "b") }, new System.Uri("http://blah"), "sub", "act");
            Assert.IsNotNull(ed2);
            Assert.That(ed2.Source, Is.EqualTo(new System.Uri("http://blah")));
            Assert.That(ed2.Subject, Is.EqualTo("sub"));
            Assert.That(ed2.Action, Is.EqualTo("act"));
            Assert.That(ed2.Key, Is.EqualTo("a|b"));

            var ed3 = ep.CreateValueEvent(new Person1 { Id = 88 }, "sub", "act");
            Assert.IsNotNull(ed3);
            Assert.That(ed3.Source, Is.Null);
            Assert.That(ed3.Subject, Is.EqualTo("sub"));
            Assert.That(ed3.Action, Is.EqualTo("act"));
            Assert.That(ed3.Key, Is.EqualTo("88"));

            var ed4 = ep.CreateValueEvent(new Person2 { PrimaryKey = new CompositeKey("a", "b") }, "sub", "act");
            Assert.IsNotNull(ed4);
            Assert.That(ed3.Source, Is.Null);
            Assert.That(ed4.Subject, Is.EqualTo("sub"));
            Assert.That(ed4.Action, Is.EqualTo("act"));
            Assert.That(ed4.Key, Is.EqualTo("a|b"));
        }

        [Test]
        public void ExtentionMethods()
        {
            var uri = new System.Uri("http://blah");
            var ep = new NullEventPublisher();
            ep.CreateEvent("sub");
            ep.CreateEvent("sub", "act");
            ep.CreateEvent("sub", "act", 88);
            ep.CreateEvent("sub", "act", CompositeKey.Create(88));

            ep.CreateEvent(uri, "sub");
            ep.CreateEvent(uri, "sub", "act");
            ep.CreateEvent(uri, "sub", "act", 88);
            ep.CreateEvent(uri, "sub", "act", CompositeKey.Create(88));

            ep.CreateValueEvent(new Person1 { Id = 88 }, "sub");
            ep.CreateValueEvent(new Person1 { Id = 88 }, "sub", "act");
            ep.CreateValueEvent(new Person1 { Id = 88 }, "sub", "act", 88);
            ep.CreateValueEvent(new Person1 { Id = 88 }, "sub", "act", CompositeKey.Create(88));

            ep.CreateValueEvent(new Person1 { Id = 88 }, uri, "sub");
            ep.CreateValueEvent(new Person1 { Id = 88 }, uri, "sub", "act");
            ep.CreateValueEvent(new Person1 { Id = 88 }, uri, "sub", "act", 88);
            ep.CreateValueEvent(new Person1 { Id = 88 }, uri, "sub", "act", CompositeKey.Create(88));

            ep.PublishEvent("sub");
            ep.PublishEvent("sub", "act");
            ep.PublishEvent("sub", "act", 88);
            ep.PublishEvent("sub", "act", CompositeKey.Create(88));

            ep.PublishEvent(uri, "sub");
            ep.PublishEvent(uri, "sub", "act");
            ep.PublishEvent(uri, "sub", "act", 88);
            ep.PublishEvent(uri, "sub", "act", CompositeKey.Create(88));

            ep.PublishNamedEvent("q", "sub");
            ep.PublishNamedEvent("q", "sub", "act");
            ep.PublishNamedEvent("q", "sub", "act", 88);
            ep.PublishNamedEvent("q", "sub", "act", CompositeKey.Create(88));

            ep.PublishNamedEvent("q", uri, "sub");
            ep.PublishNamedEvent("q", uri, "sub", "act");
            ep.PublishNamedEvent("q", uri, "sub", "act", 88);
            ep.PublishNamedEvent("q", uri, "sub", "act", CompositeKey.Create(88));

            ep.PublishValueEvent(new Person1 { Id = 88 }, "sub");
            ep.PublishValueEvent(new Person1 { Id = 88 }, "sub", "act");
            ep.PublishValueEvent(new Person1 { Id = 88 }, "sub", "act", 88);
            ep.PublishValueEvent(new Person1 { Id = 88 }, "sub", "act", CompositeKey.Create(88));

            ep.PublishValueEvent(new Person1 { Id = 88 }, uri, "sub");
            ep.PublishValueEvent(new Person1 { Id = 88 }, uri, "sub", "act");
            ep.PublishValueEvent(new Person1 { Id = 88 }, uri, "sub", "act", 88);
            ep.PublishValueEvent(new Person1 { Id = 88 }, uri, "sub", "act", CompositeKey.Create(88));

            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, "sub");
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, "sub", "act");
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, "sub", "act", 88);
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, "sub", "act", CompositeKey.Create(88));

            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, uri, "sub");
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, uri, "sub", "act");
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, uri, "sub", "act", 88);
            ep.PublishNamedValueEvent("q", new Person1 { Id = 88 }, uri, "sub", "act", CompositeKey.Create(88));
        }

        public class Person1 : IIdentifier<int>
        {
            public int Id { get; set; }
        }

        public class Person2 : IPrimaryKey
        {
            public CompositeKey PrimaryKey { get; set; }
        }
    }
}