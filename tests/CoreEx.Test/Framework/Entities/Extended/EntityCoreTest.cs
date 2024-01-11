using CoreEx.Entities;
using CoreEx.Entities.Extended;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Entities.Extended
{
    [TestFixture]
    public class EntityCoreTest
    {
        [Test]
        public void SettingAndGetting()
        {
            var ta = new TestA { Id = 88, Code = " A ", Text = " B ", DateOnly = new DateTime(2000, 01, 01, 12, 59, 59), DateTime = new DateTime(2000, 01, 01, 12, 59, 59), Description = "the AB code." };
            Assert.Multiple(() =>
            {
                Assert.That(ta.Id, Is.EqualTo(88));
                Assert.That(ta.Code, Is.EqualTo("a"));
                Assert.That(ta.Text, Is.EqualTo(" B"));
                Assert.That(ta.DateOnly, Is.EqualTo(new DateTime(2000, 01, 01)));
                Assert.That(ta.DateTime, Is.EqualTo(new DateTime(2000, 01, 01, 12, 59, 59, DateTimeKind.Utc)));
                Assert.That(ta.IsChanged, Is.True);
                Assert.That(ta.Description, Is.EqualTo("The AB Code."));
            });

            ta.AcceptChanges();
            Assert.That(ta.IsChanged, Is.False);

            ta.Code = null;
            ta.Text = null;
            ta.DateTime = null;
            ta.Description = null;
            Assert.IsEmpty(ta.Code);
            Assert.Multiple(() =>
            {
                Assert.That(ta.Text, Is.Null);
                Assert.That(ta.DateTime, Is.Null);
                Assert.That(ta.IsChanged, Is.True);
                Assert.That(ta.Description, Is.Null);
            });
        }

        private class TestA : EntityCore
        {
            private long _id;
            private string? _code;
            private string? _text;
            private DateTime _dateOnly;
            private DateTime? _dateTime;
            private string? _desc;

            public long Id
            {
                get { return _id; }
                set { SetValue(ref _id, value); }
            }

            public string? Code
            {
                get { return _code; }
                set { SetValue(ref _code, value, StringTrim.Both, StringTransform.NullToEmpty, StringCase.Lower); }
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

            public string? Description
            {
                get { return _desc; }
                set { SetValue(ref _desc, value, casing: StringCase.Title); }
            }
        }
    }
}