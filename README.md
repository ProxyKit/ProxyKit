![Image](logo.png)

# ProxyKit [![Build Status][travis build]][project] [![NuGet][nuget badge]][nuget package]

A toolkit to create **HTTP proxies** hosted in ASP.NET Core as middleware. This
allows focused code-first proxies that can be embedded in existing ASP.NET Core
applications or deployed as a standalone server. Deployable anywhere ASP.NET
Core is deployable such as Windows, Linux, Containers and Serverless (with
caveats).

Having built proxies many times before, I felt it is time make a package. Forked
from [ASP.NET labs][aspnet labs], it has been heavily modified with a different
API, to facilitate a wider variety of proxying scenarios (i.e. routing based on
a JWT claim) and interception of the proxy requests / responses for
customization of headers and (optionally) request / response bodies. It also
uses [`HttpClientFactory`] internally that will mitigate against dns caching
issues making it suitable for microservice / container environments.

<!-- TOC depthFrom:2 -->

- [1. Quick Start](#1-quick-start)
- [2. Customising the upstream request](#2-customising-the-upstream-request)
- [3. Customising the upstream response](#3-customising-the-upstream-response)
- [4. X-Forwarded Headers](#4-x-forwarded-headers)
    - [4.1. Client Sent X-Forwarded-Headers](#41-client-sent-x-forwarded-headers)
    - [4.2. Adding X-Forwarded-Headers](#42-adding-x-forwarded-headers)
    - [4.3. Copying X-Forwarded-Headers](#43-copying-x-forwarded-headers)
- [5. Making upstream servers reverse proxy friendly](#5-making-upstream-servers-reverse-proxy-friendly)
- [6. Configuring ProxyKit's HttpClient](#6-configuring-proxykits-httpclient)
- [7. Error handling](#7-error-handling)
- [8. Testing](#8-testing)
- [9. Load Balancing](#9-load-balancing)
    - [9.1. Weighted Round Robin](#91-weighted-round-robin)
- [10. Recipes](#10-recipes)
- [11. Performance considerations](#11-performance-considerations)
- [12. Note about serverless](#12-note-about-serverless)
- [13. Comparison with Ocelot](#13-comparison-with-ocelot)
- [14. How to build](#14-how-to-build)
- [15. Contributing / Feedback / Questions](#15-contributing--feedback--questions)

<!-- /TOC -->

## 1. Quick Start

ProxyKit is a `NetStandard2.0` package. Install into your ASP.NET Core project:

```bash
dotnet add package ProxyKit
```

In your `Startup`, add the proxy service:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddProxy();
    ...
}
```

Forward requests to `localhost:5001`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001")
        .Send());
}
```

What is happening here?

 1. `context.ForwardTo(upstreamHost)` is an extension method over the
    `HttpContext` that creates and initializes an `HttpRequestMessage` with
    the original request headers copied over and returns a `ForwardContext`.
 2. `Send` Sends the forward request to the upstream server and returns an
    `HttpResponseMessage`.
 3. The proxy middleware then takes the response and applies it to
    `HttpContext.Response`.

Note: `RunProxy` is [terminal] - anything added to the pipeline after `RunProxy`
will never be executed.

## 2. Customising the upstream request

One can modify the upstream request headers prior to sending them to suit
customisation needs. ProxyKit doesn't add, remove nor modify any headers by
default; one must opt in any behaviours explicitly.

In this example we will add a `X-Correlation-Id` header if it does not exist:

```csharp
public const string XCorrelationId = "X-Correlation-ID";

public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context =>
    {
        var forwardContext = context.ForwardTo("http://localhost:5001");
        if (!forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
        {
            forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
        }
        return forwardContext.Send();
    });
}
```

This can be encapsulated as an extension method:

```csharp
public static class CorrelationIdExtensions
{
    public static ForwardContext ApplyCorrelationId(this ForwardContext forwardContext)
    {
        if (forwardContext.UpstreamRequest.Headers.Contains("X-Correlation-ID"))
        {
            forwardContext.UpstreamRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
        }
        return forwardContext;
    }
}
```

... making the proxy code a little nicer to read:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://localhost:5001")
        .ApplyCorrelationId()
        .Send());
}
```

## 3. Customising the upstream response

Response from an upstream server can be modified before it is sent to the
client. In this example we are removing a header:

```csharp
 public void Configure(IApplicationBuilder app)
{
    app.RunProxy(async context =>
    {
        var response = await context
            .ForwardTo("http://localhost:5001")
            .Send();

        response.Headers.Remove("MachineID");

        return response;
    });
}
```

## 4. X-Forwarded Headers

### 4.1. Client Sent X-Forwarded-Headers 

:warning: To mitigate against spoofing attacks and misconfiguration ProxyKit
does not copy `X-Forward-*` headers from the incoming request to the upstream
request by default. To copy these headers requries opting in. See 4.3. Copying
X-Forwarded-Headers below.

### 4.2. Adding X-Forwarded-Headers

Many applications will need to know what their "outside" host / url is in order
to generate correct values. This is achieved using `X-Forwarded-*` and
`Forwarded` headers. ProxyKit supports applying `X-Forward-*` headers out of the
box (applying `Forwarded` headers support is on backlog). At time of writing,
`Forwarded` is [not supported](https://github.com/aspnet/AspNetCore/issues/5978)
in ASP.NET Core.

To add `X-Forwarded-Headers` to the request to the upstream server:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001")
        .AddXForwardedHeaders()
        .Send());
}
```

This will add `X-Forwarded-For`, `X-Forwarded-Host` and `X-Forwarded-Proto`
headers to the upstream request using values from `HttpContext`. If the proxy
middleware is hosted on a path and a `PathBase` exists on the request, then an
`X-Forwarded-PathBase` is also added.

### 4.3. Copying X-Forwarded-Headers

Chaining proxies is a common pattern in more complex setups. In this case, if
the proxy is an "internal" proxy, you will want to copy the "X-Forwarded-*"
headers from previous proxy. To do so, use `CopyXForwardedHeaders()`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001")
        .CopyXForwardedHeaders()
        .Send());
}
```

You may optionally also add the "internal" proxy details to the `X-Forwarded-*`
header values by combinging `CopyXForwardedHeaders()` and
`AddXForwardedHeaders()` (note the order is important):

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001")
        .CopyXForwardedHeaders()
        .AddXForwardedHeaders()
        .Send());
}
```

## 5. Making upstream servers reverse proxy friendly

Applications that are deployed behind a reverse proxy typically need to be
somewhat aware of that so they can generate correct URLs and paths when
responding to a browser. That is, they look at `X-Forward-*` \ `Forwarded`
headers and use their values .

In ASP.NET Core, this means using the Forwarded Headers middleware in your
application. Please refer to the [documentation][forwarded headers middleware]
for correct usage (and note the security advisory!).

**Note:** the Forwarded Headers middleware does not support
`X-Forwarded-PathBase`. This means if you proxy `http://example.com/foo/` to
`http://upstream-host/` the `/foo/` part is lost and absolute URLs cannot be
generated unless you configure your applications PathBase directly.

Related issues and discussions:

- https://github.com/aspnet/AspNetCore/issues/5978
- https://github.com/aspnet/AspNetCore/issues/5898


To support PathBase dynamically in your application with `X-Forwarded-PathBase`,
examine the header early in your pipeline and set the `PathBase` accordingly:

```csharp
var options = new ForwardedHeadersOptions
{
   ...
};
app.UseForwardedHeaders(options);
app.Use((context, next) => 
{
    if (context.Request.Headers.TryGetValue("X-Forwarded-PathBase", out var pathBases))
    {
        context.Request.PathBase = pathBases.First();
    }
    return next();
});
```

Alternatively you can use ProxyKit's `UseXForwardedHeaders` extension that
perform the same as the above (including calling `UseForwardedHeaders`):

```csharp
var options = new ForwardedHeadersOptions
{
   ...
};
app.UseXForwardedHeaders(options);
```

## 6. Configuring ProxyKit's HttpClient

When adding the Proxy to your application's service collection there is an
opportunity of to configure the internal HttpClient. As
[`HttpClientFactory`](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
is used, it's builder is exposed for you to configure:

```csharp
services.AddProxy(httpClientBuilder => /* configure http client builder */);
```

Below are two examples of what you might want to do:

1. Configure the HTTP Client's timeout to 5 seconds:

    ```csharp
    services.AddProxy(httpClientBuilder =>
        httpClientBuilder.ConfigureHttpClient =
            (client) => client.Timeout = TimeSpan.FromSeconds(5));
    ```

2. Configure the primary `HttpMessageHandler`. This is typically used in testing
   to inject a test handler (see Testing below). 

    ```csharp
    services.AddProxy(httpClientBuilder =>
        httpClientBuilder.ConfigurePrimaryHttpMessageHandler = 
            () => _testMessageHandler);
    ```

## 7. Error handling

When `HttpClient` throws the following logic applies:

1. When upstream server is not reachable then `ServiceUnavailable` is returned.
2. When upstream server is slow and client timeouts then `GatewayTimeout` is
   returned.

Not all exception scenarios and variations are caught which may result in a
`InternalServerError` being returned to your clients. Please create an issue if
a scenario is missing.

## 8. Testing

As ProxyKit is standard ASP.NET Core middleware, it can be tested using the
standard in-memory `TestServer` mechanism.

Often you will want to test ProxyKit with your application and perhaps test the
behaviour of your application when load balanced with two or more instances as
indicated below.

```
                               +----------+
                               |"Outside" |
                               |HttpClient|
                               +-----+----+
                                     |
                                     |
                                     |
                         +-----------+---------+
    +-------------------->RoutingMessageHandler|
    |                    +-----------+---------+
    |                                |
    |                                |
    |           +--------------------+-------------------------+
    |           |                    |                         |
+---+-----------v----+      +--------v---------+     +---------v--------+
|Proxy TestServer    |      |Host1 TestServer  |     |Host2 TestServer  |
|with Routing Handler|      |HttpMessageHandler|     |HttpMessageHandler|
+--------------------+      +------------------+     +------------------+
```

`RoutingMessageHandler` is an `HttpMessageHandler` that will route requests to
to specific host based on on the origin it is configured with. For ProyKit
to forward requests (in memory) to the upstream hosts, it needs to be configured
to use the `RoutingMessageHandler` as it's primary `HttpMessageHandler`.

Full example can been viewed [here](src/Recipes/06_Testing.cs).

## 9. Load Balancing

Load balancing is mechanism to decide which upstream server to forward the
request to. Out of the box, ProxyKit currently supports one type of
load balancing - Weighted Round Robin. Other types are planned.

### 9.1. Weighted Round Robin

Round Robin simply distributes requests as they arrive to the next host in a
distribution list. With optional weighting, more requests are send to host with
greater weights.

```csharp
public void Configure(IApplicationBuilder app)
{
    var roundRobin = new RoundRobin
    {
        new UpstreamHost("http://localhost:5001", weight: 1),
        new UpstreamHost("http://localhost:5002", weight: 2)
    };

    app.RunProxy(
        async context =>
        {
            var host = roundRobin.Next();

            return await context
                .ForwardTo(host)
                .Send();
        });
}
```

## 10. Recipes

Recipes are code samples that help you create proxy solutions for your needs.
If you have any ideas for a recipie, or can spot any improvements to the ones
below, please send a pull request! Recipes that stand test of time may be
promoted to an out-of-the-box feature in a future version of ProxyKit.

1. [Simple Forwarding](src/Recipes/01_Simple.cs) - Forward request to a single
   upstream host. 
2. [Proxy Pathing](src/Recipes/02_Paths.cs) - Hosting multiple proxys on
   seperate paths.
3. [Tenant Routing](src/Recipes/03_TenantRouting.cs) - Routing to a specific
   upstream host based on a `TenantId` claim for an authenticated user.
4. [Authentication offloading](src/Recipes/04_IdSrv.cs) - Using
   [IdentityServer](https://identityserver.io/) to handle authentication before
   forwarding to upstream host.
5. [Round Roblin Load Balancing](src/Recipes/05_RoundRobin.cs) - Weighed Round
   Robin load balancing to two upstream hosts.
6. [In-memory Testing](src/Recipes/06_Testing.cs) - Testing behaviour or your
   ASP.NET Core application by running two instances behind round robin proxy.
   Really useful if your application has eventually consistent aspects.
7. [Customise Upstream Requests](src/Recipes/07_CustomiseUpstreamRequest.cs) -
   Customise the upstream request by adding a header.
8. [Customise Upstream Responses](src/Recipes/08_CustomiseUpstreamResponse.cs) -
   Customise the upstream response by removing a header.
9. [Consul Service Discovery](src/Recipes/09_ConsulServiceDisco.cs) - Service
   discovery for an upstream host using [Consul](https://www.consul.io/).
10. [Copy X-Forward Headers](src/Recipes/10_CopyXForward.cs) - Copies
    `X-Forwarded-For`, `X-Forwarded-Host`, `X-Forwarded-Proto` and
    `X-Forwarded-PathBase` headers from the incoming request.

## 11. Performance considerations

According to TechEmpower's Web Framework Benchmarks, ASP.NET Core [is up there
with the fastest for plain
text](https://www.techempower.com/benchmarks/#section=data-r17&hw=ph&test=plaintext).
As ProxyKit simply captures headers and async copies request and response body
streams, it will fast enough for most scenarios.

Stress testing shows that ProxyKit is approximately 8% slower than nginx for
simple forwarding on linux. If absolute raw throughput is a concern for you then
consider nginx or alternatives. For me being able to create flexible proxies
using C# is a reasonable tradeoff for the (small) performance cost. Note that
depending on what your proxy does may impact performance so you should measure
yourself.

On windows, ProxyKit is ~3x faster than nginx. However, nginx has clearly
documented that [it has known
performance](https://nginx.org/en/docs/windows.html) issues on windows. Since
one wouldn't be running production nginx on windows this comparison is
academic.

Memory wise, ProxyKit maintained a steady ~20MB of RAM after processing millions
of requests for simple forwarding. Again, it depends on what your proxy does so
you should analyse and measure yourself.

## 12. Note about serverless

Whilst is it is possible to run full ASP.NET Core web application in [AWS
Lambda] and [Azure Functions] it should be noted that Serverless systems are
message based and not stream based. Incoming and outgoing HTTP request messages
will be buffered and potentially encoded as Base64 if binary (so larger). This
means ProxyKit should only be used for API (json) proxying in production on
Serverless. (Though proxing other payloads is fine for dev / exploration /
quick'n'dirty purposes.)

## 13. Comparison with Ocelot

[Ocelot] is an API Gateway that also runs on ASP.NET Core. A key difference
between API Gateways and general Reverse Proxies is that the former tend to be
**message** based whereas a reverse proxy is **stream** based. That is, an API
gateway will typically buffer the every request and response message to be able
to perform transformations. This is fine for an API gateway but not suitable for
a general reverse proxy performance wise nor for responses that are
chunked-encoded. See [Not Supported Ocelot docs][ocelot not supported].

Combining ProxyKit with Ocelot would give some nice options for a variety of
scenarios.

## 14. How to build

Requirements: .NET Core SDK 2.2.100 or later.

On Windows: 

```bash
.\build.cmd
```

On Linux: 
```bash
./build.sh
```

## 15. Contributing / Feedback / Questions

Any ideas for features, bugs or questions, please create an issue. Pull requests 
gratefully accepted but please create an issue for discussion first.

I can be reached on twitter at [@randompunter](https://twitter.com/randompunter)

---

<sub>[distribute](https://thenounproject.com/term/target/345443) by [ChangHoon Baek](https://thenounproject.com/changhoon.baek.50/) from [the Noun Project](https://thenounproject.com/).</sub>

[travis build]: https://travis-ci.org/damianh/ProxyKit.svg?branch=master
[project]: https://travis-ci.org/damianh/ProxyKit
[nuget badge]: https://img.shields.io/nuget/v/ProxyKit.svg
[nuget package]: https://www.nuget.org/packages/ProxyKit
[aspnet labs]: https://github.com/aspnet/AspLabs
[`httpclientfactory`]:  https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
[terminal]: https://docs.microsoft.com/en-ie/dotnet/api/microsoft.aspnetcore.builder.runextensions.run?view=aspnetcore-2.1
[forwarded headers middleware]: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-2.2
[aws lambda]: https://aws.amazon.com/blogs/developer/running-serverless-asp-net-core-web-apis-with-amazon-lambda/
[azure functions]: https://blog.wille-zone.de/post/serverless-webapi-hosting-aspnetcore-webapi-in-azure-functions/
[ocelot]: https://github.com/ThreeMammals/Ocelot
[ocelot not supported]: https://ocelot.readthedocs.io/en/latest/introduction/notsupported.html
