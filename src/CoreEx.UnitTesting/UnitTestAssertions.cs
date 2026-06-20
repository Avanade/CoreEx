#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace AwesomeAssertions;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides custom <see cref="AwesomeAssertions"/> assertion classes for use in unit testing.
/// </summary>
public static class UnitTestAssertions
{
    /// <summary>
    /// Returns a <see cref="JsonNodeAssertions"/> for asserting on the <see cref="JsonNode"/> directly.
    /// </summary>
    /// <param name="node">The <see cref="JsonNode"/> subject.</param>
    /// <returns>A <see cref="JsonNodeAssertions"/> for fluent JSON-specific chaining.</returns>
    public static JsonNodeAssertions Should(this JsonNode node) => new(node, AssertionChain.GetOrCreate());

    /// <summary>
    /// Asserts that the string is valid JSON, enabling further JSON-specific assertions.
    /// </summary>
    /// <param name="assertions">The <see cref="StringAssertions"/>.</param>
    /// <param name="because">The reason the assertion should be satisfied.</param>
    /// <param name="becauseArgs">The <paramref name="because"/> format arguments.</param>
    /// <returns>A <see cref="JsonNodeAssertions"/> for fluent JSON-specific chaining.</returns>
    public static JsonNodeAssertions BeJson(this StringAssertions assertions, string because = "", params object[] becauseArgs)
    {
        var chain = assertions.CurrentAssertionChain;

        chain.ForCondition(assertions.Subject is not null)
            .BecauseOf(because, becauseArgs)
            .WithDefaultIdentifier("JSON string")
            .FailWith("Expected {context} to be valid JSON{reason}, but it was <null>.");

        JsonNode? node = null;
        string? parseError = null;
        var isValid = true;

        try
        {
            node = JsonNode.Parse(assertions.Subject!);
        }
        catch (JsonException ex)
        {
            isValid = false;
            parseError = ex.Message;
        }

        chain.ForCondition(isValid)
            .BecauseOf(because, becauseArgs)
            .WithDefaultIdentifier("string")
            .FailWith("Expected {context} to be valid JSON{reason}, but parsing failed: {0}.", parseError);

        return new JsonNodeAssertions(node!, chain);
    }

    /// <summary>
    /// Provides JSON-specific assertions for a <see cref="JsonNode"/> subject.
    /// </summary>
    public sealed class JsonNodeAssertions : ReferenceTypeAssertions<JsonNode, JsonNodeAssertions>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNodeAssertions"/> class.
        /// </summary>
        /// <param name="subject">The <see cref="JsonNode"/> subject.</param>
        /// <param name="assertionChain">The <see cref="AssertionChain"/>.</param>
        internal JsonNodeAssertions(JsonNode subject, AssertionChain assertionChain) : base(subject, assertionChain) { }

        /// <inheritdoc/>
        protected override string Identifier => "JSON";

        /// <summary>
        /// Asserts that the JSON contains all the specified JSON paths.
        /// </summary>
        /// <param name="paths">The JSON paths to match.</param>
        /// <param name="because">The reason the assertion should be satisfied.</param>
        /// <param name="becauseArgs">The <paramref name="because"/> format arguments.</param>
        /// <returns>The current <see cref="JsonNodeAssertions"/> to support fluent-style chaining.</returns>
        /// <remarks>The original <see cref="JsonNode"/> is cloned before matching to ensure the assertion does not modify the original JSON.</remarks>
        public JsonNodeAssertions ContainAll(IEnumerable<string> paths, string because = "", params object[] becauseArgs)
        {
            foreach (var path in paths)
            {
                var jn = JsonFilter.GetMatched(Subject.DeepClone(), path);

                CurrentAssertionChain.ForCondition(jn is not null)
                    .BecauseOf(because, becauseArgs)
                    .WithDefaultIdentifier("string")
                    .FailWith("Expected {context} path to exist{reason}: {0}", path);
            }

            return this;
        }

        /// <summary>
        /// Asserts that the JSON does not contain any of the specified JSON paths.
        /// </summary>
        /// <param name="paths">The JSON paths to match.</param>
        /// <param name="because">The reason the assertion should be satisfied.</param>
        /// <param name="becauseArgs">The <paramref name="because"/> format arguments.</param>
        /// <returns>The current <see cref="JsonNodeAssertions"/> to support fluent-style chaining.</returns>
        /// <remarks>The original <see cref="JsonNode"/> is cloned before matching to ensure the assertion does not modify the original JSON.</remarks>
        public JsonNodeAssertions NotContainAny(IEnumerable<string> paths, string because = "", params object[] becauseArgs)
        {
            foreach (var path in paths)
            {
                var jn = JsonFilter.GetMatched(Subject.DeepClone(), path);

                CurrentAssertionChain.ForCondition(jn is null)
                    .BecauseOf(because, becauseArgs)
                    .WithDefaultIdentifier("string")
                    .FailWith("Expected {context} path to not exist{reason}: {0}", path);
            }

            return this;
        }

        /// <summary>
        /// Asserts that the JSON contains the specified JSON path, returning the matching <see cref="JsonNode"/> for further chaining if desired.
        /// </summary>
        /// <param name="path">The JSON path to match.</param>
        /// <param name="because">The reason the assertion should be satisfied.</param>
        /// <param name="becauseArgs">The <paramref name="because"/> format arguments.</param>
        /// <returns>The <see cref="JsonNode"/> that matches the specified JSON path.</returns>
        /// <remarks>The original <see cref="JsonNode"/> is cloned before matching to ensure the assertion does not modify the original JSON.</remarks>
        public JsonNode HavePath(string path, string because = "", params object[] becauseArgs)
        {
            var jn = JsonFilter.GetMatched(Subject.DeepClone(), path);

            CurrentAssertionChain.ForCondition(jn is not null)
                .BecauseOf(because, becauseArgs)
                .WithDefaultIdentifier("string")
                .FailWith("Expected {context} path to exist{reason}");

            return jn!;
        }
    }
}