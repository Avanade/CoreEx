using CoreEx.Entities.Extended;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

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
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                Assert.IsNull(e.OldItems);
                i++;
            };

            od.Add("a", 88);
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
        }

        [Test]
        public void ObserveIndexerAdd()
        {
            var od = new ObservableDictionary<string, int>();
            var i = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Add, e.Action);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                Assert.IsNull(e.OldItems);
                i++;
            };

            od["a"] = 88;
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
        }

        [Test]
        public void ObserveIndexerReplace()
        {
            var od = new ObservableDictionary<string, int>();
            od.Add("a", 99);

            var i = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Replace, e.Action);
                Assert.AreEqual(1, e.OldItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                Assert.AreEqual(1, e.NewItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 88), e.NewItems[0]);
                i++;
            };

            od["a"] = 88;
            Assert.AreEqual(1, i);
            Assert.AreEqual(88, od["a"]);
            Assert.IsTrue(od.TryGetValue("a", out var v));
            Assert.AreEqual(88, v);
        }

        [Test]
        public void ObserveIndexerRemove()
        {
            var od = new ObservableDictionary<string, int>();
            od.Add("a", 99);

            var i = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
                Assert.AreEqual(1, e.OldItems.Count);
                Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                Assert.IsNull(e.NewItems);
                i++;
            };

            od.Remove("a");
            Assert.AreEqual(1, i);
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.IsFalse(od.TryGetValue("a", out var v));
            Assert.AreEqual(0, v);
        }

        [Test]
        public void ObserveIndexerReset()
        {
            var od = new ObservableDictionary<string, int>();
            od.Add("a", 99);
            od.Add("b", 98);

            var i = 0;
            od.CollectionChanged += (_, e) =>
            {
                if (i == 0)
                {
                    Assert.AreEqual(NotifyCollectionChangedAction.Reset, e.Action);
                    Assert.IsNull(e.OldItems);
                    Assert.IsNull(e.NewItems);
                }
                else
                {
                    Assert.AreEqual(NotifyCollectionChangedAction.Remove, e.Action);
                    Assert.AreEqual(2, e.OldItems.Count);
                    Assert.AreEqual(new KeyValuePair<string, int>("a", 99), e.OldItems[0]);
                    Assert.AreEqual(new KeyValuePair<string, int>("b", 98), e.OldItems[1]);
                    Assert.IsNull(e.NewItems);
                }

                i++;
            };

            od.Clear();
            Assert.AreEqual(2, i);
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.IsFalse(od.TryGetValue("a", out var v));
            Assert.AreEqual(0, v);
        }
    }
}