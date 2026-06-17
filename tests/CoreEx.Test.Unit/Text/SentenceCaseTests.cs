using CoreEx.Text;

namespace CoreEx.Test.Unit.Text;

[TestFixture]
public class SentenceCaseTests
{
    [TearDown]
    public void TearDown()
    {
        // Reset static state after each test to avoid side effects.
        SentenceCase.Substitutions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { "Id", "Identifier" }, { "Etag", "ETag" } };
        SentenceCase.LastWordRemovals = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Id" };
        SentenceCase.SentenceCaseConverter = SentenceCase.SentenceCaseConversion;
    }

    #region ToPascalCase Tests

    [TestCase("EmployeeId", "EmployeeId")]           // Already PascalCase
    [TestCase("employeeId", "EmployeeId")]           // camelCase
    [TestCase("employee_id", "EmployeeId")]          // snake_case
    [TestCase("employee-id", "EmployeeId")]          // kebab-case
    [TestCase("Employee Id", "EmployeeId")]          // space-separated
    [TestCase("EMPLOYEE_ID", "EmployeeId")]          // ALL CAPS
    [TestCase("XMLParser", "XmlParser")]             // Acronym
    [TestCase("xml_parser", "XmlParser")]            // Acronym from snake
    [TestCase("VarNameDB", "VarNameDb")]             // Trailing acronym
    public void ToPascalCase_ValidInputs_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToPascalCase(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToPascalCase_NullOrEmpty_ReturnsSame(string? input)
    {
        SentenceCase.ToPascalCase(input).Should().Be(input);
    }

    [TestCase("a", "A")]
    [TestCase("A", "A")]
    [TestCase("x", "X")]
    public void ToPascalCase_SingleCharacter_ReturnsUppercase(string input, string expected)
    {
        SentenceCase.ToPascalCase(input).Should().Be(expected);
    }

    [TestCase("Employee", "Employee")]
    [TestCase("employee", "Employee")]
    [TestCase("EMPLOYEE", "Employee")]
    public void ToPascalCase_SingleWord_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToPascalCase(input).Should().Be(expected);
    }

    #endregion

    #region ToCamelCase Tests

    [TestCase("EmployeeId", "employeeId")]
    [TestCase("employeeId", "employeeId")]           // Already camelCase
    [TestCase("employee_id", "employeeId")]
    [TestCase("employee-id", "employeeId")]
    [TestCase("Employee Id", "employeeId")]
    [TestCase("XMLParser", "xmlParser")]
    [TestCase("VarNameDB", "varNameDb")]
    public void ToCamelCase_ValidInputs_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToCamelCase(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToCamelCase_NullOrEmpty_ReturnsSame(string? input)
    {
        SentenceCase.ToCamelCase(input).Should().Be(input);
    }

    #endregion

    #region ToKebabCase Tests

    [TestCase("EmployeeId", "employee-id")]
    [TestCase("employeeId", "employee-id")]
    [TestCase("employee_id", "employee-id")]
    [TestCase("employee-id", "employee-id")]         // Already kebab-case
    [TestCase("Employee Id", "employee-id")]
    [TestCase("XMLParser", "xml-parser")]
    [TestCase("VarNameDB", "var-name-db")]
    public void ToKebabCase_ValidInputs_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToKebabCase(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToKebabCase_NullOrEmpty_ReturnsSame(string? input)
    {
        SentenceCase.ToKebabCase(input).Should().Be(input);
    }

    #endregion

    #region ToSnakeCase Tests

    [TestCase("EmployeeId", "employee_id")]
    [TestCase("employeeId", "employee_id")]
    [TestCase("employee_id", "employee_id")]         // Already snake_case
    [TestCase("employee-id", "employee_id")]
    [TestCase("Employee Id", "employee_id")]
    [TestCase("XMLParser", "xml_parser")]
    [TestCase("VarNameDB", "var_name_db")]
    public void ToSnakeCase_ValidInputs_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToSnakeCase(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToSnakeCase_NullOrEmpty_ReturnsSame(string? input)
    {
        SentenceCase.ToSnakeCase(input).Should().Be(input);
    }

    #endregion

    #region SplitIntoWords Tests

    [Test]
    public void SplitIntoWords_PascalCase_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("EmployeeId").Should().Equal("Employee", "Id");
    }

    [Test]
    public void SplitIntoWords_CamelCase_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("employeeId").Should().Equal("employee", "Id");
    }

    [Test]
    public void SplitIntoWords_SnakeCase_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("employee_id").Should().Equal("employee", "id");
    }

    [Test]
    public void SplitIntoWords_KebabCase_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("employee-id").Should().Equal("employee", "id");
    }

    [Test]
    public void SplitIntoWords_SpaceSeparated_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("Employee Id").Should().Equal("Employee", "Id");
    }

    [Test]
    public void SplitIntoWords_Acronyms_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("XMLParser").Should().Equal("XML", "Parser");
        SentenceCase.SplitIntoWords("HTMLElement").Should().Equal("HTML", "Element");
        SentenceCase.SplitIntoWords("IOStream").Should().Equal("IO", "Stream");
        SentenceCase.SplitIntoWords("DBConnection").Should().Equal("DB", "Connection");
        SentenceCase.SplitIntoWords("HTTPSConnection").Should().Equal("HTTPS", "Connection");
    }

    [Test]
    public void SplitIntoWords_TrailingAcronym_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("VarNameDB").Should().Equal("Var", "Name", "DB");
    }

    [Test]
    public void SplitIntoWords_ConsecutiveDelimiters_IgnoresEmpty()
    {
        SentenceCase.SplitIntoWords("employee__id").Should().Equal("employee", "id");
        SentenceCase.SplitIntoWords("employee--id").Should().Equal("employee", "id");
        SentenceCase.SplitIntoWords("employee  id").Should().Equal("employee", "id");
    }

    [Test]
    public void SplitIntoWords_LeadingDelimiter_IgnoresLeading()
    {
        SentenceCase.SplitIntoWords("_employee").Should().Equal("employee");
        SentenceCase.SplitIntoWords("-employee").Should().Equal("employee");
        SentenceCase.SplitIntoWords(" employee").Should().Equal("employee");
    }

    [Test]
    public void SplitIntoWords_TrailingDelimiter_IgnoresTrailing()
    {
        SentenceCase.SplitIntoWords("employee_").Should().Equal("employee");
        SentenceCase.SplitIntoWords("employee-").Should().Equal("employee");
        SentenceCase.SplitIntoWords("employee ").Should().Equal("employee");
    }

    [Test]
    public void SplitIntoWords_LeadingAndTrailingDelimiters_IgnoresBoth()
    {
        SentenceCase.SplitIntoWords("_employee_id_").Should().Equal("employee", "id");
    }

    [Test]
    public void SplitIntoWords_OnlyDelimiters_ReturnsEmpty()
    {
        SentenceCase.SplitIntoWords("___").Should().BeEmpty();
        SentenceCase.SplitIntoWords("---").Should().BeEmpty();
        SentenceCase.SplitIntoWords("   ").Should().BeEmpty();
    }

    [TestCase(null)]
    [TestCase("")]
    public void SplitIntoWords_NullOrEmpty_ReturnsEmpty(string? input)
    {
        SentenceCase.SplitIntoWords(input).Should().BeEmpty();
    }

    [Test]
    public void SplitIntoWords_SingleWord_ReturnsSingleElement()
    {
        SentenceCase.SplitIntoWords("Employee").Should().Equal("Employee");
        SentenceCase.SplitIntoWords("employee").Should().Equal("employee");
    }

    [Test]
    public void SplitIntoWords_SingleCharacter_ReturnsSingleElement()
    {
        SentenceCase.SplitIntoWords("a").Should().Equal("a");
        SentenceCase.SplitIntoWords("A").Should().Equal("A");
    }

    [Test]
    public void SplitIntoWords_MixedFormats_ReturnsWords()
    {
        SentenceCase.SplitIntoWords("employee_IdValue").Should().Equal("employee", "Id", "Value");
        SentenceCase.SplitIntoWords("get-UserName").Should().Equal("get", "User", "Name");
        SentenceCase.SplitIntoWords("HTTP_Response").Should().Equal("HTTP", "Response");
    }

    #endregion

    #region ToSentenceCase Tests

    [TestCase("EmployeeId", "Employee")]             // Last word removal
    [TestCase("ProductName", "Product name")]        // Mixed case
    [TestCase("XMLParser", "XML parser")]            // Acronym
    [TestCase("employee_id", "Employee")]            // From snake_case
    [TestCase("VarNameDB", "Var name DB")]           // From docs example
    [TestCase("eTag", "ETag")]                       // ETag substitution
    [TestCase("etag", "ETag")]                       // ETag substitution
    public void ToSentenceCase_ValidInputs_ReturnsExpected(string input, string expected)
    {
        SentenceCase.ToSentenceCase(input).Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    public void ToSentenceCase_NullOrEmpty_ReturnsSame(string? input)
    {
        SentenceCase.ToSentenceCase(input).Should().Be(input);
    }

    [Test]
    public void ToSentenceCase_WithSubstitutions_AppliesCorrectly()
    {
        var original = SentenceCase.Substitutions;
        try
        {
            SentenceCase.Substitutions = new() { { "Id", "Identifier" }, { "DB", "Database" } };
            SentenceCase.ToSentenceCase("EmployeeIdCode").Should().Be("Employee identifier code");
            SentenceCase.ToSentenceCase("VarNameDB").Should().Be("Var name database");
        }
        finally
        {
            SentenceCase.Substitutions = original;
        }
    }

    [Test]
    public void ToSentenceCase_WithLastWordRemovals_RemovesCorrectly()
    {
        var original = SentenceCase.LastWordRemovals;
        try
        {
            SentenceCase.LastWordRemovals = ["Id", "Key"];
            SentenceCase.ToSentenceCase("EmployeeId").Should().Be("Employee");
            SentenceCase.ToSentenceCase("ProductKey").Should().Be("Product");
            SentenceCase.ToSentenceCase("ProductName").Should().Be("Product name");
        }
        finally
        {
            SentenceCase.LastWordRemovals = original;
        }
    }

    [Test]
    public void ToSentenceCase_UsesConverter()
    {
        SentenceCase.SentenceCaseConverter = s => "fixed";
        SentenceCase.ToSentenceCase("anything").Should().Be("fixed");
    }

    [Test]
    public void ToSentenceCase_NullConverter_ReturnsInput()
    {
        SentenceCase.SentenceCaseConverter = null;
        SentenceCase.ToSentenceCase("abcDEF").Should().Be("abcDEF");
    }

    #endregion

    #region All Formats Conversion Tests

    [Test]
    public void AllFormats_EmployeeId_ConvertsCorrectly()
    {
        const string input = "EmployeeId";

        SentenceCase.ToPascalCase(input).Should().Be("EmployeeId");
        SentenceCase.ToCamelCase(input).Should().Be("employeeId");
        SentenceCase.ToKebabCase(input).Should().Be("employee-id");
        SentenceCase.ToSnakeCase(input).Should().Be("employee_id");
        SentenceCase.ToSentenceCase(input).Should().Be("Employee");  // Last word removed
    }

    [Test]
    public void AllFormats_XMLParser_ConvertsCorrectly()
    {
        const string input = "XMLParser";

        SentenceCase.ToPascalCase(input).Should().Be("XmlParser");
        SentenceCase.ToCamelCase(input).Should().Be("xmlParser");
        SentenceCase.ToKebabCase(input).Should().Be("xml-parser");
        SentenceCase.ToSnakeCase(input).Should().Be("xml_parser");
        SentenceCase.ToSentenceCase(input).Should().Be("XML parser");
    }

    [Test]
    public void AllFormats_FromSnakeCase_ConvertsCorrectly()
    {
        const string input = "employee_id";

        SentenceCase.ToPascalCase(input).Should().Be("EmployeeId");
        SentenceCase.ToCamelCase(input).Should().Be("employeeId");
        SentenceCase.ToKebabCase(input).Should().Be("employee-id");
        SentenceCase.ToSnakeCase(input).Should().Be("employee_id");
        SentenceCase.ToSentenceCase(input).Should().Be("Employee");
    }

    [Test]
    public void AllFormats_FromKebabCase_ConvertsCorrectly()
    {
        const string input = "employee-id";

        SentenceCase.ToPascalCase(input).Should().Be("EmployeeId");
        SentenceCase.ToCamelCase(input).Should().Be("employeeId");
        SentenceCase.ToKebabCase(input).Should().Be("employee-id");
        SentenceCase.ToSnakeCase(input).Should().Be("employee_id");
        SentenceCase.ToSentenceCase(input).Should().Be("Employee");
    }

    #endregion
}