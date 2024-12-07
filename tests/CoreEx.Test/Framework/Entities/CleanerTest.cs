using CoreEx.Configuration;
using CoreEx.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreEx.Test.Framework.Entities
{
    [TestFixture, NonParallelizable]
    public class CleanerTest
    {
        [Test]
        public void DateTimeCleaning()
        {
            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeLocal;
            var dt = DateTime.UtcNow;
            var dtc = Cleaner.Clean(dt);
            Assert.That(dtc.Kind, Is.EqualTo(DateTimeKind.Local));

            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeUtc;
            dtc = Cleaner.Clean(dt);
            Assert.That(dtc.Kind, Is.EqualTo(DateTimeKind.Utc));

            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeLocal;
        }

        [Test]
        public void NullableDateTimeCleaning()
        {
            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeLocal;
            DateTime? dt = DateTime.UtcNow;
            DateTime? dtc = Cleaner.Clean(dt);
            Assert.That(dtc!.Value.Kind, Is.EqualTo(DateTimeKind.Local));

            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeUtc;
            dtc = Cleaner.Clean(dt);
            Assert.That(dtc!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));

            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeLocal;

            dt = null;
            dtc = Cleaner.Clean(dt);
            Assert.That(dtc, Is.Null);
        }

        [Test]
        public void DateTimeTransformFromSettings()
        {
            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeUtc;

            ConfigurationBuilder builder = new();
            Dictionary<string, string> testSettings = new()
            {
                {"CoreEx:Cleaner:DateTimeTransform", "DateTimeLocal"}
            };
            builder.AddInMemoryCollection(testSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(builder.Build())
                .AddDefaultSettings()
                .BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            using var ec = ExecutionContext.CreateNew();
            ec.ServiceProvider = scope.ServiceProvider;

            DateTime? dt = DateTime.UtcNow;
            DateTime? dtc = Cleaner.Clean(dt);
            Assert.Multiple(() =>
            {
                Assert.That(dtc!.Value.Kind, Is.EqualTo(DateTimeKind.Local));
                Assert.That(Cleaner.DefaultDateTimeTransform, Is.EqualTo(DateTimeTransform.DateTimeUtc));
            });
        }

        [Test]
        public void DateTimeTransformFromSettings_Load()
        {
            Cleaner.DefaultDateTimeTransform = DateTimeTransform.DateTimeUtc;

            ConfigurationBuilder builder = new();
            Dictionary<string, string> testSettings = new()
            {
                {"Cleaner:DateTimeTransform", "DateTimeLocal"}
            };
            builder.AddInMemoryCollection(testSettings);

            IServiceProvider serviceProvider = new ServiceCollection()
                .AddSingleton<IConfiguration>(builder.Build())
                .AddDefaultSettings()
                .BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            using var ec = ExecutionContext.CreateNew();
            ec.ServiceProvider = scope.ServiceProvider;

            DateTime? dtc = DateTime.UtcNow;
            for (int i = 0; i < 10000; i++)
            {
                dtc = Cleaner.Clean(DateTime.UtcNow);
                Assert.That(dtc!.Value.Kind, Is.EqualTo(DateTimeKind.Local), "Iteration" + i);
            }
        }

        [Test]
        public void StringTransformCleaning()
        {
            var s1 = "";
            var s2 = (string?)null;
            var s3 = "ABC";

            Assert.Multiple(() =>
            {
                Assert.That(Cleaner.Clean(s1), Is.Null);
                Assert.That(Cleaner.Clean(s2), Is.Null);
                Assert.That(Cleaner.Clean(s3), Is.EqualTo("ABC"));
            });

            Cleaner.DefaultStringTransform = StringTransform.NullToEmpty;
            Assert.Multiple(() =>
            {
                Assert.That(Cleaner.Clean(s1), Is.EqualTo(""));
                Assert.That(Cleaner.Clean(s2), Is.EqualTo(""));
                Assert.That(Cleaner.Clean(s3), Is.EqualTo("ABC"));
            });

            Cleaner.DefaultStringTransform = StringTransform.EmptyToNull;
        }

        [Test]
        public void StringTrimCleaning()
        {
            var s = " ABC ";
            Assert.That(Cleaner.Clean(s), Is.EqualTo(" ABC"));

            Cleaner.DefaultStringTrim = StringTrim.Both;
            Assert.That(Cleaner.Clean(s), Is.EqualTo("ABC"));

            Cleaner.DefaultStringTrim = StringTrim.End;
        }
    }
}