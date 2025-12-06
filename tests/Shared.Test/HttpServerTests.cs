using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Reflection;
using Shared.Config;
using Shared.Http;
using Xunit;

namespace Shared.Tests.Http
{
    public class HttpServerTests
    {
        private static void SetAppConfiguration(params (string Key, string Value)[] entries)
        {
            var sd = new StringDictionary();
            foreach (var (key, value) in entries)
            {
                sd[key] = value;
            }

            var field = typeof(Configuration).GetField("appConfiguration",
                BindingFlags.NonPublic | BindingFlags.Static);
            field!.SetValue(null, sd);
        }

        private class TestHttpServer : HttpServer
        {
            public bool InitCalled { get; private set; }
            public HttpListener Listener => server;
            public HttpRouter Router => router;

            public override void Init()
            {
                InitCalled = true;
            }
        }

        [Fact]
        public void Constructor_ConfiguresHttpListenerPrefixFromConfigurationAndCallsInit()
        {
            SetAppConfiguration(
                ("HOST", "http://localhost"),
                ("PORT", "12345")
            );

            var server = new TestHttpServer();

            Assert.True(server.InitCalled);

            var prefixes = server.Listener.Prefixes.Cast<string>().ToList();
            Assert.Single(prefixes);
            Assert.Equal("http://localhost:12345/", prefixes[0]);
        }

        [Fact]
        public void Stop_WhenNotListening_DoesNotThrow()
        {
            SetAppConfiguration();

            var server = new TestHttpServer();

            var ex = Record.Exception(() => server.Stop());

            Assert.Null(ex);
        }
    }
}
