using CoreEx.Entities;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture]
    public class EntityCoreTest
    {
        [Test]
        public void SettingAndGetting()
        {
            var ta = new TestA { Id = 88, Code = " A ", Text = " B ", DateOnly = new DateTime(2000, 01, 01, 12, 59, 59), DateTime = new DateTime(2000, 01, 01, 12, 59, 59) };
            Assert.AreEqual(88, ta.Id);
            Assert.AreEqual("A", ta.Code);
            Assert.AreEqual(" B", ta.Text);
            Assert.AreEqual(new DateTime(2000, 01, 01), ta.DateOnly);
            Assert.AreEqual(new DateTime(2000, 01, 01, 12, 59, 59, DateTimeKind.Utc), ta.DateTime);
            Assert.IsTrue(ta.IsChanged);

            ta.AcceptChanges();
            Assert.IsFalse(ta.IsChanged);

            ta.Code = null;
            ta.Text = null;
            ta.DateTime = null;
            Assert.IsEmpty(ta.Code);
            Assert.IsNull(ta.Text);
            Assert.IsNull(ta.DateTime);
            Assert.IsTrue(ta.IsChanged);
        }

        private class TestA : EntityCore
        {
            private long _id;
            private string? _code;
            private string? _text;
            private DateTime _dateOnly;
            private DateTime? _dateTime;

            public long Id
            {
                get { return _id; }
                set { SetValue(ref _id, value); }
            }

            public string? Code
            {
                get { return _code; }
                set { SetValue(ref _code, value, StringTrim.Both, StringTransform.NullToEmpty); }
            }

            public string? Text
            {
                get { return _text; }
                set { SetValue(ref _text, value); }
            }

            public DateTime DateOnly
            {
                get { return _dateOnly; }
                set { SetValue(ref _dateOnly, value, DateTimeTransform.DateOnly); }
            }

            public DateTime? DateTime
            {
                get { return _dateTime; }
                set { SetValue(ref _dateTime, value); }
            }

            //public override void CleanUp()
            //{
            //    base.CleanUp();
            //    Id = Cleaner.Clean(Id);
            //    Text = Cleaner.Clean(Text);
            //    DateOnly = Cleaner.Clean(DateOnly);
            //    DateTime = Cleaner.Clean(DateTime);
            //}
        }

        //public void Property_ConcurrentUpdating()
        //{
        //    // EntityBase etc. is not designed to be thread-sage. Not generally supported.
        //    var a = new TestA();
        //    a.TrackChanges();

        //    var ts = new Task[100];

        //    for (int i = 0; i < ts.Length; i++)
        //        ts[i] = CreateValueUpdateTask(a);

        //    for (int i = 0; i < ts.Length; i++)
        //        ts[i].Start();

        //    Task.WaitAll(ts);

        //    Assert.IsNotNull(a.ChangeTracking);
        //    Assert.AreEqual(4, a.ChangeTracking.Count);
        //    Assert.AreEqual("Id", a.ChangeTracking[0]);
        //    Assert.AreEqual("Text", a.ChangeTracking[1]);
        //    Assert.AreEqual("Now", a.ChangeTracking[2]);
        //    Assert.AreEqual("Time", a.ChangeTracking[3]);
        //}

        //private Task CreateValueUpdateTask(TestA a)
        //{
        //    return new Task(() =>
        //    {
        //        var now = Cleaner.Clean(DateTime.Now);
        //        a.Id = now.Ticks;
        //        a.Text = now.ToLongDateString();
        //        a.DateOnly = now;
        //        a.DateTime = now;
        //    });
        //}
    }
}
