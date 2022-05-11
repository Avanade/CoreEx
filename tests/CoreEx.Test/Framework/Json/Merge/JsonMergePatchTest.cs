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

            public string[] PrimaryKeyProperties => new string[] { "Code" };

            public CompositeKey PrimaryKey => new CompositeKey(Code);
        }

        public class KeyDataCollection : PrimaryKeyCollection<KeyData> { }

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
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge("<xml/>", ref td));
            Assert.AreEqual(ex!.Message, "'<' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.");
        }

        [Test]
        public void Merge_Empty()
        {
            var td = new TestData();
            Assert.IsFalse(new JsonMergePatch().Merge("{ }", ref td));
        }

        [Test]
        public void Merge_Property_StringValue()
        {
            var td = new TestData { Name = "Fred" };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"name\": \"Barry\" }", ref td));
            Assert.AreEqual("Barry", td!.Name);
        }

        [Test]
        public void Merge_Property_StringValue_DifferentNameCasingSupported()
        {
            var td = new TestData { Name = "Fred" };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"nAmE\": \"Barry\" }", ref td));
            Assert.AreEqual("Barry", td!.Name);
        }

        [Test]
        public void Merge_Property_StringValue_DifferentNameCasingNotSupported()
        {
            var td = new TestData { Name = "Fred" };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { NameComparer = StringComparer.Ordinal }).Merge("{ \"nAmE\": \"Barry\" }", ref td));
            Assert.AreEqual("Fred", td!.Name);
        }

        [Test]
        public void Merge_Property_StringNull()
        {
            var td = new TestData { Name = "Fred" };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"name\": null }", ref td));
            Assert.IsNull(td!.Name);
        }

        [Test]
        public void Merge_Property_StringNumberValue()
        {
            var td = new TestData { Name = "Fred" };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge("{ \"name\": 123 }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 13.");
        }

        [Test]
        public void Merge_Property_String_MalformedA()
        {
            var td = new TestData();
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge("{ \"name\": [ \"Barry\" ] }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON value could not be converted to System.String. Path: $.name | LineNumber: 0 | BytePositionInLine: 11.");
        }

        [Test]
        public void Merge_PrimitiveTypesA()
        {
            var td = new TestData();
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }", ref td));

            Assert.AreEqual(new Guid("13512759-4f50-e911-b35c-bc83850db74d"), td!.Id);
            Assert.AreEqual("Barry", td.Name);
            Assert.IsTrue(td.IsValid);
            Assert.AreEqual(new DateTime(2018, 12, 31), td.Date);
            Assert.AreEqual(12, td.Count);
            Assert.AreEqual(132.58m, td.Amount);
        }

        [Test]
        public void Merge_PrimitiveTypes_NonCached_X100()
        {
            for (int i = 0; i < 100; i++)
            {
                var td = new TestData();
                Assert.IsTrue(new JsonMergePatch().Merge("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }", ref td));
            }
        }

        [Test]
        public void Merge_PrimitiveTypes_Cached_X100()
        {
            var jom = new JsonMergePatch();
            for (int i = 0; i < 100; i++)
            {
                var td = new TestData();
                Assert.IsTrue(jom.Merge("{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58 }", ref td));
            }
        }

        [Test]
        public void Merge_Property_SubEntityNull()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"sub\": null }", ref td));
            Assert.IsNull(td!.Sub);
        }

        [Test]
        public void Merge_Property_SubEntityNewEmpty()
        {
            var td = new TestData();
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"sub\": { } }", ref td));
            Assert.IsNotNull(td!.Sub);
            Assert.IsNull(td.Sub!.Code);
            Assert.IsNull(td.Sub.Text);
        }

        [Test]
        public void Merge_Property_SubEntityExistingEmpty()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.IsFalse(new JsonMergePatch().Merge<TestData>("{ \"sub\": { } }", ref td));
            Assert.IsNotNull(td!.Sub);
            Assert.IsNull(td.Sub!.Code);
            Assert.IsNull(td.Sub.Text);
        }

        [Test]
        public void Merge_Property_SubEntityExistingChanged()
        {
            var td = new TestData { Sub = new SubData() };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"sub\": { \"code\": \"x\", \"text\": \"xxx\" } }", ref td));
            Assert.IsNotNull(td!.Sub);
            Assert.AreEqual("x", td.Sub!.Code);
            Assert.AreEqual("xxx", td.Sub.Text);
        }

        [Test]
        public void Merge_Property_ArrayMalformed()
        {
            var td = new TestData();
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch().Merge("{ \"values\": { } }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON value could not be converted to System.Int32[]. Path: $.values | LineNumber: 0 | BytePositionInLine: 13.");
        }

        [Test]
        public void Merge_Property_ArrayNull()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"values\": null }", ref td));
            Assert.IsNull(td!.Values);
        }

        [Test]
        public void Merge_Property_ArrayEmpty()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"values\": [] }", ref td));
            Assert.IsNotNull(td!.Values);
            Assert.AreEqual(0, td.Values!.Length);
        }

        [Test]
        public void Merge_Property_ArrayValues_NoChanges()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.IsFalse(new JsonMergePatch().Merge("{ \"values\": [ 1, 2, 3] }", ref td));
            Assert.IsNotNull(td!.Values);
            Assert.AreEqual(3, td.Values!.Length);
        }

        [Test]
        public void Merge_Property_ArrayValues_Changes()
        {
            var td = new TestData { Values = new int[] { 1, 2, 3 } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"values\": [ 3, 2, 1] }", ref td));
            Assert.IsNotNull(td!.Values);
            Assert.AreEqual(3, td.Values!.Length);
            Assert.AreEqual(3, td.Values[0]);
            Assert.AreEqual(2, td.Values[1]);
            Assert.AreEqual(1, td.Values[2]);
        }

        [Test]
        public void Merge_Property_NoKeys_ListNull()
        {
            var td = new TestData { NoKeys = new List<SubData> { new SubData() } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"nokeys\": null }", ref td));
            Assert.IsNull(td!.Values);
        }

        [Test]
        public void Merge_Property_NoKeys_ListEmpty()
        {
            var td = new TestData { NoKeys = new List<SubData> { new SubData() } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"nokeys\": [ ] }", ref td));
            Assert.IsNotNull(td!.NoKeys);
            Assert.AreEqual(0, td.NoKeys!.Count);
        }

        [Test]
        public void Merge_Property_NoKeys_List()
        {
            var td = new TestData { NoKeys = new List<SubData> { new SubData() } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"nokeys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, { }, null ] }", ref td));
            Assert.IsNotNull(td!.NoKeys);
            Assert.AreEqual(3, td.NoKeys!.Count);

            Assert.IsNotNull(td.NoKeys[0]);
            Assert.AreEqual("abc", td.NoKeys[0].Code);
            Assert.AreEqual("xyz", td.NoKeys[0].Text);

            Assert.IsNotNull(td.NoKeys[1]);
            Assert.IsNull(td.NoKeys[1].Code);
            Assert.IsNull(td.NoKeys[1].Text);

            Assert.IsNull(td.NoKeys[2]);
        }

        [Test]
        public void Merge_Property_Keys_ListNull()
        {
            var td = new TestData { Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"keys\": null }", ref td));

            Assert.IsNull(td!.Keys);
        }

        [Test]
        public void Merge_Property_Keys_ListEmpty()
        {
            var td = new TestData { Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"keys\": [ ] }", ref td));

            Assert.IsNotNull(td!.Keys);
            Assert.AreEqual(0, td.Keys!.Count);
        }

        [Test]
        public void Merge_Property_Keys_Null()
        {
            var td = new TestData { Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"keys\": [ null ] }", ref td));

            Assert.IsNotNull(td!.Keys);
            Assert.AreEqual(1, td.Keys!.Count);
            Assert.IsNull(td.Keys[0]);
        }

        [Test]
        public void Merge_Property_Keys_Replace()
        {
            var td = new TestData { Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"keys\": [ { \"code\": \"abc\" }, { \"code\": \"uvw\", \"text\": \"xyz\" } ] }", ref td));

            Assert.IsNotNull(td!.Keys);
            Assert.AreEqual(2, td.Keys!.Count);
            Assert.AreEqual("abc", td.Keys[0].Code);
            Assert.AreEqual(null, td.Keys[0].Text);
            Assert.AreEqual("uvw", td.Keys[1].Code);
            Assert.AreEqual("xyz", td.Keys[1].Text);
        }

        [Test]
        public void Merge_Property_Keys_NoChanges()
        {
            // Note, although technically no changes, there is no means to verify without specific equality checking, so is seen as a change.
            var td = new TestData { Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{ \"keys\": [ { \"code\": \"abc\", \"text\": \"def\" } ] }", ref td));

            Assert.IsNotNull(td!.Keys);
            Assert.AreEqual(1, td.Keys!.Count);
            Assert.AreEqual("abc", td.Keys[0].Code);
            Assert.AreEqual("def", td.Keys[0].Text);
        }

        [Test]
        public void Merge_Property_KeysColl_ListNull()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": null }", ref td));
            Assert.IsNull(td!.Values);
        }

        [Test]
        public void Merge_Property_KeysColl_ListEmpty()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ ] }", ref td));
            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(0, td.KeysColl!.Count);
        }

        [Test]
        public void Merge_Property_KeysColl_Null()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "abc", Text = "def" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ null ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(1, td.KeysColl!.Count);
            Assert.IsNull(td.KeysColl[0]);
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateNulls()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ null, null ] }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON array must not contain items with duplicate 'IPrimaryKey' keys. Path: $.keyscoll");
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals1()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { }, { } ] }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON array must not contain items with duplicate 'IPrimaryKey' keys. Path: $.keyscoll");
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals2()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"a\" }, { \"code\": \"a\" } ] }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON array must not contain items with duplicate 'IPrimaryKey' keys. Path: $.keyscoll");
        }

        [Test]
        public void Merge_Property_KeysColl_DuplicateVals_Dest()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData(), new KeyData() } };
            var ex = Assert.Throws<JsonMergePatchException>(() => new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { } ] }", ref td));
            Assert.AreEqual(ex!.Message, "The JSON array destination collection must not contain items with duplicate 'IPrimaryKey' keys prior to merge. Path: $.keyscoll");
        }

        [Test]
        public void Merge_Property_KeysColl_Null_NoChanges()
        {
            var td = new TestData { };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": null }", ref td));

            Assert.IsNull(td!.KeysColl);
        }

        [Test]
        public void Merge_Property_KeysColl_Empty_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection() };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(0, td.KeysColl!.Count);
        }

        [Test]
        public void Merge_Property_KeysColl_NullItem_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { null! } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ null ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(1, td.KeysColl!.Count);
            Assert.IsNull(td.KeysColl[0]);
        }

        [Test]
        public void Merge_Property_KeysColl_Item_NoChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a" } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"a\"  } ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(1, td.KeysColl!.Count);
            Assert.AreEqual("a", td.KeysColl[0].Code);
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_Changes()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"a\", \"text\": \"zz\" }, { \"code\": \"b\" } ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(2, td.KeysColl!.Count);
            Assert.AreEqual("a", td.KeysColl[0].Code);
            Assert.AreEqual("zz", td.KeysColl[0].Text);
            Assert.AreEqual("b", td.KeysColl[1].Code);
            Assert.AreEqual("bb", td.KeysColl[1].Text);
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_SequenceChanges()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"b\", \"text\": \"yy\" }, { \"code\": \"a\" } ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(2, td.KeysColl!.Count);
            Assert.AreEqual("b", td.KeysColl[0].Code);
            Assert.AreEqual("yy", td.KeysColl[0].Text);
            Assert.AreEqual("a", td.KeysColl[1].Code);
            Assert.AreEqual("aa", td.KeysColl[1].Text);
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_AllNew()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"y\", \"text\": \"yy\" }, { \"code\": \"z\", \"text\": \"zz\" } ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(2, td.KeysColl!.Count);
            Assert.AreEqual("y", td.KeysColl[0].Code);
            Assert.AreEqual("yy", td.KeysColl[0].Text);
            Assert.AreEqual("z", td.KeysColl[1].Code);
            Assert.AreEqual("zz", td.KeysColl[1].Text);
        }

        [Test]
        public void Merge_Property_KeysColl_KeyedItem_Delete()
        {
            var td = new TestData { KeysColl = new KeyDataCollection { new KeyData { Code = "a", Text = "aa" }, new KeyData { Code = "b", Text = "bb" }, new KeyData { Code = "c", Text = "cc" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge }).Merge("{ \"keyscoll\": [ { \"code\": \"a\" }, { \"code\": \"c\" } ] }", ref td));

            Assert.IsNotNull(td!.KeysColl);
            Assert.AreEqual(2, td.KeysColl!.Count);
            Assert.AreEqual("a", td.KeysColl[0].Code);
            Assert.AreEqual("aa", td.KeysColl[0].Text);
            Assert.AreEqual("c", td.KeysColl[1].Code);
            Assert.AreEqual("cc", td.KeysColl[1].Text);
        }

        // *** Dictionary<string, string> - DictionaryMergeApproach.Replace

        [Test]
        public void Merge_Property_DictReplace_Null()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{ \"dict\": null }", ref td));
            Assert.IsNull(td!.Dict);
        }

        [Test]
        public void Merge_Property_DictReplace_Empty()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{ \"dict\": {} }", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(0, td.Dict!.Count);
        }

        [Test]
        public void Merge_Property_DictReplace_NullValue()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{ \"dict\": {\"k\":null} }", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual(null, td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictReplace_DuplicateKeys_IntoNull()
        {
            var td = new TestData();

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual("v2", td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictReplace_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict = new Dictionary<string, string>() };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual("v2", td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictReplace_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict\":{\"k\":\"v\",\"k1\":\"v1\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
            Assert.AreEqual("v1", td.Dict["k1"]);
        }

        [Test]
        public void Merge_Property_DictReplace_ReOrder_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict\":{\"k1\":\"v1\",\"k\":\"v\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
            Assert.AreEqual("v1", td.Dict["k1"]);
        }

        [Test]
        public void Merge_Property_DictReplace_AddUpdateDelete_Replace()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict\":{\"k\":\"v\",\"k2\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
            Assert.AreEqual("v2", td.Dict["k2"]);
        }

        // *** Dictionary<string, string> - DictionaryMergeApproach.Merge

        [Test]
        public void Merge_Property_DictMerge_Null()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{ \"dict\": null }", ref td));
            Assert.IsNull(td!.Dict);
        }

        [Test]
        public void Merge_Property_DictMerge_Empty()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            // Should result in no changes as no property (key) was provided.
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{ \"dict\": {} }", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictMerge_NullValue()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" } } };

            // A key with a value of null indicates it should be removed.
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{ \"dict\": {\"k\":null} }", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(0, td.Dict!.Count);
        }

        [Test]
        public void Merge_Property_DictMerge_DuplicateKeys_IntoNull()
        {
            var td = new TestData { };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual("v2", td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictMerge_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict = new Dictionary<string, string>() };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict\":{\"k\":\"v\",\"k\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(1, td.Dict!.Count);
            Assert.AreEqual("v2", td.Dict["k"]);
        }

        [Test]
        public void Merge_Property_DictMerge_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict\":{\"k\":\"v\",\"k1\":\"v1\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
            Assert.AreEqual("v1", td.Dict["k1"]);
        }

        [Test]
        public void Merge_Property_DictMerge_ReOrder_NoChange()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict\":{\"k1\":\"v1\",\"k\":\"v\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("v", td.Dict["k"]);
            Assert.AreEqual("v1", td.Dict["k1"]);
        }

        [Test]
        public void Merge_Property_DictMerge_AddUpdateDelete()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{\"dict\":{\"k\":\"vx\",\"k2\":\"v2\",\"k1\":null}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(2, td.Dict!.Count);
            Assert.AreEqual("vx", td.Dict["k"]);
            Assert.AreEqual("v2", td.Dict["k2"]);
        }

        [Test]
        public void Merge_Property_DictMerge_AddUpdate()
        {
            var td = new TestData { Dict = new Dictionary<string, string> { { "k", "v" }, { "k1", "v1" } } };
            Assert.IsTrue(new JsonMergePatch().Merge("{\"dict\":{\"k\":\"vx\",\"k2\":\"v2\"}}", ref td));
            Assert.IsNotNull(td!.Dict);
            Assert.AreEqual(3, td.Dict!.Count);
            Assert.AreEqual("vx", td.Dict["k"]);
            Assert.AreEqual("v1", td.Dict["k1"]);
            Assert.AreEqual("v2", td.Dict["k2"]);
        }

        // ***

        [Test]
        public void Merge_Property_Dict2Replace_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData>() };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"a\":{\"code\": \"bb\",\"text\": \"bbb\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(1, td.Dict2!.Count);
            Assert.AreEqual("bb", td.Dict2["a"].Code);
            Assert.AreEqual("bbb", td.Dict2["a"].Text);
        }

        [Test]
        public void Merge_Property_Dict2Replace_NoChange()
        {
            // Note, although technically no changes, there is no means to verify without specific equality checking, so is seen as a change.
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"b\":{\"code\": \"bb\",\"text\": \"bbb\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(2, td.Dict2!.Count);
            Assert.AreEqual("aa", td.Dict2["a"].Code);
            Assert.AreEqual("aaa", td.Dict2["a"].Text);
            Assert.AreEqual("bb", td.Dict2["b"].Code);
            Assert.AreEqual("bbb", td.Dict2["b"].Text);
        }

        [Test]
        public void Merge_Property_Dict2Replace_AddUpdateDelete_Replace()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace }).Merge("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(2, td.Dict2!.Count);
            Assert.AreEqual("aaaa", td.Dict2["a"].Code);
            Assert.AreEqual(null, td.Dict2["a"].Text);
            Assert.AreEqual("cc", td.Dict2["c"].Code);
            Assert.AreEqual("ccc", td.Dict2["c"].Text);
        }

        // ***

        [Test]
        public void Merge_Property_KeyDict2Merge_DuplicateKeys_IntoEmpty()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData>() };

            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"a\":{\"code\": \"bb\",\"text\": \"bbb\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(1, td.Dict2!.Count);
            Assert.AreEqual("bb", td.Dict2["a"].Code);
            Assert.AreEqual("bbb", td.Dict2["a"].Text);
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_NoChange()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.IsFalse(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict2\":{\"a\":{\"code\": \"aa\",\"text\": \"aaa\"},\"b\":{\"code\": \"bb\",\"text\": \"bbb\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(2, td.Dict2!.Count);
            Assert.AreEqual("aa", td.Dict2["a"].Code);
            Assert.AreEqual("aaa", td.Dict2["a"].Text);
            Assert.AreEqual("bb", td.Dict2["b"].Code);
            Assert.AreEqual("bbb", td.Dict2["b"].Text);
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_AddUpdateDelete()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"},\"b\":null}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(2, td.Dict2!.Count);
            Assert.AreEqual("aaaa", td.Dict2["a"].Code);
            Assert.AreEqual("aaa", td.Dict2["a"].Text);
            Assert.AreEqual("cc", td.Dict2["c"].Code);
            Assert.AreEqual("ccc", td.Dict2["c"].Text);
        }

        [Test]
        public void Merge_Property_KeyDict2Merge_AddUpdate()
        {
            var td = new TestData { Dict2 = new Dictionary<string, KeyData> { { "a", new KeyData { Code = "aa", Text = "aaa" } }, { "b", new KeyData { Code = "bb", Text = "bbb" } } } };
            Assert.IsTrue(new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge("{\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}", ref td));
            Assert.IsNotNull(td!.Dict2);
            Assert.AreEqual(3, td.Dict2!.Count);
            Assert.AreEqual("aaaa", td.Dict2["a"].Code);
            Assert.AreEqual("aaa", td.Dict2["a"].Text);
            Assert.AreEqual("bb", td.Dict2["b"].Code);
            Assert.AreEqual("bbb", td.Dict2["b"].Text);
            Assert.AreEqual("cc", td.Dict2["c"].Code);
            Assert.AreEqual("ccc", td.Dict2["c"].Text);
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
                var td = new TestData { Values = new int[] { 1, 2, 3 }, Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } }, Dict = new Dictionary<string, string>() { { "a", "b" } }, Dict2 = new Dictionary<string, KeyData> { { "x", new KeyData { Code = "xx" } } } };
                new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge, DictionaryMergeApproach = DictionaryMergeApproach.Merge }).Merge(text, ref td);
            }
        }

        [Test]
        public void Merge_XLoadTest_WithCache_1000()
        {
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { PrimaryKeyCollectionMergeApproach = PrimaryKeyCollectionMergeApproach.Merge, DictionaryMergeApproach = DictionaryMergeApproach.Merge });
            var text = "{ \"id\": \"13512759-4f50-e911-b35c-bc83850db74d\", \"name\": \"Barry\", \"isValid\": true, \"date\": \"2018-12-31\", \"count\": \"12\", \"amount\": 132.58, \"dict\": {\"k\":\"v\",\"k1\":\"v1\"}, "
                    + "\"values\": [ 1, 2, 4], \"sub\": { \"code\": \"abc\", \"text\": \"xyz\" }, \"nokeys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, null, { } ], "
                    + "\"keys\": [ { \"code\": \"abc\", \"text\": \"xyz\" }, { }, null ],\"dict2\":{\"a\":{\"code\": \"aaaa\"},\"c\":{\"code\": \"cc\",\"text\": \"ccc\"}}}";

            for (int i = 0; i < 1000; i++)
            {
                var td = new TestData { Values = new int[] { 1, 2, 3 }, Keys = new List<KeyData> { new KeyData { Code = "abc", Text = "def" } }, Dict = new Dictionary<string, string>() { { "a", "b" } }, Dict2 = new Dictionary<string, KeyData> { { "x", new KeyData { Code = "xx" } } } };
                jmp.Merge(text, ref td);
            }
        }

        // **

        [Test]
        public void Merge_RootSimple_NullString()
        {
            string? s = null;
            var jmp = new JsonMergePatch();
            Assert.IsFalse(jmp.Merge("null", ref s));
            Assert.AreEqual(null, s);

            s = "x";
            Assert.IsTrue(jmp.Merge("null", ref s));
            Assert.AreEqual(null, s);

            s = null;
            Assert.IsTrue(jmp.Merge("\"x\"", ref s));
            Assert.AreEqual("x", s);

            Assert.IsFalse(jmp.Merge("\"x\"", ref s));
            Assert.AreEqual("x", s);
        }

        [Test]
        public void Merge_RootSimple_NullInt()
        {
            int? i = null;
            var jmp = new JsonMergePatch();
            Assert.IsFalse(jmp.Merge("null", ref i));
            Assert.AreEqual(null, i);

            i = 88;
            Assert.IsTrue(jmp.Merge("null", ref i));
            Assert.AreEqual(null, i);

            i = null;
            Assert.IsTrue(jmp.Merge("88", ref i));
            Assert.AreEqual(88, i);

            Assert.IsFalse(jmp.Merge("88", ref i));
            Assert.AreEqual(88, i);
        }

        [Test]
        public void Merge_RootSimple_Int()
        {
            int i = 0;
            var jmp = new JsonMergePatch();
            Assert.IsFalse(jmp.Merge("0", ref i));
            Assert.AreEqual(0, i);

            Assert.IsTrue(jmp.Merge("88", ref i));
            Assert.AreEqual(88, i);
        }

        [Test]
        public void Merge_RootComplex_Null()
        {
            SubData? sd = null!;
            var jmp = new JsonMergePatch();

            Assert.IsFalse(jmp.Merge("null", ref sd));
            Assert.IsNull(sd);

            sd = new SubData { Code = "X" };
            Assert.IsTrue(jmp.Merge("null", ref sd));
            Assert.IsNull(sd);

            sd = null;
            Assert.IsTrue(jmp.Merge("{\"code\":\"x\"}", ref sd));
            Assert.IsNotNull(sd);
            Assert.AreEqual("x", sd!.Code);
        }

        [Test]
        public void Merge_RootArray_Simple_Null()
        {
            var arr = new int[0];
            var jmp = new JsonMergePatch();
            jmp.Merge("null", ref arr);
            Assert.AreEqual(null, arr);

            arr = new int[] { 1, 2 };
            jmp.Merge("null", ref arr);
            Assert.AreEqual(null, arr);

            int[]? arr2 = null;
            jmp.Merge("null", ref arr2);
            Assert.AreEqual(null, arr2);

            arr2 = new int[] { 1, 2 };
            jmp.Merge("null", ref arr2);
            Assert.AreEqual(null, arr2);
        }

        [Test]
        public void Merge_RootArray_Simple()
        {
            var arr = Array.Empty<int>();
            var jmp = new JsonMergePatch();
            Assert.IsTrue(jmp.Merge("[1,2,3]", ref arr));
            Assert.AreEqual(new int[] { 1, 2, 3 }, arr);

            Assert.IsFalse(jmp.Merge("[1,2,3]", ref arr));
            Assert.AreEqual(new int[] { 1, 2, 3 }, arr);
        }

        [Test]
        public void Merge_RootArray_Complex()
        {
            var arr = new SubData[] { new SubData { Code = "a", Text = "aa" }, new SubData { Code = "b", Text = "bb" } };
            var jmp = new JsonMergePatch();

            // No equality checker so will appear as changed - is a replacement.
            Assert.IsTrue(jmp.Merge("[{\"code\":\"a\",\"text\":\"aa\"},{\"code\":\"b\",\"text\":\"bb\"}]", ref arr));
            Assert.AreEqual(2, arr!.Length);

            // Replaced.
            Assert.IsTrue(jmp.Merge("[{\"code\":\"c\",\"text\":\"cc\"},{\"code\":\"b\",\"text\":\"bb\"}]", ref arr));
            Assert.AreEqual(2, arr!.Length);
            Assert.AreEqual("c", arr[0].Code);
        }

        [Test]
        public void Merge_RootDictionary_Simple()
        {
            var dict = new Dictionary<string, int>();
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge });

            Assert.IsTrue(jmp.Merge("{\"x\":1}", ref dict));
            Assert.AreEqual(new Dictionary<string, int> { { "x", 1 } }, dict);

            Assert.IsFalse(jmp.Merge("{\"x\":1}", ref dict));
            Assert.AreEqual(new Dictionary<string, int> { { "x", 1 } }, dict);

            dict = new Dictionary<string, int>();
            jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace });

            Assert.IsTrue(jmp.Merge("{\"x\":1}", ref dict));
            Assert.AreEqual(new Dictionary<string, int> { { "x", 1 } }, dict);

            Assert.IsFalse(jmp.Merge("{\"x\":1}", ref dict));
            Assert.AreEqual(new Dictionary<string, int> { { "x", 1 } }, dict);
        }

        [Test]
        public void Merge_RootDictionary_Complex()
        {
            var dict = new Dictionary<string, SubData>();
            var jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Merge });

            Assert.IsTrue(jmp.Merge("{\"x\":{\"code\":\"xx\"},\"y\":{\"code\":\"yy\",\"text\":\"YY\"}}", ref dict));
            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict!.Count);
            Assert.AreEqual("xx", dict!["x"].Code);
            Assert.AreEqual(null, dict!["x"].Text);
            Assert.AreEqual("yy", dict!["y"].Code);
            Assert.AreEqual("YY", dict!["y"].Text);

            Assert.IsTrue(jmp.Merge("{\"y\":{\"code\":\"yyy\"},\"x\":{\"code\":\"xxx\"}}", ref dict));
            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict!.Count);
            Assert.AreEqual("xxx", dict!["x"].Code);
            Assert.AreEqual(null, dict!["x"].Text);
            Assert.AreEqual("yyy", dict!["y"].Code);
            Assert.AreEqual("YY", dict!["y"].Text);

            // --

            dict = new Dictionary<string, SubData>();
            jmp = new JsonMergePatch(new JsonMergePatchOptions { DictionaryMergeApproach = DictionaryMergeApproach.Replace });

            Assert.IsTrue(jmp.Merge("{\"x\":{\"code\":\"xx\"},\"y\":{\"code\":\"yy\",\"text\":\"YY\"}}", ref dict));
            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict!.Count);
            Assert.AreEqual("xx", dict!["x"].Code);
            Assert.AreEqual(null, dict!["x"].Text);
            Assert.AreEqual("yy", dict!["y"].Code);
            Assert.AreEqual("YY", dict!["y"].Text);

            Assert.IsTrue(jmp.Merge("{\"y\":{\"code\":\"yyy\"},\"x\":{\"code\":\"xxx\"}}", ref dict));
            Assert.IsNotNull(dict);
            Assert.AreEqual(2, dict!.Count);
            Assert.AreEqual("xxx", dict!["x"].Code);
            Assert.AreEqual(null, dict!["x"].Text);
            Assert.AreEqual("yyy", dict!["y"].Code);
            Assert.AreEqual(null, dict!["y"].Text);
        }

        [Test]
        public void Merge_RootKeyedColl()
        {

        }
    }
}