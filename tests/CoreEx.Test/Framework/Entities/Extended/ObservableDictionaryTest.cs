using CoreEx.Entities.Extended;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace CoreEx.Test.Framework.Entities.Extended
{
    [TestFixture]
    public class ObservableDictionaryTest
    {
        private static readonly Func<string, string> _keyModifier = k => k.ToUpperInvariant();

        [Test]
        public void ObserveAdd()
        {
            var od = new ObservableDictionary<string, int> { KeyModifier = _keyModifier };
            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
                    Assert.That(e.NewItems!, Has.Count.EqualTo(1));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(e.NewItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 88)));
                    Assert.That(e.OldItems, Is.Null);
                });
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.That(e.PropertyName, Is.EqualTo("Count"));
                j++;
            };

            od.Add("a", 88);
            Assert.Multiple(() =>
            {
                Assert.That(i, Is.EqualTo(1));
                Assert.That(od["a"], Is.EqualTo(88));
                Assert.That(od.TryGetValue("a", out var v), Is.True);
                Assert.That(v, Is.EqualTo(88));
                Assert.That(j, Is.EqualTo(1));
            });
        }

        [Test]
        public void ObserveIndexerAdd()
        {
            var od = new ObservableDictionary<string, int>() { KeyModifier = _keyModifier };
            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
                    Assert.That(e.NewItems!, Has.Count.EqualTo(1));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(e.NewItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 88)));
                    Assert.That(e.OldItems, Is.Null);
                });
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.That(e.PropertyName, Is.EqualTo("Count"));
                j++;
            };

            od["a"] = 88;
            Assert.Multiple(() =>
            {
                Assert.That(i, Is.EqualTo(1));
                Assert.That(od["a"], Is.EqualTo(88));
                Assert.That(od.TryGetValue("a", out var v), Is.True);
                Assert.That(v, Is.EqualTo(88));
                Assert.That(j, Is.EqualTo(1));
            });
        }

        [Test]
        public void ObserveIndexerReplace()
        {
            var od = new ObservableDictionary<string, int> { KeyModifier = _keyModifier };
            od.Add("a", 99);

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Replace));
                    Assert.That(e.OldItems!, Has.Count.EqualTo(1));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(e.OldItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 99)));
                    Assert.That(e.NewItems!, Has.Count.EqualTo(1));
                });
                Assert.That(e.NewItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 88)));
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.That(e.PropertyName, Is.EqualTo("Count"));
                j++;
            };

            od["a"] = 88;
            Assert.Multiple(() =>
            {
                Assert.That(i, Is.EqualTo(1));
                Assert.That(od["a"], Is.EqualTo(88));
                Assert.That(od.TryGetValue("a", out var v), Is.True);
                Assert.That(v, Is.EqualTo(88));
                Assert.That(j, Is.EqualTo(1));
            });
        }

        [Test]
        public void ObserveRemove()
        {
            var od = new ObservableDictionary<string, int> { KeyModifier = _keyModifier };
            od.Add("a", 99);

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                Assert.Multiple(() =>
                {
                    Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
                    Assert.That(e.OldItems!, Has.Count.EqualTo(1));
                });
                Assert.Multiple(() =>
                {
                    Assert.That(e.OldItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 99)));
                    Assert.That(e.NewItems, Is.Null);
                });
                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.That(e.PropertyName, Is.EqualTo("Count"));
                j++;
            };

            od.Remove("a");
            Assert.That(i, Is.EqualTo(1));
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.Multiple(() =>
            {
                Assert.That(od.TryGetValue("a", out var v), Is.False);
                Assert.That(v, Is.EqualTo(0));
                Assert.That(j, Is.EqualTo(1));
            });
        }

        [Test]
        public void ObserveClear()
        {
            var od = new ObservableDictionary<string, int> { KeyModifier = _keyModifier };
            od.Add("a", 99);
            od.Add("b", 98);

            var i = 0;
            var j = 0;
            od.CollectionChanged += (_, e) =>
            {
                if (i == 0)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Remove));
                        Assert.That(e.OldItems!, Has.Count.EqualTo(2));
                    });
                    Assert.Multiple(() =>
                    {
                        Assert.That(e.OldItems![0], Is.EqualTo(new KeyValuePair<string, int>("A", 99)));
                        Assert.That(e.OldItems![1], Is.EqualTo(new KeyValuePair<string, int>("B", 98)));
                        Assert.That(e.NewItems, Is.Null);
                    });
                }
                else
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(e.Action, Is.EqualTo(NotifyCollectionChangedAction.Reset));
                        Assert.That(e.OldItems, Is.Null);
                        Assert.That(e.NewItems, Is.Null);
                    });
                }

                i++;
            };
            od.PropertyChanged += (_, e) =>
            {
                Assert.That(e.PropertyName, Is.EqualTo("Count"));
                j++;
            };

            od.Clear();
            Assert.That(i, Is.EqualTo(2));
            Assert.Throws<KeyNotFoundException>(() => { var x = od["a"]; });
            Assert.Multiple(() =>
            {
                Assert.That(od.TryGetValue("a", out var v), Is.False);
                Assert.That(v, Is.EqualTo(0));
                Assert.That(j, Is.EqualTo(1));
            });

            // A subsequent clear should not raise any events.
            i = j = 0;
            od.Clear();
            Assert.Multiple(() =>
            {
                Assert.That(i, Is.EqualTo(0));
                Assert.That(j, Is.EqualTo(0));
            });
        }
    }
}