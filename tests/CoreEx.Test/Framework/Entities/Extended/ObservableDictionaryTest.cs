using CoreEx.Entities.Extended;
using NUnit.Framework;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace CoreEx.Test.Framework.Entities.Extended
{
    [TestFixture]
    public class ObservableDictionaryTest
    {
        [Test]
        public void ObserveAdd()
        {
            var od = new ObservableDictionary<string, int>();
            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                Assert.IsNull(e.OldItems);
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.AreEqual("Count", e.PropertyName);
                j++;
            };

            od.Add("a", 88);
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
            Assert.AreEqual(1, j);
        }

        [Test]
        public void ObserveIndexerAdd()
        {
            var od = new ObservableDictionary<string, int>();
            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                Assert.IsNull(e.OldItems);
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.AreEqual("Count", e.PropertyName);
                j++;
            };

            od["a"] = 88;
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
            Assert.AreEqual(1, j);
        }

        [Test]
        public void ObserveIndexerReplace()
        {
            var od = new ObservableDictionary<string, int>
            {
                { "a", 99 }
            };

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.AreEqual(1, e.OldItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.AreEqual("Count", e.PropertyName);
                j++;
            };

            od["a"] = 88;
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
            Assert.AreEqual(1, j);
        }

        [Test]
        public void ObserveRemove()
        {
            var od = new ObservableDictionary<string, int>
            {
                { "a", 99 }
            };

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.AreEqual(1, e.OldItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                Assert.IsNull(e.NewItems);
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.AreEqual("Count", e.PropertyName);
                j++;
            };

            od.Remove("a");
            Assert.AreEqual(1, i);
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.IsFalse(od.TryGetValue("a", out var v));
            Assert.AreEqual(0, v);
            Assert.AreEqual(1, j);
        }

        [Test]
        public void ObserveClear()
        {
            var od = new ObservableDictionary<string, int>
            {
                { "a", 99 },
                { "b", 98 }
            };

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                if (i == 0)
                {
                    Assert.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
                    Assert.AreEqual(2, e.OldItems.Count);
                    Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                    Assert.AreEqual(new KeyValuePair<string, int>("b", 98), e.OldItems[1]);
                    Assert.IsNull(e.NewItems);
                }
                else
                {
                    Assert.AreEqual(NotifyCollectionChangedAction.Reset, e.Action);
                    Assert.IsNull(e.OldItems);
                    Assert.IsNull(e.NewItems);
                }

                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.AreEqual("Count", e.PropertyName);
                j++;
            };

            od.Clear();
            Assert.AreEqual(2, i);
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.IsFalse(od.TryGetValue("a", out var v));
            Assert.AreEqual(0, v);
            Assert.AreEqual(1, j);

            // A subsequent clear should not raise any events.
            i = j = 0;
            od.Clear();
            Assert.AreEqual(0, i);
            Assert.AreEqual(0, j);
        }
    }
}