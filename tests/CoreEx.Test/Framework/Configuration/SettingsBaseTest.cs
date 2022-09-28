using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreEx.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CoreEx.Test.Framework.Configuration
{
    [TestFixture, NonParallelizable]
    public class SettingsBaseTest
    {
        public class SettingsForTesting : SettingsBase
        {
            public SettingsForTesting(IConfiguration configuration, params string[] prefixes) : base(configuration, prefixes)
            {
            }

            public string PropTest => GetValue<string>();

            public string PropTest2 => "some hardcoded value";

            public string SomethingGlobal => GetValue<string>();
            public string PropTestNested => GetValue<string>(defaultValue: SomethingGlobal);

        }

        private IConfiguration CreateTestConfiguration()
        {
            Environment.SetEnvironmentVariable("this_is_a_unittest_underscore__key", "underscoreValue");
            ConfigurationBuilder builder = new ConfigurationBuilder();
            Dictionary<string, string> testSettings = new Dictionary<string, string>()
            {
                {"SomethingGlobal", "foo"},
                {"prefix1/key1", "value1"},
                {"prefix2/key2", "value2"},
                {"common/key2", "commonValue2"},
                {"very/custom/prefix/key3", "value3"},
                {"key2", "globalValue2"}
            };
            builder.AddInMemoryCollection(testSettings);
            return builder.AddEnvironmentVariables("this_is_a_unittest_").Build();
        }

        [Test]
        public void CommonSettings_Should_Not_ThrowException_When_ConfigurationNull()
        {
            new SettingsForTesting(null!);
        }

        [Test]
        public void CommonSettings_Should_ThrowException_When_PrefixesNull()
        {
            // Arrange
            var configuration = CreateTestConfiguration();

            // Act
            Action act = () => new SettingsForTesting(configuration, prefixes: null!);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void CommonSettings_Should_ThrowException_When_PrefixIsNullOrEmpty()
        {
            // Arrange
            var configuration = CreateTestConfiguration();
            var prefixes = new string[] { "foo", string.Empty };

            // Act
            Action act = () => new SettingsForTesting(configuration, prefixes);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Test]
        public void GetValue_Should_Return_Default_Value_When_Key_Not_Found()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "prefix2/", "Common/" });

            var result = settings.GetValue<string>("key2", "foo");

            // Assert
            result.Should().Be("value2", because: "First matched prefix is returned, Sample prefix takes precedence");
        }

        [Test]
        public void GetValueByFullKey_Should_Return_Value_When_Key_Found()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("very/custom/prefix/key3");

            // Assert
            result.Should().Be("value3");
        }

        [Test]
        public void GetValueByFullKey_Should_Return_Value_When_Key_FoundWithoutPrefix()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("SomethingGlobal");

            // Assert
            result.Should().Be("foo");
        }

        [Test]
        public void GetValueByFullKey_Should_Return_Default_When_Key_NotFound()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<bool>("very/custom/notfound", true);

            // Assert
            result.Should().Be(true);
        }

        [Test]
        public void GetValue_Should_Return_LastPrefixMatched_Value()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<bool>("very/custom/notfound", true);

            // Assert
            result.Should().Be(true);
        }

        [Test]
        public void GetValue_Should_Return_Value_When_KeysWithDoubleUnderscores()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("underscore__key");

            // Assert
            result.Should().Be("underscoreValue");
        }

        [Test]
        public void GetValue_Should_Return_Value_When_SemicolonKeyUsedForDoubleUnderscore()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("underscore:key");

            // Assert
            result.Should().Be("underscoreValue");
        }


        [Test]
        public void GetValue_Should_Return_Value_From_Property_When_Class_Has_it()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("PropTest2");

            // Assert
            result.Should().Be("some hardcoded value");
        }

        [Test]
        public void GetValue_Should_Return_Value_From_NestedProperty_When_Class_Has_it()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var result = settings.GetValue<string>("PropTestNested");

            // Assert
            settings.SomethingGlobal.Should().Be("foo");
            result.Should().Be("foo");
        }

        // 50 * 5 * 100 ms = 25 seconds
        [Test, Repeat(50)]
        public void RunInParallel()
        {
            var configuration = CreateTestConfiguration();
            var settings = new SettingsForTesting(configuration, new string[] { "Sample/", "Common/" });

            var reps = Enumerable.Range(1, 50);

            var testResult = Parallel.ForEach(reps, (i) =>
            {
                var result = settings.GetValue<string>("PropTestNested");

                // Assert
                settings.SomethingGlobal.Should().Be("foo");
                result.Should().Be("foo");

                ExamineValuesOfLocalObjectsEitherSideOfAwait(settings).Wait();
            });

            testResult.IsCompleted.Should().BeTrue();
        }

        static async Task ExamineValuesOfLocalObjectsEitherSideOfAwait(SettingsForTesting settings)
        {
            var result = settings.GetValue<string>("PropTestNested");
            settings.SomethingGlobal.Should().Be("foo");
            result.Should().Be("foo");

            await Task.Delay(100);

            result = settings.GetValue<string>("PropTestNested");
            settings.SomethingGlobal.Should().Be("foo");
            result.Should().Be("foo");
        }

    }
}