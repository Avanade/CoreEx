using CoreEx.Data;
using NUnit.Framework;
using System;

namespace CoreEx.Test.Framework.Data
{
    [TestFixture]
    public class QueryFilterParserTest
    {
        private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
            .WithFilter(filter => filter
                .AddField<string>("LastName", c => c.SupportKinds(QueryFilterTokenKind.AllStringOperators).AlsoCheckNotNull())
                .AddField<string>("FirstName", c => c.SupportKinds(QueryFilterTokenKind.AllStringOperators).UseUpperCase())
                .AddField<string>("Code")
                .AddField<DateTime>("Birthday", "BirthDate")
                .AddField<int>("Age")
                .AddField<decimal>("Salary")
                .AddField<bool>("IsOld"))
            .WithDefaultOrderBy("LastName, FirstName");

        private void AssertFilter(string filter, string expected, params object[] expectedArgs)
        {
            var result = _queryConfig.FilterParser.Parse(filter);
            Assert.Multiple(() =>
            {
                Assert.That(result.FilterBuilder.ToString(), Is.EqualTo(expected));
                Assert.That(result.Args, Is.EquivalentTo(expectedArgs));
            });
        }

        private void AssertException(string filter, string expected)
        {
            var ex = Assert.Throws<QueryFilterParserException>(() => _queryConfig.FilterParser.Parse(filter));
            Assert.That(ex.Message, Does.StartWith($"Filter is invalid: {expected}"));
        }

        [Test]
        public void Parse_SimpleValid()
        {
            AssertFilter("lastname eq 'Smith'", "(LastName != null && LastName == @0)", "Smith");
            AssertFilter("lastname eq null", "LastName == null");
            AssertFilter("firstname eq 'Angela'", "FirstName.ToUpper() == @0", "ANGELA");
            AssertFilter("code eq 'Xyz'", "Code == @0", "Xyz");
            AssertFilter("birthday eq 1980-01-01", "BirthDate == @0", new DateTime(1980, 1, 1));
            AssertFilter("birthday ne 1980-01-01", "BirthDate != @0", new DateTime(1980, 1, 1));
            AssertFilter("age lt 100", "Age < @0", 100);
            AssertFilter("age le 100", "Age <= @0", 100);
            AssertFilter("age gt 100", "Age > @0", 100);
            AssertFilter("age ge 100", "Age >= @0", 100);
            AssertFilter("salary gt 1036.42", "Salary > @0", 1036.42);
            AssertFilter("isold eq true", "IsOld == true");
            AssertFilter("IsOld ne false", "IsOld != false");
            AssertFilter("ISOLD ne null", "IsOld != null");
            AssertFilter("isold", "IsOld");
        }

        [Test]
        public void Parse_In()
        {
            AssertFilter("code in ('abc', 'def')", "Code in (@0, @1)", "abc", "def");
            AssertFilter("age in (20, 30, 40)", "Age in (@0, @1, @2)", 20, 30, 40);
            AssertFilter("age in (20)", "Age in (@0)", 20);

            AssertException("code in", "The final expression is incomplete.");
            AssertException("code in ()", "Field 'code' constant must be specified before the closing ')' for the 'in' operator.");
            AssertException("code in (null)", "Field 'code' constant must not be null for an 'in' operator.");
            AssertException("code in ))", "Field 'code' must specify an opening '(' for the 'in' operator.");
            AssertException("code in ((", "Field 'code' must close ')' the 'in' operator before specifying a further open '('.");
            AssertException("code in (,)", "Field 'code' constant ',' is not considered valid.");
            AssertException("age in (1 2)", "Field 'age' expects a ',' separator between constant values for an 'in' operator.");
        }

        [Test]
        public void Parse_ComplexValid()
        {
            AssertFilter("(age eq 1 or age eq 2) and isold eq true", "(Age == @0 || Age == @1) && IsOld == true", 1, 2);
            AssertFilter("(age  eq  1  or  age  eq  2 ) and isold    ", "(Age == @0 || Age == @1) && IsOld", 1, 2);
            AssertFilter("(age eq 1 or age eq 2) or (age eq 8 or age eq 9)", "(Age == @0 || Age == @1) || (Age == @2 || Age == @3)", 1, 2, 8, 9);
            AssertFilter("((age eq 1 or age eq 2) or (age eq 8 or age eq 9))", "((Age == @0 || Age == @1) || (Age == @2 || Age == @3))", 1, 2, 8, 9);
        }

