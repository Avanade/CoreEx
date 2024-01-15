
using Microsoft.Extensions.Configuration;
using SolaceSystems.Solclient.Messaging;

namespace CoreEx.Test.TestFunction
{
    [TestFixture]
    [Category("WithSolace")]
    public class PubSubOrchestratedTest
    {
        // NOTE: PubSub local instance must be running in container for test to execute
        private static IContext? _solaceContext;

        [Test]
        public void PubSubSend_Success()
        {
            // Arrange
            PubSubSender sender = SetupSender();
            var events = new List<EventSendData>
            {
                new EventSendData { Id = "123", Subject = "my.Product", Data = new BinaryData(Encoding.UTF8.GetBytes("Test Message1")), Destination = "try-me" },
                new EventSendData { Id = "124", Subject = "my.Product", Data = new BinaryData(Encoding.UTF8.GetBytes("Test Message2")), Destination = "try-me" }
            };

            // Act
            sender.SendAsync(events).Wait();
        }

        [Test]
        public void PubSubSend_GreaterThan50Events_OnlySendsBatchOf50()
        {
            // Arrange
            PubSubSender sender = SetupSender();
            var events = new List<EventSendData>();
            //var dataPayload1 = new BinaryData(Encoding.UTF8.GetBytes("Test Message1"));
            //var dataPayload2 = new BinaryData(Encoding.UTF8.GetBytes("Test Message2"));
            // build 51 events
            for (int i = 200; i < 251; i++)
            {
                events.Add(new EventSendData { Id = i.ToString(), Subject = "my.Product", Data = new BinaryData(Encoding.UTF8.GetBytes($"Test Message{i}")), Destination = "try-me" });
            }

            // Act
            sender.SendAsync(events).Wait();
        }

        [Test]
        public void PubSubSend_NoDestination_SendstoDefaultTopic()
        {
            // Arrange
            PubSubSender sender = SetupSender();
            var events = new List<EventSendData>
            {
                new EventSendData { Id = "123", Subject = "my.Product", Data = new BinaryData(Encoding.UTF8.GetBytes("Test Message1")) },
                new EventSendData { Id = "124", Subject = "my.Product", Data = new BinaryData(Encoding.UTF8.GetBytes("Test Message2")) }
            };

            // Act
            sender.SendAsync(events).Wait();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _solaceContext?.Dispose();
        }

        private static PubSubSender SetupSender()
        {
            var logger = GetLogger<PubSubSender>();
            var convertor = new EventSendDataToPubSubConverter();
            var sessionProperties = new SessionProperties
            {
                Host = "ws://localhost:8008",
                VPNName = "default",
                UserName = "default",
                Password = "default"
            };

            var config = new ConfigurationBuilder().SetBasePath(Environment.CurrentDirectory).AddJsonFile("appsettings.unittest.json");
            var testSettings = new DefaultSettings(config.Build());

            if (_solaceContext == null)
            {
                var cfp = new ContextFactoryProperties { SolClientLogLevel = SolLogLevel.Warning };
                cfp.LogToConsoleError();
                ContextFactory.Instance.Init(cfp);
                _solaceContext = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
            }

            var sender = new PubSubSender(_solaceContext, sessionProperties, testSettings, logger, null, convertor);
            return sender;
        }

        /// <summary>
        /// Gets a console <see cref="ILogger"/>.
        /// </summary>
        /// <typeparam name="T">The logger <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="ILogger"/>.</returns>
        private static ILogger<T> GetLogger<T>() => LoggerFactory.Create(b =>
        {
            b.SetMinimumLevel(LogLevel.Trace);
            b.ClearProviders();
            b.AddConsole();
        }).CreateLogger<T>();
    }
}