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
            Assert.That(r.AreEqual, Is.True);
        }

        [Test]
        public void Compare_Object_DiffOrderAndDiffNumberFormat()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}");
            Assert.Multiple(() =>
            {
                Assert.That(r.AreEqual, Is.True);
                Assert.That(r.ToString(), Is.EqualTo("No differences detected."));
            });
        }

        [Test]
        public void Compare_Object_DiffValuesAndTypes()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"brian\",\"age\":41.0,\"cool\":false,\"salary\":null}");
            Assert.Multiple(() =>
            {
                Assert.That(r.HasDifferences, Is.True);
                Assert.That(r.ToString(), Is.EqualTo(@"Path '$.name': Value is not equal: ""gary"" != ""brian"".
Path '$.age': Value is not equal: 40.0 != 41.0.
Path '$.cool': Kind is not equal: Null != False.
Path '$.salary': Kind is not equal: Number != Null."));
            });
        }

        [Test]
        public void Compare_Object_PropertyNameMismatch()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}");
            Assert.Multiple(() =>
            {
                Assert.That(r.HasDifferences, Is.True);
                Assert.That(r.ToString(), Is.EqualTo(@"Path '$.name': Does not exist in right JSON.
Path '$.salary': Does not exist in right JSON.
Path '$.Name': Does not exist in left JSON."));
            });
        }

        [Test]
        public void Compare_Object_PropertyNameMismatch_Exclude()
        {
            var r = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}", "name", "Name");
            Assert.Multiple(() =>
            {
                Assert.That(r.HasDifferences, Is.True);
                Assert.That(r.ToString(), Is.EqualTo(@"Path '$.salary': Does not exist in right JSON."));
            });
        }

        [Test]
        public void Compare_Array_LengthMismatch()
        {
            var r = new JsonElementComparer().Compare("[1,2,3]", "[1,2,3,4]");
            Assert.Multiple(() =>
            {
                Assert.That(r.HasDifferences, Is.True);
                Assert.That(r.ToString(), Is.EqualTo("Path '$': Array lengths are not equal: 3 != 4."));
            });
        }

        [Test]
        public void Compare_Array_ItemMismatch()
        {
            var r = new JsonElementComparer().Compare("[1,2,3,5]", "[1,2,3,4]");
            Assert.That(r.ToString(), Is.EqualTo("Path '$[3]': Value is not equal: 5 != 4."));
        }

        [Test]
        public void Compare_Object_Array_ItemMismatch()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}");
            Assert.That(r.ToString(), Is.EqualTo(@"Path '$.names[1].name': Value is not equal: ""brian"" != ""rebecca""."));
        }

        [Test]
        public void Compare_Object_Array_Exclude()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}", "names.name");
            Assert.That(r.AreEqual, Is.True);
        }

        [Test]
        public void Compare_Object_Array_ItemMismatchComplex()
        {
            var r = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}");
            Assert.That(r.ToString(), Is.EqualTo(@"Path '$.names[0].address.street': Value is not equal: 1 != 2."));
        }

        [Test]
        public void CompareValue()
        {
            var r = new JsonElementComparer().CompareValue(100, 100);
            Assert.Multiple(() =>
            {
                Assert.That(r.AreEqual, Is.True);
                Assert.That(r.ToString(), Is.EqualTo("No differences detected."));
            });

            r = new JsonElementComparer().CompareValue(100, "Abc");
            Assert.Multiple(() =>
            {
                Assert.That(r.HasDifferences, Is.True);
                Assert.That(r.ToString(), Is.EqualTo("Path '$': Kind is not equal: Number != String."));
            });
        }

        [Test]
        public void Compare_Exact_Number()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Exact };
            var r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.2000}");
            Assert.That(r.AreEqual, Is.False);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.200}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.0E+2}", "{\"value\": 100}");
            Assert.That(r.AreEqual, Is.False);
        }

        [Test]
        public void Compare_Semantic_Number()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Semantic };
            var r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.2000}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.200}", "{\"value\": 1.200}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": 1.0E+2}", "{\"value\": 100}");
            Assert.That(r.AreEqual, Is.True);
        }

        [Test]
        public void Compare_Exact_String()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Exact };
            var r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01\"}", "{\"value\": \"2000-01-01T00:00:00\"}");
            Assert.That(r.AreEqual, Is.False);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01T13:52:18\"}", "{\"value\": \"2000-01-01T13:52:18\"}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-E7A9-40B8-A9D6-14317CE36EFD\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.That(r.AreEqual, Is.False);
        }

        [Test]
        public void Compare_Semantic_String()
        {
            var ro = new JsonElementComparerOptions { ValueComparison = JsonElementComparison.Semantic };
            var r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01\"}", "{\"value\": \"2000-01-01T00:00:00\"}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"2000-01-01T13:52:18\"}", "{\"value\": \"2000-01-01T13:52:18\"}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.That(r.AreEqual, Is.True);

            r = new JsonElementComparer(ro).Compare("{\"value\": \"327b0068-E7A9-40B8-A9D6-14317CE36EFD\"}", "{\"value\": \"327b0068-e7a9-40b8-a9d6-14317ce36efd\"}");
            Assert.That(r.AreEqual, Is.True);
        }

        [Test]
        public void Equals_Object_SameSame()
        {
            Assert.That(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"), Is.True);
        }

        [Test]
        public void Equals_Object_DiffOrderAndDiffNumberFormat()
        {
            Assert.That(new JsonElementComparer().Equals("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}", "{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"), Is.True);
        }

        [Test]
        public void Equals_Object_DiffValuesAndTypes()
        {
            Assert.That(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"brian\",\"age\":41.0,\"cool\":false,\"salary\":null}"), Is.False);
        }

        [Test]
        public void Equals_Object_PropertyNameMismatch()
        {
            Assert.That(new JsonElementComparer().Equals("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}", "{\"Name\":\"gary\",\"age\":40.0,\"cool\":false}"), Is.False);
        }

        [Test]
        public void Equals_Array_LengthMismatch()
        {
            Assert.That(new JsonElementComparer().Equals("[1,2,3]", "[1,2,3,4]"), Is.False);
        }

        [Test]
        public void Equals_Array_ItemMismatch()
        {
            Assert.That(new JsonElementComparer().Equals("[1,2,3,5]", "[1,2,3,4]"), Is.False);
        }

        [Test]
        public void Equals_Object_Array_ItemMismatch()
        {
            Assert.That(new JsonElementComparer().Equals("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\"},{\"name\":\"rebecca\"}]}"), Is.False);
        }

        [Test]
        public void Equals_Object_Array_ItemMismatchComplex()
        {
            Assert.That(new JsonElementComparer().Equals("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}"), Is.False);
        }

        [Test]
        public void Hashcode_Object_DiffOrderAndDiffNumberFormat()
        {
            Assert.Multiple(() =>
            {
                Assert.That(new JsonElementComparer().GetHashCode("{\"name\":\"gary\",\"age\":40.0,\"cool\":false,\"salary\":null}"), Is.EqualTo(new JsonElementComparer().GetHashCode("{\"name\":\"gary\",\"cool\":false,\"age\":40,\"salary\":null}")));
                Assert.That(new JsonElementComparer().GetHashCode("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}"), Is.Not.EqualTo(new JsonElementComparer().GetHashCode("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}")));
            });
        }

        [Test]
        public void ToMergePatch_Object_Simple()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":false,\"salary\":null}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"age\":41.0,\"cool\":false,\"salary\":null}"));
        }

        [Test]
        public void ToMergePatch_Object_Simple_Null_To_Null()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":null,\"salary\":null}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"age\":41.0,\"salary\":null}"));
        }

        [Test]
        public void ToMergePatch_Object_Simple_Null_To_Null_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"age\":40.0,\"cool\":null,\"salary\":42000}", "{\"name\":\"gary\",\"age\":41.0,\"cool\":null,\"salary\":null}").ToMergePatch("cool");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"age\":41.0,\"cool\":null,\"salary\":null}"));
        } 

        [Test]
        public void ToMergePatch_Object_Simple_Nested()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"address\":{\"postcode\":12345}}"));
        }

        [Test]
        public void ToMergePatch_Object_Simple_Nested_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"address\":{\"street\":\"petherick\",\"postcode\":12345}}"));

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address.street");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"address\":{\"street\":\"petherick\",\"postcode\":12345}}"));

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":12345}}").ToMergePatch("address.country");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"address\":{\"postcode\":12345}}"));

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{}"));

            jn = new JsonElementComparer().Compare("{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}", "{\"name\":\"gary\",\"address\":{\"street\":\"petherick\",\"postcode\":1234}}").ToMergePatch("address.country");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{}"));
        }

        [Test]
        public void ToMergePatch_Object_With_ReplaceAllArray()
        {
            var jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{}"));
        }

        [Test]
        public void ToMergePatch_Object_With_NoReplaceAllArray()
        {
            var o = new JsonElementComparerOptions { ReplaceAllArrayItemsOnMerge = false };
            var jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"address\":{\"street\":2}}]}"));

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{}"));
        }

        [Test]
        public void ToMergePatch_Object_With_Array_Paths()
        {
            var jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":1}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer().Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":1}},{\"name\":\"brian\"}]}"));
        }

        [Test]
        public void ToMergePatch_Object_With_NoReplaceAllArray_Paths()
        {
            var o = new JsonElementComparerOptions { ReplaceAllArrayItemsOnMerge = false };
            var jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\":2}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names.name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"gary\"},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 2}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"address\":{\"street\":2}},{\"name\":\"brian\"}]}"));

            jn = new JsonElementComparer(o).Compare("{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}", "{\"names\":[{\"name\":\"gary\",\"address\":{\"street\": 1}},{\"name\":\"brian\"}]}").ToMergePatch("names[1].name");
            Assert.That(jn!.ToJsonString(), Is.EqualTo("{\"names\":[{\"name\":\"brian\"}]}"));
        }

        [Test]
        public void ToMergePatch_Value()
        {
            var jn = new JsonElementComparer().Compare("\"Blah\"", "\"Blah2\"").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("\"Blah2\""));

            jn = new JsonElementComparer().Compare("123", "456").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("456"));

            jn = new JsonElementComparer().Compare("true", "true").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("true"));

            jn = new JsonElementComparer().Compare("null", "null").ToMergePatch();
            Assert.That(jn, Is.Null);
        }

        [Test]
        public void ToMergePatch_Root_Array()
        {
            var jn = new JsonElementComparer().Compare("[1,2,3]", "[1,9,3]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[1,9,3]"));

            jn = new JsonElementComparer().Compare("[1,2,3]", "[1,null,3]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[1,null,3]"));

            jn = new JsonElementComparer().Compare("[1,2,3]", "[1,null,3,{\"age\":21}]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[1,null,3,{\"age\":21}]"));

            var o = new JsonElementComparerOptions { ReplaceAllArrayItemsOnMerge = false };
            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,9,3]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[9]"));

            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,3]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[null]"));

            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,{\"age\":21}]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[null,{\"age\":21}]"));

            // Array length difference always results in a replace (i.e. all).
            jn = new JsonElementComparer(o).Compare("[1,2,3]", "[1,null,{\"age\":21},8]").ToMergePatch();
            Assert.That(jn!.ToJsonString(), Is.EqualTo("[1,null,{\"age\":21},8]"));
        }
    }
}