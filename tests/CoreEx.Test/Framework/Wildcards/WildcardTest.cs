using CoreEx.Wildcards;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Test.Framework.Wildcards
{
    [TestFixture]
    public class WildcardTest
    {
        #region Determine

        [Test]
        public void Parse_1_NoneOrEqual()
        {
            var wc = Wildcard.BothAll;
            Check(WildcardSelection.None, null, wc.Parse(null));
            Check(WildcardSelection.None, null, wc.Parse(string.Empty));
            Check(WildcardSelection.None, null, wc.Parse(" "));
            Check(WildcardSelection.Equal, "X", wc.Parse("X"));
            Check(WildcardSelection.Equal, "XX", wc.Parse("XX"));
            Check(WildcardSelection.Equal, "XXX", wc.Parse("XXX"));
            Check(WildcardSelection.Equal, "XXX", wc.Parse(" XXX "));
        }

        [Test]
        public void Parse_2_Single()
        {
            var wc = Wildcard.BothAll;
            Check(WildcardSelection.Single | WildcardSelection.MultiWildcard, "*", wc.Parse("*"));
            Check(WildcardSelection.Single | WildcardSelection.MultiWildcard, "*", wc.Parse("**"));
            Check(WildcardSelection.Single | WildcardSelection.MultiWildcard, "*", wc.Parse("***"));
            Check(WildcardSelection.Single | WildcardSelection.SingleWildcard, "?", wc.Parse("?"));
            Check(WildcardSelection.StartsWith | WildcardSelection.EndsWith | WildcardSelection.SingleWildcard | WildcardSelection.AdjacentWildcards, "??", wc.Parse("??"));
            Check(WildcardSelection.StartsWith | WildcardSelection.EndsWith | WildcardSelection.Embedded | WildcardSelection.SingleWildcard | WildcardSelection.AdjacentWildcards, "???", wc.Parse("???"));
        }

        [Test]
        public void Parse_3_StartsAndEndsWithOrContains()
        {
            var wc = Wildcard.BothAll;
            Check(WildcardSelection.EndsWith | WildcardSelection.MultiWildcard, "*X", wc.Parse("*X"));
            Check(WildcardSelection.EndsWith | WildcardSelection.SingleWildcard, "?X", wc.Parse("?X"));
            Check(WildcardSelection.EndsWith | WildcardSelection.SingleWildcard, "?XX", wc.Parse("?XX"));
            Check(WildcardSelection.StartsWith | WildcardSelection.MultiWildcard, "X*", wc.Parse("X*"));
            Check(WildcardSelection.StartsWith | WildcardSelection.SingleWildcard, "X?", wc.Parse("X?"));
            Check(WildcardSelection.StartsWith | WildcardSelection.SingleWildcard, "XX?", wc.Parse("XX?"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Contains, "*X*", wc.Parse("*X*"));
            Check(WildcardSelection.SingleWildcard | WildcardSelection.Contains, "?X?", wc.Parse("?X?"));
        }

        [Test]
        public void Parse_4_EmbeddedOrContains()
        {
            var wc = Wildcard.BothAll;
            Check(WildcardSelection.Embedded | WildcardSelection.MultiWildcard, "X*X", wc.Parse("X*X"));
            Check(WildcardSelection.Embedded | WildcardSelection.MultiWildcard, "XX*XX", wc.Parse("XX*XX"));
            Check(WildcardSelection.Embedded | WildcardSelection.SingleWildcard, "XX?XX", wc.Parse("XX?XX"));
            Check(WildcardSelection.Embedded | WildcardSelection.SingleWildcard, "XX?XX", wc.Parse("XX?XX"));
            Check(WildcardSelection.Embedded | WildcardSelection.MultiWildcard, "X*X*XX", wc.Parse("X*X*XX"));
            Check(WildcardSelection.Embedded | WildcardSelection.MultiWildcard, "X*XX", wc.Parse("X**XX"));
            Check(WildcardSelection.Embedded | WildcardSelection.MultiWildcard | WildcardSelection.StartsWith, "XX*XX*", wc.Parse("XX*XX*"));

            Check(WildcardSelection.Contains | WildcardSelection.MultiWildcard, "*X*", wc.Parse("*X*"));
            Check(WildcardSelection.Contains | WildcardSelection.MultiWildcard | WildcardSelection.SingleWildcard, "*X?", wc.Parse("*X?"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Contains, "*X*", wc.Parse("**X*"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Contains, "*X*", wc.Parse("*X**"));
        }

        [Test]
        public void Parse_5_InvalidCharacters()
        {
            var wc = new Wildcard(WildcardSelection.BothAll, singleWildcard: '_', charactersNotAllowed: new char[] { '?' });
            Check(WildcardSelection.SingleWildcard | WildcardSelection.InvalidCharacter | WildcardSelection.StartsWith, "X?_", wc.Parse("X?_"));
        }

        [Test]
        public void Parse_6_SpaceTreatment()
        {
            var wc = new Wildcard(WildcardSelection.MultiAll, spaceTreatment: WildcardSpaceTreatment.Compress);
            Check(WildcardSelection.Equal, "X X", wc.Parse("X X"));
            Check(WildcardSelection.Equal, "X X", wc.Parse("X  X"));
            Check(WildcardSelection.Equal, "X X X", wc.Parse("X  X  X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.EndsWith, "*X X", wc.Parse("*X X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.StartsWith, "X X*", wc.Parse("X  X*"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X X* X", wc.Parse("X  X*  X"));

            wc = new Wildcard(WildcardSelection.MultiAll, spaceTreatment: WildcardSpaceTreatment.MultiWildcardAlways);
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X*X", wc.Parse("X X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X*X", wc.Parse("X  X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X*X*X", wc.Parse("X  X  X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded | WildcardSelection.EndsWith, "*X*X", wc.Parse("*X X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded | WildcardSelection.StartsWith, "X*X*", wc.Parse("X  X*"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X*X*X", wc.Parse("X  X*  X"));

            wc = new Wildcard(WildcardSelection.MultiAll, spaceTreatment: WildcardSpaceTreatment.MultiWildcardWhenOthers);
            Check(WildcardSelection.Equal, "X X", wc.Parse("X X"));
            Check(WildcardSelection.Equal, "X X", wc.Parse("X  X"));
            Check(WildcardSelection.Equal, "X X X", wc.Parse("X  X  X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded | WildcardSelection.EndsWith, "*X*X", wc.Parse("*X X"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded | WildcardSelection.StartsWith, "X*X*", wc.Parse("X  X*"));
            Check(WildcardSelection.MultiWildcard | WildcardSelection.Embedded, "X*X*X", wc.Parse("X  X*  X"));
        }

        private void Check(WildcardSelection selection, string? text, WildcardResult result)
        {
            Assert.Multiple(() =>
            {
                Assert.That(result.Selection, Is.EqualTo(selection));
                Assert.That(result.Text, Is.EqualTo(text));
            });
        }

        #endregion

        #region Validate

        [Test]
        public void Validate_1_Default()
        {
            var wc = Wildcard.BothAll;
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.True);
                Assert.That(wc.Validate(string.Empty), Is.True);
                Assert.That(wc.Validate("X"), Is.True);
                Assert.That(wc.Validate("*"), Is.True);
                Assert.That(wc.Validate("?"), Is.True);
                Assert.That(wc.Validate("XX"), Is.True);
                Assert.That(wc.Validate("*X"), Is.True);
                Assert.That(wc.Validate("?X"), Is.True);
                Assert.That(wc.Validate("X*"), Is.True);
                Assert.That(wc.Validate("X?"), Is.True);
                Assert.That(wc.Validate("XXX"), Is.True);
                Assert.That(wc.Validate("X*X"), Is.True);
                Assert.That(wc.Validate("X?X"), Is.True);
                Assert.That(wc.Validate("*?*"), Is.True);
                Assert.That(wc.Validate("*X*"), Is.True);
            });
        }

        [Test]
        public void Validate_2_CharactersNotAllowed()
        {
            var wc = new Wildcard(WildcardSelection.BothAll, multiWildcard: '%', singleWildcard: '_', charactersNotAllowed: new char[] { '*', '?' });
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.True);
                Assert.That(wc.Validate(string.Empty), Is.True);
                Assert.That(wc.Validate("X"), Is.True);
                Assert.That(wc.Validate("*"), Is.False);
                Assert.That(wc.Validate("?"), Is.False);
                Assert.That(wc.Validate("XX"), Is.True);
                Assert.That(wc.Validate("*X"), Is.False);
                Assert.That(wc.Validate("?X"), Is.False);
                Assert.That(wc.Validate("X*"), Is.False);
                Assert.That(wc.Validate("X?"), Is.False);
                Assert.That(wc.Validate("XXX"), Is.True);
                Assert.That(wc.Validate("X*X"), Is.False);
                Assert.That(wc.Validate("X?X"), Is.False);
                Assert.That(wc.Validate("*?*"), Is.False);
                Assert.That(wc.Validate("*X*"), Is.False);
            });
        }

        [Test]
        public void Validate_3_EndWildcardOnly()
        {
            var wc = new Wildcard(WildcardSelection.EndsWith | WildcardSelection.MultiWildcard | WildcardSelection.SingleWildcard, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.False);
                Assert.That(wc.Validate(string.Empty), Is.False);
                Assert.That(wc.Validate("X"), Is.False);
                Assert.That(wc.Validate("*"), Is.False);
                Assert.That(wc.Validate("?"), Is.False);
                Assert.That(wc.Validate("XX"), Is.False);
                Assert.That(wc.Validate("*X"), Is.True);
                Assert.That(wc.Validate("?X"), Is.True);
                Assert.That(wc.Validate("X*"), Is.False);
                Assert.That(wc.Validate("X?"), Is.False);
                Assert.That(wc.Validate("XXX"), Is.False);
                Assert.That(wc.Validate("X*X"), Is.False);
                Assert.That(wc.Validate("X?X"), Is.False);
                Assert.That(wc.Validate("*?*"), Is.False);
                Assert.That(wc.Validate("*X*"), Is.False);
            });
        }

        [Test]
        public void Validate_4_StartWildcardOnly()
        {
            var wc = new Wildcard(WildcardSelection.StartsWith | WildcardSelection.MultiWildcard | WildcardSelection.SingleWildcard, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.False);
                Assert.That(wc.Validate(string.Empty), Is.False);
                Assert.That(wc.Validate("X"), Is.False);
                Assert.That(wc.Validate("*"), Is.False);
                Assert.That(wc.Validate("?"), Is.False);
                Assert.That(wc.Validate("XX"), Is.False);
                Assert.That(wc.Validate("*X"), Is.False);
                Assert.That(wc.Validate("?X"), Is.False);
                Assert.That(wc.Validate("X*"), Is.True);
                Assert.That(wc.Validate("X?"), Is.True);
                Assert.That(wc.Validate("XXX"), Is.False);
                Assert.That(wc.Validate("X*X"), Is.False);
                Assert.That(wc.Validate("X?X"), Is.False);
                Assert.That(wc.Validate("*?*"), Is.False);
                Assert.That(wc.Validate("*X*"), Is.False);
            });
        }

        [Test]
        public void Validate_5_EmbeddedWildcardOnly()
        {
            var wc = new Wildcard(WildcardSelection.Embedded | WildcardSelection.MultiWildcard | WildcardSelection.SingleWildcard, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.False);
                Assert.That(wc.Validate(string.Empty), Is.False);
                Assert.That(wc.Validate("X"), Is.False);
                Assert.That(wc.Validate("*"), Is.False);
                Assert.That(wc.Validate("?"), Is.False);
                Assert.That(wc.Validate("XX"), Is.False);
                Assert.That(wc.Validate("*X"), Is.False);
                Assert.That(wc.Validate("?X"), Is.False);
                Assert.That(wc.Validate("X*"), Is.False);
                Assert.That(wc.Validate("X?"), Is.False);
                Assert.That(wc.Validate("XXX"), Is.False);
                Assert.That(wc.Validate("X*X"), Is.True);
                Assert.That(wc.Validate("X?X"), Is.True);
                Assert.That(wc.Validate("*?*"), Is.False);
                Assert.That(wc.Validate("*X*"), Is.False);
            });
        }

        [Test]
        public void Validate_6_SingleOrMultiWildcard()
        {
            var wc = new Wildcard(WildcardSelection.Embedded | WildcardSelection.MultiWildcard, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate("X*X"), Is.True);
                Assert.That(wc.Validate("X?X"), Is.False);
            });

            wc = new Wildcard(WildcardSelection.Embedded | WildcardSelection.SingleWildcard, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate("X*X"), Is.False);
                Assert.That(wc.Validate("X?X"), Is.True);
            });
        }

        [Test]
        public void Validate_7_NoneAndEqual()
        {
            var wc = new Wildcard(WildcardSelection.None | WildcardSelection.Equal, singleWildcard: Wildcard.SingleWildcardCharacter);
            Assert.Multiple(() =>
            {
                Assert.That(wc.Validate(null), Is.True);
                Assert.That(wc.Validate(string.Empty), Is.True);
                Assert.That(wc.Validate("X"), Is.True);
                Assert.That(wc.Validate("*"), Is.False);
                Assert.That(wc.Validate("?"), Is.False);
                Assert.That(wc.Validate("XX"), Is.True);
                Assert.That(wc.Validate("*X"), Is.False);
                Assert.That(wc.Validate("?X"), Is.False);
                Assert.That(wc.Validate("X*"), Is.False);
                Assert.That(wc.Validate("X?"), Is.False);
                Assert.That(wc.Validate("XXX"), Is.True);
                Assert.That(wc.Validate("X*X"), Is.False);
                Assert.That(wc.Validate("X?X"), Is.False);
                Assert.That(wc.Validate("*?*"), Is.False);
                Assert.That(wc.Validate("*X*"), Is.False);
            });
        }

        #endregion

        #region WhereWildcard

        private class Person
        {
            public string? First { get; set; }
            public string? Last { get; set; }
        }

        private List<Person> GetPeople()
        {
            return new List<Person>
            {
                new() { First = "Amy", Last = "Johnson" },
                new() { First = "Jenny", Last = "Smith" },
                new() { First = "Gerry", Last = "McQuire" },
                new() { First = "Gary", Last = "Lawson" },
                new() { First = "Simon", Last = "Reynolds" },
                new() { First = "Amanada", Last = "Gray" },
                new() { First = "B", Last = "P" },
                new() { First = null, Last = null }
            };
        }

        [Test]
        public void WhereWildcard_IEnumerableExtensions()
        {
            Assert.Multiple(() =>
            {
                // None (all).
                Assert.That(GetPeople().WhereWildcard(x => x.First, null).Select(x => x.Last).Count(), Is.EqualTo(8));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "").Select(x => x.Last).Count(), Is.EqualTo(8));

                // Equal.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "SIMON").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "SIMON", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Single (all).
                Assert.That(GetPeople().WhereWildcard(x => x.First, "*").Select(x => x.Last).Count(), Is.EqualTo(8));

                // Starts with.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "SI*").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "SI*", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Ends with.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "*ON").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "*ON", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Contains.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "*IM*").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "*IM*", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Regex-based: embedded.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "S*N").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "S*N", ignoreCase: false).Select(x => x.Last).SingleOrDefault(), Is.Null);

                // Regex-based: single-char match.
                Assert.That(GetPeople().WhereWildcard(x => x.First, "G?RY", wildcard: Wildcard.BothAll).Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Lawson"));
                Assert.That(GetPeople().WhereWildcard(x => x.First, "G?RY", ignoreCase: false, wildcard: Wildcard.BothAll).Select(x => x.Last).SingleOrDefault(), Is.Null);

                // Regex-based: single-char all.
                Assert.That(GetPeople().Where(x => true).WhereWildcard(x => x.First, " ? ", wildcard: new Wildcard(WildcardSelection.MultiAll, singleWildcard: char.MinValue)).Select(x => x.Last).SingleOrDefault(), Is.Null);
                Assert.That(GetPeople().Where(x => true).WhereWildcard(x => x.First, " ? ", wildcard: Wildcard.BothAll).Select(x => x.Last).SingleOrDefault(), Is.EqualTo("P"));
            });
        }

        [Test]
        public void WhereWildcard_IQueryableExtensions()
        {
            Assert.Multiple(() =>
            {
                // None(all).
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, null).Select(x => x.Last).Count(), Is.EqualTo(8));
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "").Select(x => x.Last).Count(), Is.EqualTo(8));

                // Equal.
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "SIMON").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "SIMON", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Single (all).
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "*").Select(x => x.Last).Count(), Is.EqualTo(8));

                // Starts with.
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "SI*").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "SI*", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Ends with.
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "*ON").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "*ON", ignoreCase: false).SingleOrDefault(), Is.Null);

                // Contains.
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "*IM*").Select(x => x.Last).SingleOrDefault(), Is.EqualTo("Reynolds"));
                Assert.That(GetPeople().AsQueryable().WhereWildcard(x => x.First, "*IM*", ignoreCase: false).SingleOrDefault(), Is.Null);
            });

            // Embedded.
            Assert.Throws<InvalidOperationException>(() => GetPeople().AsQueryable().WhereWildcard(x => x.First, "S*N").Select(x => x.Last).SingleOrDefault())!.Message.Should().Be("Wildcard selection text is not supported.");
            Assert.Throws<InvalidOperationException>(() => GetPeople().AsQueryable().WhereWildcard(x => x.First, "S*N", ignoreCase: false).Select(x => x.Last).SingleOrDefault())!.Message.Should().Be("Wildcard selection text is not supported.");

            Assert.Multiple(() =>
            {
                // Single-char all; '?' is ignored.
                Assert.That(GetPeople().AsQueryable().Where(x => true).WhereWildcard(x => x.First, " ? ", wildcard: new Wildcard(WildcardSelection.MultiAll, singleWildcard: char.MinValue)).Select(x => x.Last).SingleOrDefault(), Is.Null);
                Assert.That(GetPeople().AsQueryable().Where(x => true).WhereWildcard(x => x.First, " ? ", ignoreCase: false, wildcard: new Wildcard(WildcardSelection.MultiAll, singleWildcard: char.MinValue)).Select(x => x.Last).SingleOrDefault(), Is.Null);
            });
        }

        [Test]
        public void WhereWildcard_IQueryableExtensions_Load_Skippable()
        {
            var p = GetPeople();

            for (int i = 0; i < 100; i++)
            {
                p.AsQueryable().WhereWildcard(x => x.First, null).Select(x => x.Last).Count();
                p.AsQueryable().WhereWildcard(x => x.First, "").Select(x => x.Last).Count();
                p.AsQueryable().WhereWildcard(x => x.First, "*").Select(x => x.Last).Count();
            }
        }

        [Test]
        public void WhereWildcard_IQueryableExtensions_Load_WithExpressions()
        {
            var p = GetPeople();

            for (int i = 0; i < 100; i++)
            {
                p.AsQueryable().WhereWildcard(x => x.First, "SIMON").Select(x => x.Last).SingleOrDefault();
                p.AsQueryable().WhereWildcard(x => x.First, "SIMON", ignoreCase: false).SingleOrDefault();
                p.AsQueryable().WhereWildcard(x => x.First, "SI*").Select(x => x.Last).SingleOrDefault();
                p.AsQueryable().WhereWildcard(x => x.First, "*ON", ignoreCase: false).SingleOrDefault();
                p.AsQueryable().WhereWildcard(x => x.First, "*IM*").Select(x => x.Last).SingleOrDefault();
                p.AsQueryable().WhereWildcard(x => x.First, "*IM*", ignoreCase: false).SingleOrDefault();
                p.AsQueryable().Where(x => true).WhereWildcard(x => x.First, " ? ", wildcard: new Wildcard(WildcardSelection.MultiAll, singleWildcard: char.MinValue)).Select(x => x.Last).SingleOrDefault();
                p.AsQueryable().Where(x => true).WhereWildcard(x => x.First, " ? ", ignoreCase: false, wildcard: new Wildcard(WildcardSelection.MultiAll, singleWildcard: char.MinValue)).Select(x => x.Last).SingleOrDefault();
            }

            // Calc as t (time) / 100 (iterations) / 8 (queries) = avg per invocation cost :: this builds run-time expression to execute - super flexible but will not be uber fast.
        }

        #endregion
    }
}