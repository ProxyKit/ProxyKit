// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ProxyKit
{
    public class ProxyTests
    {
        private TestMessageHandler _testMessageHandler;
        private readonly IWebHostBuilder _builder;

        public ProxyTests()
        {
            _testMessageHandler = new TestMessageHandler
            {
                Sender = req =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.Created);
                    response.Headers.Add("testHeader", "testHeaderValue");
                    response.Content = new StringContent("Response Body");
                    return response;
                }
            };

            _builder = new WebHostBuilder()
                .ConfigureServices(services => services.AddProxy(httpClientBuilder =>
                {
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => _testMessageHandler);
                }));
        }


        [Theory]
        [InlineData("GET", 3001)]
        [InlineData("HEAD", 3002)]
        [InlineData("TRACE", 3003)]
        [InlineData("DELETE", 3004)]
        public async Task PassthroughRequestsWithoutBodyWithResponseHeaders(string methodType, int port)
        {
            _builder.Configure(app => app.RunProxy(context =>
            {
                var forwardContext = context.ForwardTo($"http://localhost:{port}");
                return forwardContext.Send();
            }));
            var server = new TestServer(_builder);

            var requestMessage = new HttpRequestMessage(new HttpMethod(methodType), "");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();
            responseMessage.Headers.TryGetValues("testHeader", out var testHeaderValue);

            responseMessage.StatusCode.ShouldBe(HttpStatusCode.Created);
            responseContent.ShouldBe("Response Body");
            testHeaderValue.SingleOrDefault().ShouldBe("testHeaderValue");

            var sentRequest = _testMessageHandler.SentRequestMessages.Single();
            sentRequest.ShouldSatisfyAllConditions(
                () =>
                {
                    sentRequest.Headers.TryGetValues("Host", out var hostValue);
                    hostValue.Single().ShouldBe("localhost:" + port);
                },
                () => sentRequest.RequestUri.ToString().ShouldBe("http://localhost:" + port + "/"),
                () => sentRequest.Method.ShouldBe(new HttpMethod(methodType))
            );
        }

        [Theory]
        [InlineData("POST", 3005)]
        [InlineData("PUT", 3006)]
        [InlineData("OPTIONS", 3007)]
        [InlineData("NewHttpMethod", 3008)]
        public async Task PassthroughRequestsWithBody(string methodType, int port)
        {
            _builder.Configure(app => app.RunProxy(
                context => context
                    .ForwardTo($"http://localhost:{port}/foo/")
                    .AddXForwardedHeaders()
                    .Send()));
            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(new HttpMethod(methodType), "http://mydomain.example")
            {
                Content = new StringContent("Request Body")
            };
            var response = await client.SendAsync(request);

            // Assert response
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.ShouldBe("Response Body");
            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            // Assert sent message
            var sentRequest = _testMessageHandler.SentRequestMessages.Single();
            sentRequest.Headers.TryGetValues("Host", out var hostValue);
            hostValue.SingleOrDefault().ShouldBe("localhost:" + port);
            sentRequest.RequestUri.ToString().ShouldBe("http://localhost:" + port + "/foo/");
            sentRequest.Method.ShouldBe(new HttpMethod(methodType));
        }

        [Fact]
        public async Task ApplyXForwardedHeaders()
        {
            _builder.Configure(app => app.RunProxy(
                context => context
                    .ForwardTo("http://localhost:5000/bar/")
                    .AddXForwardedHeaders()
                    .Send()));
            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://mydomain.example")
            {
                Content = new StringContent("Request Body")
            };
            await client.SendAsync(requestMessage);

            var sentRequest = _testMessageHandler.SentRequestMessages.Single();
            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedHost).ShouldBeTrue();
            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedProto).ShouldBeTrue();
        }

        [Fact]
        public async Task ReturnsStatusCode()
        {
            _builder.Configure(app =>
                app.RunProxy(context => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable))));
            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://mydomain.example")
            {
                Content = new StringContent("Request Body")
            };
            var response = await client.SendAsync(requestMessage);

            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }

        [Fact]
        public async Task X_Forwarded_Headers_should_be_removed_by_default()
        {
            _builder.Configure(app => app.RunProxy(
                context => context
                    .ForwardTo("http://localhost:5000/bar/")
                    .Send()));
            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://mydomain.example")
            {
                Content = new StringContent("Request Body")
            };
            requestMessage.Headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedFor, "127.0.0.1");
            requestMessage.Headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedProto, "http");
            requestMessage.Headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedHost, "localhost");
            requestMessage.Headers.TryAddWithoutValidation(XForwardedExtensions.XForwardedPathBase, "bar");
            await client.SendAsync(requestMessage);

            var sentRequest = _testMessageHandler.SentRequestMessages.Single();

            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedHost).ShouldBeFalse();
            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedProto).ShouldBeFalse();
            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedFor).ShouldBeFalse();
            sentRequest.Headers.Contains(XForwardedExtensions.XForwardedPathBase).ShouldBeFalse();
        }

        [Fact]
        public async Task Response_stream_should_not_be_Flushed_if_the_response_is_ReadyOnly()
        {
            _testMessageHandler = new TestMessageHandler
            {
                Sender = req =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.Found)
                    {
                        // Usually the response of FOUND verb comes with null stream in TestHost. At least that's been observed sometimes.
                        Content = new StreamContent(Stream.Null)
                    };
                    return response;
                }
            };

            _builder.Configure(app => app.RunProxy(
                    context => context
                        .ForwardTo("http://localhost:5000/bar/")
                        .Send()))
                .ConfigureServices(services => services.AddProxy(httpClientBuilder =>
                {
                    //overwrite the registration that made in constructor with the null stream handler
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => _testMessageHandler);
                }));
            var server = new TestServer(_builder);
            HttpClient client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(HttpMethod.Get, "http://mydomain.example")
            {
                Content = new StringContent("Request Body")
            };

            Func<Task> send = () => client.SendAsync(requestMessage);
            send.ShouldNotThrow();
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        [InlineData("TRACE")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task Request_body_should_stay_as_is(string httpMethod)
        {
            const string text = "you shall stay same";

            _testMessageHandler = new TestMessageHandler
            {
                Sender = message => new HttpResponseMessage(HttpStatusCode.OK)
            };

            _builder.Configure(app => app.RunProxy(
                    context => context
                        .ForwardTo("http://localhost:5000/bar/")
                        .Send()))
                .ConfigureServices(services => services.AddProxy(httpClientBuilder =>
                {
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => _testMessageHandler);
                }));

            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), "http://mydomain.example")
            {
                Content = new StringContent(text)
            };

            await client.SendAsync(requestMessage);
            var sentRequest = _testMessageHandler.SentRequestMessages.First();
            var sentContent = sentRequest.Content;

            sentContent.ShouldNotBeNull();
            var sentString = await sentContent.ReadAsStringAsync();
            sentString.Length.ShouldBe(text.Length);

            server.Dispose();
        }

        [Theory]
        [InlineData("GET", false)]
        [InlineData("POST", true)]
        [InlineData("TRACE", false)]
        [InlineData("PUT", true)]
        [InlineData("DELETE", false)]
        [InlineData("PATCH", true)]
        public async Task Only_copy_request_body_when_condition_is_met(string httpMethod, bool shouldCopy)
        {
            const string text = "you might stay same";

            _testMessageHandler = new TestMessageHandler
            {
                Sender = message => new HttpResponseMessage(HttpStatusCode.OK)
            };

            _builder.Configure(app => app.RunProxy(
                    context => context
                        .ForwardTo("http://localhost:5000/bar/")
                        .Send()))
                .ConfigureServices(services => services.AddProxy(httpClientBuilder =>
                {
                    httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => _testMessageHandler);
                },
                    options =>
                {
                    options.CopyRequestBodyIf = request => request.Method == httpMethod && shouldCopy;
                }));

            var server = new TestServer(_builder);
            var client = server.CreateClient();

            var requestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), "http://mydomain.example")
            {
                Content = new StringContent(text)
            };

            await client.SendAsync(requestMessage);
            var sentRequest = _testMessageHandler.SentRequestMessages.First();
            var sentContent = sentRequest.Content;

            (sentContent != null).ShouldBe(shouldCopy);
            var sentString = string.Empty;
            if (shouldCopy)
            {
                sentContent.ShouldNotBeNull();
                sentString = await sentContent.ReadAsStringAsync();
            }
            (sentString.Length > 0).ShouldBe(shouldCopy);

            server.Dispose();
        }
    }

    internal class TestMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Sender { get; set; }

        public List<HttpRequestMessage> SentRequestMessages { get; } = new List<HttpRequestMessage>();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentRequestMessages.Add(request);

            return Sender != null
                ? Task.FromResult(Sender(request))
                : Task.FromResult<HttpResponseMessage>(null);
        }
    }
}
