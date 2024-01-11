using CoreEx.Entities;
using CoreEx.Json.Merge;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEx.Test.Framework.Json.Merge
{
    [TestFixture]
    public class JsonMergePatchTest
    {
        public class SubData
        {
            public string? Code { get; set; }
            public string? Text { get; set; }
            public int Count { get; set; }
        }

        public class KeyData : IPrimaryKey
        {
            public string? Code { get; set; }
            public string? Text { get; set; }
            public string? Other { get; set; }
            public CompositeKey PrimaryKey => new(Code);
        }

        public class KeyDataCollection : EntityKeyCollection<KeyData> { }

        public class NonKeyData
        {
            public string? Code { get; set; }

            public string? Text { get; set; }
        }

        public class NonKeyDataCollection : List<NonKeyData> { }

        public class TestData
        {
            public Guid Id { get; set; }
            public string? Name { get; set; }
            [JsonIgnore]
            public string? Ignore { get; set; }
            public bool IsValid { get; set; }
            public DateTime Date { get; set; }
            public int Count { get; set; }
            public decimal Amount { get; set; }
            public SubData? Sub { get; set; }
            public int[]? Values { get; set; }
            public List<SubData>? NoKeys { get; set; }
            public List<KeyData>? Keys { get; set; }
            public KeyDataCollection? KeysColl { get; set; }
            public NonKeyDataCollection? NonKeys { get; set; }
            public Dictionary<string, string>? Dict { get; set; }
            public Dictionary<string, KeyData>? Dict2 { get; set; }
        }

        [Test]
        public void Merge_NullJsonArgument()
        {
            var td = new TestData();
            Assert.Throws<ArgumentNullException>(() => { new JsonMergePatch().Merge(null!, ref td); });
        }

        [Test]
        public void Merge_Malformed()
        {
            var td = new TestData();
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge(BinaryData.FromString("<xml/>"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0."));
        }

        [Test]
        public void Merge_Empty()
        {
            var td = new TestData();
            Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ }"), ref td), Is.False);
        }

        [Test]
        public void Merge_Property_StringValue()
        {
            var td = new TestData { Name = "Fred" };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"name\": \"Barry\" }"), ref td), Is.True);
                Assert.That(td!.Name, Is.EqualTo("Barry"));
            });
        }

        [Test]
        public void Merge_Property_StringValue_DifferentNameCasingSupported()
        {
            var td = new TestData { Name = "Fred" };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"nAmE\": \"Barry\" }"), ref td), Is.True);
                Assert.That(td!.Name, Is.EqualTo("Barry"));
            });
        }

        [Test]
        public void Merge_Property_StringValue_DifferentNameCasingNotSupported()
        {
            var td = new TestData { Name = "Fred" };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { NameComparer = StringComparer.Ordinal }).Merge(BinaryData.FromString("{ \"nAmE\": \"Barry\" }"), ref td), Is.False);
                Assert.That(td!.Name, Is.EqualTo("Fred"));
            });
        }

        [Test]
        public void Merge_Property_StringNull()
        {
            var td = new TestData { Name = "Fred" };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"name\": null }"), ref td), Is.True);
                Assert.That(td!.Name, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_StringNumberValue()
        {
            var td = new TestData { Name = "Fred" };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge(BinaryData.FromString("{ \"name\": 123 }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 13."));
        }

        [Test]
        public void Merge_Property_String_MalformedA()
        {
            var td = new TestData();
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge(BinaryData.FromString("{ \"name\": [ \"Barry\" ] }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 11."));
        }

        [Test]
        public void Merge_PrimitiveTypesA()
        {
            var td = new TestData();
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }"), ref td), Is.True);

                Assert.That(td!.Id, Is.EqualTo(new Guid("13512759-4f50-e911-b35c-bc83850db74d")));
                Assert.That(td.Name, Is.EqualTo("Barry"));
                Assert.That(td.IsValid, Is.True);
                Assert.That(td.Date, Is.EqualTo(new DateTime(2018, 12, 31)));
                Assert.That(td.Count, Is.EqualTo(12));
            });
            Assert.That(td.Amount, Is.EqualTo(132.58m));
        }

        [Test]
        public void Merge_PrimitiveTypes_NonCached_X100()
        {
            for (int i = 0; i < 100; i++)
            {
                var td = new TestData();
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }"), ref td), Is.True);
            }
        }

        [Test]
        public void Merge_PrimitiveTypes_Cached_X100()
        {
            var jom = new JsonMergePatch();
            for (int i = 0; i < 100; i++)
            {
                var td = new TestData();
                Assert.That(jom.Merge(BinaryData.FromString("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }"), ref td), Is.True);
            }
        }

        [Test]
        public void Merge_Property_SubEntityNull()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"sub\": null }"), ref td), Is.True);
                Assert.That(td!.Sub, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_SubEntityNewEmpty()
        {
            var td = new TestData();
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"sub\": { } }"), ref td), Is.True);
                Assert.That(td!.Sub, Is.Not.Null);
                Assert.That(td.Sub!.Code, Is.Null);
                Assert.That(td.Sub.Text, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_SubEntityExistingEmpty()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge<TestData>(BinaryData.FromString("{ \"sub\": { } }"), ref td), Is.False);
                Assert.That(td!.Sub, Is.Not.Null);
                Assert.That(td.Sub!.Code, Is.Null);
                Assert.That(td.Sub.Text, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_SubEntityExistingChanged()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"sub\": { \"code\": \"x\", \"text\": \"xxx\" } }"), ref td), Is.True);
                Assert.That(td!.Sub, Is.Not.Null);
                Assert.That(td.Sub!.Code, Is.EqualTo("x"));
                Assert.That(td.Sub.Text, Is.EqualTo("xxx"));
            });
        }

        [Test]
        public void Merge_Property_ArrayMalformed()
        {
            var td = new TestData();
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge(BinaryData.FromString("{ \"values\": { } }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON value could not be converted to System.Int32[]. Path: $.values | LineNumber: 0 | BytePositionInLine: 13."));
        }

        [Test]
        public void Merge_Property_ArrayNull()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"values\": null }"), ref td), Is.True);
                Assert.That(td!.Values, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_ArrayEmpty()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"values\": [] }"), ref td), Is.True);
                Assert.That(td!.Values, Is.Not.Null);
                Assert.That(td.Values!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_ArrayValues_NoChanges()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"values\": [ 1, 2, 3] }"), ref td), Is.False);
                Assert.That(td!.Values, Is.Not.Null);
                Assert.That(td.Values!, Has.Length.EqualTo(3));
            });
        }

        [Test]
        public void Merge_Property_ArrayValues_Changes()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"values\": [ 3, 2, 1] }"), ref td), Is.True);
                Assert.That(td!.Values, Is.Not.Null);
                Assert.That(td.Values!, Has.Length.EqualTo(3));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Values[0], Is.EqualTo(3));
                Assert.That(td.Values[1], Is.EqualTo(2));
                Assert.That(td.Values[2], Is.EqualTo(1));
            });
        }

        [Test]
        public void Merge_Property_NoKeys_ListNull()
        {
            var td = new TestData { NoKeys = new List<SubData> { new() } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"nokeys\": null }"), ref td), Is.True);
                Assert.That(td!.Values, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_NoKeys_ListEmpty()
        {
            var td = new TestData { NoKeys = new List<SubData> { new() } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"nokeys\": [ ] }"), ref td), Is.True);
                Assert.That(td!.NoKeys, Is.Not.Null);
                Assert.That(td!.NoKeys!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_NoKeys_List()
        {
            var td = new TestData { NoKeys = new List<SubData> { new() } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"nokeys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, { }, null ] }"), ref td), Is.True);
                Assert.That(td!.NoKeys, Is.Not.Null);
                Assert.That(td.NoKeys!, Has.Count.EqualTo(3));
                Assert.That(td.NoKeys![0], Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.NoKeys[0].Code, Is.EqualTo("abc"));
                Assert.That(td.NoKeys[0].Text, Is.EqualTo("xyz"));
                Assert.That(td.NoKeys[1], Is.Not.Null);
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.NoKeys[1].Code, Is.Null);
                Assert.That(td.NoKeys[1].Text, Is.Null);
                Assert.That(td.NoKeys[2], Is.Null);
            });
        }

        [Test]
        public void Merge_Property_Keys_ListNull()
        {
            var td = new TestData { Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"keys\": null }"), ref td), Is.True);
                Assert.That(td!.Keys, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_Keys_ListEmpty()
        {
            var td = new TestData { Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"keys\": [ ] }"), ref td), Is.True);

                Assert.That(td!.Keys, Is.Not.Null);
                Assert.That(td.Keys!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_Keys_Null()
        {
            var td = new TestData { Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"keys\": [ null ] }"), ref td), Is.True);

                Assert.That(td!.Keys, Is.Not.Null);
                Assert.That(td.Keys!, Has.Count.EqualTo(1));
                Assert.That(td.Keys![0], Is.Null);
            });
        }

        [Test]
        public void Merge_Property_Keys_Replace()
        {
            var td = new TestData { Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"keys\": [ { \"code\": \"abc\" }, { \"code\": \"uvw\", \"text\": \"xyz\" } ] }"), ref td), Is.True);

                Assert.That(td!.Keys, Is.Not.Null);
                Assert.That(td.Keys!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Keys[0].Code, Is.EqualTo("abc"));
                Assert.That(td.Keys[0].Text, Is.EqualTo(null));
                Assert.That(td.Keys[1].Code, Is.EqualTo("uvw"));
                Assert.That(td.Keys[1].Text, Is.EqualTo("xyz"));
            });
        }

        [Test]
        public void Merge_Property_Keys_NoChanges()
        {
            // Note, although technically no changes, there is no means to verify without specific equality checking, so is seen as a change.
            var td = new TestData { Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{ \"keys\": [ { \"code\": \"abc\", \"text\": \"def\" } ] }"), ref td), Is.True);

                Assert.That(td!.Keys, Is.Not.Null);
                Assert.That(td.Keys!, Has.Count.EqualTo(1));
            });
            
            Assert.Multiple(() =>
            {
                Assert.That(td.Keys[0].Code, Is.EqualTo("abc"));
                Assert.That(td.Keys[0].Text, Is.EqualTo("def"));
            });
        }

        [Test]
        public void Merge_Property_KeysColl_ListNull()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": null }"), ref td), Is.True);
                Assert.That(td!.Values, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_ListEmpty()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ ] }"), ref td), Is.True);
                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_Null()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "abc", Text = "def" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ null ] }"), ref td), Is.True);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(1));
                Assert.That(td.KeysColl![0], Is.Null);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateNulls()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ null, null ] }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON array must not contain items with duplicate 'IEntityKey' keys. Path: $.keyscoll"));
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals1()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { }, { } ] }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON array must not contain items with duplicate 'IEntityKey' keys. Path: $.keyscoll"));
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals2()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"a\" }, { \"code\": \"a\" } ] }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON array must not contain items with duplicate 'IEntityKey' keys. Path: $.keyscoll"));
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals_Dest()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData(), new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { } ] }"), ref td));
            Assert.That(ex!.Message, Is.EqualTo("The JSON array destination collection must not contain items with duplicate 'IEntityKey' keys prior to merge. Path: $.keyscoll"));
        }

        [Test]
        public void Merge_Property_KeysColl_Null_NoChanges()
        {
            var td = new TestData { };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": null }"), ref td), Is.False);

                Assert.That(td!.KeysColl, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_Empty_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection() };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ ] }"), ref td), Is.False);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_NullItem_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { null! } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ null ] }"), ref td), Is.False);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(1));
                Assert.That(td.KeysColl![0], Is.Null);
            });
        }

        [Test]
        public void Merge_Property_KeysColl_Item_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"a\"  } ] }"), ref td), Is.False);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(1));
                Assert.That(td.KeysColl![0].Code, Is.EqualTo("a"));
            });
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_Changes()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"a\", \"text\": \"zz\" }, { \"code\": \"b\" } ] }"), ref td), Is.True);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(2));
            });
            Assert.Multiple(() =>
            {
                Assert.That(td.KeysColl[0].Code, Is.EqualTo("a"));
                Assert.That(td.KeysColl[0].Text, Is.EqualTo("zz"));
                Assert.That(td.KeysColl[1].Code, Is.EqualTo("b"));
                Assert.That(td.KeysColl[1].Text, Is.EqualTo("bb"));
            });
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_SequenceChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"b\", \"text\": \"yy\" }, { \"code\": \"a\" } ] }"), ref td), Is.True);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.KeysColl[0].Code, Is.EqualTo("b"));
                Assert.That(td.KeysColl[0].Text, Is.EqualTo("yy"));
                Assert.That(td.KeysColl[1].Code, Is.EqualTo("a"));
                Assert.That(td.KeysColl[1].Text, Is.EqualTo("aa"));
            });
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_AllNew()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"y\", \"text\": \"yy\" }, { \"code\": \"z\", \"text\": \"zz\" } ] }"), ref td), Is.True);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(2));
            });
            
            Assert.Multiple(() =>
            {
                Assert.That(td.KeysColl[0].Code, Is.EqualTo("y"));
                Assert.That(td.KeysColl[0].Text, Is.EqualTo("yy"));
                Assert.That(td.KeysColl[1].Code, Is.EqualTo("z"));
                Assert.That(td.KeysColl[1].Text, Is.EqualTo("zz"));
            });
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_Delete()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" }, new KeyData { Code = "c", Text = "cc" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"keyscoll\": [ { \"code\": \"a\" }, { \"code\": \"c\" } ] }"), ref td), Is.True);

                Assert.That(td!.KeysColl, Is.Not.Null);
                Assert.That(td.KeysColl!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.KeysColl[0].Code, Is.EqualTo("a"));
                Assert.That(td.KeysColl[0].Text, Is.EqualTo("aa"));
                Assert.That(td.KeysColl[1].Code, Is.EqualTo("c"));
                Assert.That(td.KeysColl[1].Text, Is.EqualTo("cc"));
            });
        }

        // *** Dictionary<string, string> - DictionaryMergeApproach.Replace

        [Test]
        public void Merge_Property_DictReplace_Null()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{ \"dict\": null }"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_DictReplace_Empty()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{ \"dict\": {} }"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_DictReplace_NullValue()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{ \"dict\": {\"k\":null} }"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo(null));
            });
        }

        [Test]
        public void Merge_Property_DictReplace_DuplicateKeys_IntoNull()
        {
            var td = new TestData();

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo("v2"));
            });
        }

        [Test]
        public void Merge_Property_DictReplace_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict = new Dictionary<string, string>() };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo("v2"));
            });
        }

        [Test]
        public void Merge_Property_DictReplace_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k1\":\"v1\"}}"), ref td), Is.False);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("v"));
                Assert.That(td.Dict["k1"], Is.EqualTo("v1"));
            });
        }

        [Test]
        public void Merge_Property_DictReplace_ReOrder_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict\":{\"k1\":\"v1\",\"k\":\"v\"}}"), ref td), Is.False);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("v"));
                Assert.That(td.Dict["k1"], Is.EqualTo("v1"));
            });
        }

        [Test]
        public void Merge_Property_DictReplace_AddUpdateDelete_Replace()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k2\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("v"));
                Assert.That(td.Dict["k2"], Is.EqualTo("v2"));
            });
        }

        // *** Dictionary<string, string> - DictionaryMergeApproach.Merge

        [Test]
        public void Merge_Property_DictMerge_Null()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"dict\": null }"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Null);
            });
        }

        [Test]
        public void Merge_Property_DictMerge_Empty()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                // Should result in no changes as no property (key) was provided.
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"dict\": {} }"), ref td), Is.False);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo("v"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_NullValue()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.Multiple(() =>
            {
                // A key with a value of null indicates it should be removed.
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{ \"dict\": {\"k\":null} }"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Is.Empty);
            });
        }

        [Test]
        public void Merge_Property_DictMerge_DuplicateKeys_IntoNull()
        {
            var td = new TestData { };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo("v2"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict = new Dictionary<string, string>() };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(1));
                Assert.That(td.Dict!["k"], Is.EqualTo("v2"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict\":{\"k\":\"v\",\"k1\":\"v1\"}}"), ref td), Is.False);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("v"));
                Assert.That(td.Dict["k1"], Is.EqualTo("v1"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_ReOrder_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict\":{\"k1\":\"v1\",\"k\":\"v\"}}"), ref td), Is.False);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("v"));
                Assert.That(td.Dict["k1"], Is.EqualTo("v1"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_AddUpdateDelete()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{\"dict\":{\"k\":\"vx\",\"k2\":\"v2\",\"k1\":null}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("vx"));
                Assert.That(td.Dict["k2"], Is.EqualTo("v2"));
            });
        }

        [Test]
        public void Merge_Property_DictMerge_AddUpdate()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch().Merge(BinaryData.FromString("{\"dict\":{\"k\":\"vx\",\"k2\":\"v2\"}}"), ref td), Is.True);
                Assert.That(td!.Dict, Is.Not.Null);
                Assert.That(td.Dict!, Has.Count.EqualTo(3));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict["k"], Is.EqualTo("vx"));
                Assert.That(td.Dict["k1"], Is.EqualTo("v1"));
                Assert.That(td.Dict["k2"], Is.EqualTo("v2"));
            });
        }

        // ***

        [Test]
        public void Merge_Property_Dict2Replace_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData>() };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"a\":{\"code\": \"bb\",\"text\": \"bbb\"}}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(1));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("bb"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("bbb"));
            });
        }

        [Test]
        public void Merge_Property_Dict2Replace_NoChange()
        {
            // Note, although technically no changes, there is no means to verify without specific equality checking, so is seen as a change.
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"b\":{\"code\": \"bb\",\"text\": \"bbb\"}}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("aa"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("aaa"));
                Assert.That(td.Dict2["b"].Code, Is.EqualTo("bb"));
                Assert.That(td.Dict2["b"].Text, Is.EqualTo("bbb"));
            });
        }

        [Test]
        public void Merge_Property_Dict2Replace_AddUpdateDelete_Replace()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("aaaa"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo(null));
                Assert.That(td.Dict2["c"].Code, Is.EqualTo("cc"));
                Assert.That(td.Dict2["c"].Text, Is.EqualTo("ccc"));
            });
        }

        // ***

        [Test]
        public void Merge_Property_KeyDict2Merge_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData>() };

            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"a\":{\"code\": \"bb\",\"text\": \"bbb\"}}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(1));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("bb"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("bbb"));
            });
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_NoChange()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"b\":{\"code\": \"bb\",\"text\": \"bbb\"}}}"), ref td), Is.False);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("aa"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("aaa"));
                Assert.That(td.Dict2["b"].Code, Is.EqualTo("bb"));
                Assert.That(td.Dict2["b"].Text, Is.EqualTo("bbb"));
            });
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_AddUpdateDelete()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"},\"b\":null}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(2));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("aaaa"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("aaa"));
                Assert.That(td.Dict2["c"].Code, Is.EqualTo("cc"));
                Assert.That(td.Dict2["c"].Text, Is.EqualTo("ccc"));
            });
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_AddUpdate()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.Multiple(() =>
            {
                Assert.That(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}"), ref td), Is.True);
                Assert.That(td!.Dict2, Is.Not.Null);
                Assert.That(td.Dict2!, Has.Count.EqualTo(3));
            });

            Assert.Multiple(() =>
            {
                Assert.That(td.Dict2["a"].Code, Is.EqualTo("aaaa"));
                Assert.That(td.Dict2["a"].Text, Is.EqualTo("aaa"));
                Assert.That(td.Dict2["b"].Code, Is.EqualTo("bb"));
                Assert.That(td.Dict2["b"].Text, Is.EqualTo("bbb"));
                Assert.That(td.Dict2["c"].Code, Is.EqualTo("cc"));
                Assert.That(td.Dict2["c"].Text, Is.EqualTo("ccc"));
            });
        }

        // ***

        [Test]
        public void Merge_XLoadTest_NoCache_1000()
        {
            var text = "{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58, \"dict\": {\"k\":\"v\",\"k1\":\"v1\"}, "
                    + "\"values\": [ 1, 2, 4], \"sub\": { \"code\": \"abc\", \"text\": \"xyz\" }, \"nokeys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, null, { } ], "
                    + "\"keys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, { }, null ],\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}";

            for (int i = 0; i < 1000; i++)
            {
                var td = new TestData { Values = new int[] { 1, 2, 3 }, Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } }, Dict = new Dictionary<string, string>() { { "a", "b" } }, Dict2 = new Dictionary<string, KeyData> { { "x", new KeyData { Code = "xx" } } } };
                new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge, DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(BinaryData.FromString(text), ref td);
            }
        }

        [Test]
        public void Merge_XLoadTest_WithCache_1000()
        {
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { EntityKeyCollectionMergeApproach = EntityKeyCollectionMergeApproach.Merge, DictionaryMergeApproach = DictionaryMergeApproach.Merge });
            var text = "{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58, \"dict\": {\"k\":\"v\",\"k1\":\"v1\"}, "
                    + "\"values\": [ 1, 2, 4], \"sub\": { \"code\": \"abc\", \"text\": \"xyz\" }, \"nokeys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, null, { } ], "
                    + "\"keys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, { }, null ],\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}";

            for (int i = 0; i < 1000; i++)
            {
                var td = new TestData { Values = new int[] { 1, 2, 3 }, Keys = new List<KeyData> { new() { Code = "abc", Text = "def" } }, Dict = new Dictionary<string, string>() { { "a", "b" } }, Dict2 = new Dictionary<string, KeyData> { { "x", new KeyData { Code = "xx" } } } };
                jmp.Merge(BinaryData.FromString(text), ref td);
            }
        }

        // **

        [Test]
        public void Merge_RootSimple_NullString()
        {
            string? s = null;
            var jmp = new JsonMergePatch();
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref s), Is.False);
                Assert.That(s, Is.EqualTo(null));
            });

            s = "x";
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref s), Is.True);
                Assert.That(s, Is.EqualTo(null));
            });

            s = null;
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("\"x\""), ref s), Is.True);
                Assert.That(s, Is.EqualTo("x"));
            });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("\"x\""), ref s), Is.False);
                Assert.That(s, Is.EqualTo("x"));
            });
        }

        [Test]
        public void Merge_RootSimple_NullInt()
        {
            int? i = null;
            var jmp = new JsonMergePatch();
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref i), Is.False);
                Assert.That(i, Is.EqualTo(null));
            });

            i = 88;
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref i), Is.True);
                Assert.That(i, Is.EqualTo(null));
            });

            i = null;
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("88"), ref i), Is.True);
                Assert.That(i, Is.EqualTo(88));
            });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("88"), ref i), Is.False);
                Assert.That(i, Is.EqualTo(88));
            });
        }

        [Test]
        public void Merge_RootSimple_Int()
        {
            int i = 0;
            var jmp = new JsonMergePatch();
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("0"), ref i), Is.False);
                Assert.That(i, Is.EqualTo(0));

                Assert.That(jmp.Merge(BinaryData.FromString("88"), ref i), Is.True);
                Assert.That(i, Is.EqualTo(88));
            });
        }

        [Test]
        public void Merge_RootComplex_Null()
        {
            SubData? sd = null!;
            var jmp = new JsonMergePatch();

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref sd), Is.False);
                Assert.That(sd, Is.Null);
            });

            sd = new SubData { Code = "X" };
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("null"), ref sd), Is.True);
                Assert.That(sd, Is.Null);
            });

            sd = null;
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("{\"code\":\"x\"}"), ref sd), Is.True);
                Assert.That(sd, Is.Not.Null);
            });
            Assert.That(sd!.Code, Is.EqualTo("x"));
        }

        [Test]
        public void Merge_RootArray_Simple_Null()
        {
            var arr = Array.Empty<int>();
            var jmp = new JsonMergePatch();
            jmp.Merge(BinaryData.FromString("null"), ref arr);
            Assert.That(arr, Is.EqualTo(null));

            arr = new int[] { 1, 2 };
            jmp.Merge(BinaryData.FromString("null"), ref arr);
            Assert.That(arr, Is.EqualTo(null));

            int[]? arr2 = null;
            jmp.Merge(BinaryData.FromString("null"), ref arr2);
            Assert.That(arr2, Is.EqualTo(null));

            arr2 = new int[] { 1, 2 };
            jmp.Merge(BinaryData.FromString("null"), ref arr2);
            Assert.That(arr2, Is.EqualTo(null));
        }

        [Test]
        public void Merge_RootArray_Simple()
        {
            var arr = Array.Empty<int>();
            var jmp = new JsonMergePatch();
            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("[1,2,3]"), ref arr), Is.True);
                Assert.That(arr, Is.EqualTo(new int[] { 1, 2, 3 }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("[1,2,3]"), ref arr), Is.False);
                Assert.That(arr, Is.EqualTo(new int[] { 1, 2, 3 }));
            });
        }

        [Test]
        public void Merge_RootArray_Complex()
        {
            var arr = new SubData[] { new() { Code = "a", Text = "aa" }, new() { Code = "b", Text = "bb" } };
            var jmp = new JsonMergePatch();

            Assert.Multiple(() =>
            {
                // No equality checker so will appear as changed - is a replacement.
                Assert.That(jmp.Merge(BinaryData.FromString("[{\"code\":\"a\",\"text\":\"aa\"},{\"code\":\"b\",\"text\":\"bb\"}]"), ref arr), Is.True);
                Assert.That(arr!, Has.Length.EqualTo(2));

                // Replaced.
                Assert.That(jmp.Merge(BinaryData.FromString("[{\"code\":\"c\",\"text\":\"cc\"},{\"code\":\"b\",\"text\":\"bb\"}]"), ref arr), Is.True);
                Assert.That(arr!, Has.Length.EqualTo(2));
                Assert.That(arr![0].Code, Is.EqualTo("c"));
            });
        }

        [Test]
        public void Merge_RootDictionary_Simple()
        {
            var dict = new Dictionary<string, int>();
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":1}"), ref dict), Is.True);
                Assert.That(dict, Is.EqualTo(new Dictionary<string, int> { { "x", 1 } }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":1}"), ref dict), Is.False);
                Assert.That(dict, Is.EqualTo(new Dictionary<string, int> { { "x", 1 } }));
            });

            dict = new Dictionary<string, int>();
            jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":1}"), ref dict), Is.True);
                Assert.That(dict, Is.EqualTo(new Dictionary<string, int> { { "x", 1 } }));
            });

            Assert.Multiple(() =>
            {
                Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":1}"), ref dict), Is.False);
                Assert.That(dict, Is.EqualTo(new Dictionary<string, int> { { "x", 1 } }));
            });
        }

        [Test]
        public void Merge_RootDictionary_Complex()
        {
            var dict = new Dictionary<string, SubData>();
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge });

            Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":{\"code\":\"xx\"},\"y\":{\"code\":\"yy\",\"text\":\"YY\"}}"), ref dict), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict!, Has.Count.EqualTo(2));
                Assert.That(dict!["x"].Code, Is.EqualTo("xx"));
                Assert.That(dict!["x"].Text, Is.EqualTo(null));
                Assert.That(dict!["y"].Code, Is.EqualTo("yy"));
                Assert.That(dict!["y"].Text, Is.EqualTo("YY"));
            });

            Assert.That(jmp.Merge(BinaryData.FromString("{\"y\":{\"code\":\"yyy\"},\"x\":{\"code\":\"xxx\"}}"), ref dict), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict!, Has.Count.EqualTo(2));
                Assert.That(dict!["x"].Code, Is.EqualTo("xxx"));
                Assert.That(dict!["x"].Text, Is.EqualTo(null));
                Assert.That(dict!["y"].Code, Is.EqualTo("yyy"));
                Assert.That(dict!["y"].Text, Is.EqualTo("YY"));
            });

            // --

            dict = new Dictionary<string, SubData>();
            jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace });

            Assert.That(jmp.Merge(BinaryData.FromString("{\"x\":{\"code\":\"xx\"},\"y\":{\"code\":\"yy\",\"text\":\"YY\"}}"), ref dict), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(dict, Is.Not.Null);
                Assert.That(dict!, Has.Count.EqualTo(2));
                Assert.That(dict!["x"].Code, Is.EqualTo("xx"));
                Assert.That(dict!["x"].Text, Is.EqualTo(null));
                Assert.That(dict!["y"].Code, Is.EqualTo("yy"));
                Assert.That(dict!["y"].Text, Is.EqualTo("YY"));
            });

            Assert.That(jmp.Merge(BinaryData.FromString("{\"y\":{\"code\":\"yyy\"},\"x\":{\"code\":\"xxx\"}}"), ref dict), Is.True);
            Assert.Multiple(() =>
            {

                Assert.That(dict, Is.Not.Null);
                Assert.That(dict!, Has.Count.EqualTo(2));
                Assert.That(dict!["x"].Code, Is.EqualTo("xxx"));
                Assert.That(dict!["x"].Text, Is.EqualTo(null));
                Assert.That(dict!["y"].Code, Is.EqualTo("yyy"));
                Assert.That(dict!["y"].Text, Is.EqualTo(null));
            });
        }
    }
}