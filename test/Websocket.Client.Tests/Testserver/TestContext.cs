﻿using System;
using Microsoft.AspNetCore.TestHost;
using Serilog;
using Serilog.Events;
using Xunit.Abstractions;

namespace Websocket.Client.Tests.TestServer
{
    public class TestContext<TStartup> where TStartup : class
    {
        private readonly TestServerApplicationFactory<TStartup> _factory;

        public TestContext(ITestOutputHelper output)
        {
            _factory = new TestServerApplicationFactory<TStartup>();
            InitLogging(output);
        }

        public WebSocketClient NativeTestClient { get; set; }

        public Uri InvalidUri { get; } = new("wss://invalid-url.local");

        public IWebsocketClient CreateClient()
        {
            var httpClient = _factory.CreateClient(); // This is needed since _factory.Server would otherwise be null
            return CreateClient(_factory.Server.BaseAddress);
        }

        public IWebsocketClient CreateClient(Uri serverUrl)
        {
            var wsUri = new UriBuilder(serverUrl)
            {
                Scheme = "ws",
                Path = "ws"
            }.Uri;
            return new WebsocketClient(wsUri,
                async (uri, token) =>
                {
                    if (_factory.Server == null)
                    {
                        throw new InvalidOperationException("Connection to websocket server failed, check url");
                    }

                    if (uri == InvalidUri)
                    {
                        throw new InvalidOperationException("Connection to websocket server failed, check url");
                    }

                    NativeTestClient = _factory.Server.CreateWebSocketClient();
                    var ws = await NativeTestClient.ConnectAsync(uri, token).ConfigureAwait(false);
                    //await Task.Delay(1000, token);
                    return ws;
                });
        }

        public IWebsocketClient CreateInvalidClient(Uri serverUrl)
        {
            var wsUri = new UriBuilder(serverUrl)
            {
                Scheme = "ws",
                Path = "ws"
            }.Uri;
            return new WebsocketClient(wsUri,
                (uri, token) => throw new InvalidOperationException("Connection to websocket server failed, check url"));
        }

        private void InitLogging(ITestOutputHelper output)
        {
            if (output == null)
                return;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(output, LogEventLevel.Verbose)
                .CreateLogger();
        }
    }
}
