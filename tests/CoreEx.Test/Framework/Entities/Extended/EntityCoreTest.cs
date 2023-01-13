﻿using CoreEx.Entities;
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
            Assert.AreEqual(88, ta.Id);
            Assert.AreEqual("a", ta.Code);
            Assert.AreEqual(" B", ta.Text);
            Assert.AreEqual(new DateTime(2000, 01, 01), ta.DateOnly);
            Assert.AreEqual(new DateTime(2000, 01, 01, 12, 59, 59, DateTimeKind.Utc), ta.DateTime);
            Assert.IsTrue(ta.IsChanged);
            Assert.AreEqual("The AB Code.", ta.Description);

            ta.AcceptChanges();
            Assert.IsFalse(ta.IsChanged);

            ta.Code = null;
            ta.Text = null;
            ta.DateTime = null;
            ta.Description = null;
            Assert.IsEmpty(ta.Code);
            Assert.IsNull(ta.Text);
            Assert.IsNull(ta.DateTime);
            Assert.IsTrue(ta.IsChanged);
            Assert.IsNull(ta.Description);
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