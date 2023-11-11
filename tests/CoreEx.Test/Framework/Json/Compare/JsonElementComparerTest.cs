using CoreEx.Json.Compare;
using NUnit.Framework;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Test.Framework.Json.Compare
{
    [TestFixture]
    internal class JsonElementComparerTest
    {
        [Test]
        public void Compare_Object_SameSame()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}");
            Assert.IsTrue(r.AreEqual);
        }

        [Test]
        public void Compare_Object_DiffOrderAndDiffNumberFormat()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}");
            Assert.IsTrue(r.AreEqual);
            Assert.AreEqual("No differences detected.", r.ToString());
        }

        [Test]
        public void Compare_Object_DiffValuesAndTypes()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"brian\",\"age\":41.0,\"cool\":false,\"salary\":null}");
            Assert.IsTrue(r.HasDifferences);
            Assert.AreEqual(@"Path '$.name': Value is not equal: ""gary"" != ""brian"".
Path '$.age': Value is not equal: 40.0 != 41.0.
Path '$.cool': Kind is not equal: Null != False.
Path '$.salary': Kind is not equal: Number != Null.", r.ToString());
        }

        [Test]
        public void Compare_Object_PropertyNameMismatch()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}");
            Assert.IsTrue(r.HasDifferences);
            Assert.AreEqual(@"Path '$.name': Does not exist in right JSON.
Path '$.salary': Does not exist in right JSON.
Path '$.Name': Does not exist in left JSON.", r.ToString());
        }

        [Test]
        public void Compare_Object_PropertyNameMismatch_Exclude()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}", "name", "Name");
            Assert.IsTrue(r.HasDifferences);
            Assert.AreEqual(@"Path '$.salary': Does not exist in right JSON.", r.ToString());
        }

        [Test]
        public void Compare_Array_LengthMismatch()
        {
            var r = new JsonElementComparer().Compare("[1,2,3]", "[1,2,3,4]");
            Assert.IsTrue(r.HasDifferences);
            Assert.AreEqual("Path '$': Array lengths are not equal: 3 != 4.", r.ToString());
        }

        [Test]
        public void Compare_Array_ItemMismatch()
        {
            var r = new JsonElementComparer().Compare("[1,2,3,5]", "[1,2,3,4]");
            Assert.AreEqual("Path '$[3]': Value is not equal: 5 != 4.", r.ToString());
        }

        [Test]
        public void Compare_Object_Array_ItemMismatch()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}");
            Assert.AreEqual(@"Path '$.names[1].name': Value is not equal: ""brian"" != ""rebecca"".", r.ToString());
        }

        [Test]
        public void Compare_Object_Array_Exclude()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}", "names.name");
            Assert.IsTrue(r.AreEqual);
        }

        [Test]
        public void Compare_Object_Array_ItemMismatchComplex()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}");
            Assert.AreEqual(@"Path '$.names[0].address.street': Value is not equal: 1 != 2.", r.ToString());
        }

        [Test]
        public void CompareValue()
        {
            var r = new JsonElementComparer().CompareValue(100, 100);
            Assert.IsTrue(r.AreEqual);
            Assert.AreEqual("No differences detected.", r.ToString());

            r = new JsonElementComparer().CompareValue(100, "Abc");
            Assert.IsTrue(r.HasDifferences);
            Assert.AreEqual("Path '$': Kind is not equal: Number != String.", r.ToString());
        }

        [Test]
        public void Compare_Exact_Number()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Exact };
            var r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.2000}");
            Assert.IsFalse(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.200}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.0E+2}", "{\"value\": 100}");
            Assert.IsFalse(r.AreEqual);
        }

        [Test]
        public void Compare_Semantic_Number()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Semantic };
            var r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.2000}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.200}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.0E+2}", "{\"value\": 100}");
            Assert.IsTrue(r.AreEqual);
        }

        [Test]
        public void Compare_Exact_String()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Exact };
            var r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01\"}", "{\"value\": \"2000-01-01T00:00:00\"}");
            Assert.IsFalse(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01T13:52:18\"}", "{\"value\": \"2000-01-01T13:52:18\"}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-E7A9-40B8-A9D6-14317CE36EFD\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.IsFalse(r.AreEqual);
        }

        [Test]
        public void Compare_Semantic_String()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Semantic };
            var r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01\"}", "{\"value\": \"2000-01-01T00:00:00\"}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01T13:52:18\"}", "{\"value\": \"2000-01-01T13:52:18\"}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.IsTrue(r.AreEqual);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-E7A9-40B8-A9D6-14317CE36EFD\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.IsTrue(r.AreEqual);
        }

        [Test]
        public void Equals_Object_SameSame()
        {
            Assert.IsTrue(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"));
        }

        [Test]
        public void Equals_Object_DiffOrderAndDiffNumberFormat()
        {
            Assert.IsTrue(new JsonElementComparer().Equals("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"));
        }

        [Test]
        public void Equals_Object_DiffValuesAndTypes()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"brian\",\"age\":41.0,\"cool\":false,\"salary\":null}"));
        }

        [Test]
        public void Equals_Object_PropertyNameMismatch()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}"));
        }

        [Test]
        public void Equals_Array_LengthMismatch()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("[1,2,3]", "[1,2,3,4]"));
        }

        [Test]
        public void Equals_Array_ItemMismatch()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("[1,2,3,5]", "[1,2,3,4]"));
        }

        [Test]
        public void Equals_Object_Array_ItemMismatch()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}"));
        }

        [Test]
        public void Equals_Object_Array_ItemMismatchComplex()
        {
            Assert.IsFalse(new JsonElementComparer().Equals("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}"));
        }

        [Test]
        public void Hashcode_Object_DiffOrderAndDiffNumberFormat()
        {
            Assert.AreEqual(new JsonElementComparer().GetHashCode("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}"), new JsonElementComparer().GetHashCode("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"));
            Assert.AreNotEqual(new JsonElementComparer().GetHashCode("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}"), new JsonElementComparer().GetHashCode("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}"));
        }

        [Test]
        public void ToMergePatch_Object_Simple()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":false,\"salary\":null}").ToMergePatch();
            Assert.AreEqual("{\"age\":41.0,\"cool\":false,\"salary\":null}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_Simple_Null_To_Null()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":null,\"salary\":null}").ToMergePatch();
            Assert.AreEqual("{\"age\":41.0,\"salary\":null}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_Simple_Null_To_Null_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":null,\"salary\":null}").ToMergePatch("cool");
            Assert.AreEqual("{\"age\":41.0,\"cool\":null,\"salary\":null}", jn!.ToJsonString());
        } 

        [Test]
        public void ToMergePatch_Object_Simple_Nested()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch();
            Assert.AreEqual("{\"address\":{\"postcode\":12345}}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_Simple_Nested_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address");
            Assert.AreEqual("{\"address\":{\"street\":\"petherick\",\"postcode\":12345}}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address.street");
            Assert.AreEqual("{\"address\":{\"street\":\"petherick\",\"postcode\":12345}}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address.country");
            Assert.AreEqual("{\"address\":{\"postcode\":12345}}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}").ToMergePatch();
            Assert.AreEqual("{}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}").ToMergePatch("address.country");
            Assert.AreEqual("{}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_With_ReplaceAllArray()
        {
            var jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.AreEqual("{}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_With_NoReplaceAllArray()
        {
            var o = new JsonElementComparerOptions { AlwaysReplaceAllArrayItems = false };
            var jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.AreEqual("{\"names\":[{\"address\":{\"street\":2}}]}", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.AreEqual("{}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_With_Array_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":1}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":1}},{\"name\":\"brian\"}]}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Object_With_NoReplaceAllArray_Paths()
        {
            var o = new JsonElementComparerOptions { AlwaysReplaceAllArrayItems = false };
            var jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.AreEqual("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.AreEqual("{\"names\":[{\"address\":{\"street\":2}},{\"name\":\"brian\"}]}", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.AreEqual("{\"names\":[{\"name\":\"brian\"}]}", jn!.ToJsonString());
        }

        [Test]
        public void ToMergePatch_Value()
        {
            var jn = new JsonElementComparer().Compare("\"Blah\"", "\"Blah2\"").ToMergePatch();
            Assert.AreEqual("\"Blah2\"", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("123", "456").ToMergePatch();
            Assert.AreEqual("456", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("true", "true").ToMergePatch();
            Assert.AreEqual("true", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("null", "null").ToMergePatch();
            Assert.IsNull(jn);
        }

        [Test]
        public void ToMergePatch_Root_Array()
        {
            var jn = new JsonElementComparer().Compare("[1,2,3]", "[1,9,3]").ToMergePatch();
            Assert.AreEqual("[1,9,3]", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("[1,2,3]", "[1,null,3]").ToMergePatch();
            Assert.AreEqual("[1,null,3]", jn!.ToJsonString());

            jn = new JsonElementComparer().Compare("[1,2,3]", "[1,null,3,{\"age\":21}]").ToMergePatch();
            Assert.AreEqual("[1,null,3,{\"age\":21}]", jn!.ToJsonString());

            var o = new JsonElementComparerOptions { AlwaysReplaceAllArrayItems = false };
            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,9,3]").ToMergePatch();
            Assert.AreEqual("[9]", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,3]").ToMergePatch();
            Assert.AreEqual("[null]", jn!.ToJsonString());

            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,{\"age\":21}]").ToMergePatch();
            Assert.AreEqual("[null,{\"age\":21}]", jn!.ToJsonString());

            // Array length difference always results in a replace (i.e. all).
            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,{\"age\":21},8]").ToMergePatch();
            Assert.AreEqual("[1,null,{\"age\":21},8]", jn!.ToJsonString());
        }
    }
}