        [Test]
        public void Parse_Invalid()
        {
            AssertException("banana", "Field 'banana' is not supported.");
            AssertException("banana eq", "Field 'banana' is not supported.");
            AssertException("age apple", "Field 'age' does not support 'apple' as an operator.");
            AssertException("age 'apple'", "Field 'age' does not support ''apple'' as an operator.");
            AssertException("age eq 'apple'", "Field 'age' constant 'apple' must not be specified as a Literal where the underlying type is not a string.");
            AssertException("age eq 1990-01-01", "Field 'age' has a Value '1990-01-01' that is not a valid Int32.");
            AssertException("null eq null", "There is a 'null' positioning that is syntactically incorrect.");
            AssertException("true eq null", "There is a 'true' positioning that is syntactically incorrect.");
            AssertException("false eq null", "There is a 'false' positioning that is syntactically incorrect.");
            AssertException("and", "There is a 'and' positioning that is syntactically incorrect.");
            AssertException("or", "There is a 'or' positioning that is syntactically incorrect.");
            AssertException("and age eq 1", "There is a 'and' positioning that is syntactically incorrect.");
            AssertException("or age eq 1", "There is a 'or' positioning that is syntactically incorrect.");
            AssertException("age eq 1 and", "The final expression is incomplete.");
            AssertException("age eq 1 or", "The final expression is incomplete.");
            AssertException("isold ge true", "Field 'isold' does not support the 'ge' operator.");
            AssertException("age xx 1", "Field 'age' does not support 'xx' as an operator.");
            AssertException("age ge null", "Field 'age' constant must not be null for an 'ge' operator.");

            AssertException("(age eq 1", "There is an opening '(' that has no matching closing ')'.");
            AssertException("age eq 1)", "There is a closing ')' that has no matching opening '('.");
            AssertException("age ( 1", "Field 'age' does not support '(' as an operator.");
            AssertException("age eq (", "Field 'age' constant '(' is not considered valid.");
            AssertException("age eq )", "Field 'age' constant ')' is not considered valid.");
        }

        [Test]
        public void Parse_Literals()
        {
            AssertException("code eq '", "A Literal has not been terminated.");
            AssertException("code eq '''", "A Literal has not been terminated.");
            AssertException("code eq '''''", "A Literal has not been terminated.");

            AssertFilter("code eq ''", "Code == @0", string.Empty);
            AssertFilter("code eq ''''", "Code == @0", "'");
            AssertFilter("code eq 'x''x'", "Code == @0", "x'x");
            AssertFilter("code eq 'x'''", "Code == @0", "x'");
            AssertFilter("code eq '''x'", "Code == @0", "'x");
            AssertFilter("code eq '''x'''", "Code == @0", "'x'");

            AssertFilter("code eq 'null'", "Code == @0", "null");

            AssertException("code eq 1", "Field 'code' constant '1' must be specified as a Literal where the underlying type is a string.");
            AssertException("age eq '8'", "Field 'age' constant '8' must not be specified as a Literal where the underlying type is not a string.");
        }

        [Test]
        public void Parse_StringFunction()
        {
            AssertFilter("startswith(firstName, 'abc')", "FirstName.ToUpper().StartsWith(@0)", "ABC");
            AssertFilter("endswith(firstName, 'abc')", "FirstName.ToUpper().EndsWith(@0)", "ABC");
            AssertFilter("contains(firstName, 'abc')", "FirstName.ToUpper().Contains(@0)", "ABC");
            AssertFilter("contains(lastname, 'xyz')", "(LastName != null && LastName.Contains(@0))", "xyz");

            AssertException("startswith(code, 'abc')", "Field 'code' does not support the 'startswith' function.");
            AssertException("startswith)code, 'abc')", "A 'startswith' function expects an opening '(' not a ')'.");
            AssertException("startswith(firstname( 'abc')", "A 'startswith' function expects a ',' separator between the field and its constant.");
            AssertException("startswith(firstname, null)", "A 'startswith' function references a null constant which is not supported.");
            AssertException("startswith(firstname, 'abc',", "A 'startswith' function expects a closing ')' not a ','.");
        }

        [Test]
        public void Parse_Not()
        {
            AssertFilter("not (age eq 1)", "!(Age == @0)", 1);
            AssertFilter("age eq 1 and not (age eq 2)", "Age == @0 && !(Age == @1)", 1, 2);

            AssertException("age eq 1 and not age eq 2", "A 'not' expects an opening '(' to start an expression versus a syntactically incorrect 'age' token.");
            AssertException("age  eq  1  not", "There is a 'not' positioning that is syntactically incorrect.");
        }
    }
}