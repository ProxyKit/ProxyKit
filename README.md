![Image](logo.png)

# ProxyKit [![NuGet][nuget badge]][nuget package] [![Feedz][feedz badge]][feedz package]

A toolkit to create code-first **HTTP Reverse Proxies** hosted in ASP.NET Core as middleware. This
allows focused code-first proxies that can be embedded in existing ASP.NET Core
applications or deployed as a standalone server. Deployable anywhere ASP.NET
Core is deployable such as Windows, Linux, Containers and Serverless (with
caveats).

Having built proxies many times before, I felt it is time to make a package. Forked
from [ASP.NET labs][aspnet labs], it has been heavily modified with a different
API, to facilitate a wider variety of proxying scenarios (i.e. routing based on
a JWT claim) and interception of the proxy requests / responses for
customization of headers and (optionally) request / response bodies. It also
uses [`HttpClientFactory`] internally that will mitigate against DNS caching
issues making it suitable for microservice / container environments.

<!-- TOC depthFrom:2 -->

- [ProxyKit ![NuGet][nuget package] [![Feedz][feedz badge]][feedz package]](#proxykit-nugetnuget-package-feedzfeedz-badgefeedz-package)
  - [1. Quick Start](#1-quick-start)
    - [1.1. Install](#11-install)
    - [1.2. Forward HTTP Requests](#12-forward-http-requests)
    - [1.3. Forward WebSocket Requests](#13-forward-websocket-requests)
  - [2. Core Features](#2-core-features)
    - [2.1. Customising the upstream HTTP request](#21-customising-the-upstream-http-request)
    - [2.2. Customising the upstream response](#22-customising-the-upstream-response)
    - [2.3. X-Forwarded Headers](#23-x-forwarded-headers)
      - [2.3.1. Client Sent X-Forwarded-Headers](#231-client-sent-x-forwarded-headers)
      - [2.3.2. Adding X-Forwarded-* Headers](#232-adding-x-forwarded--headers)
      - [2.3.3. Copying X-Forwarded headers](#233-copying-x-forwarded-headers)
    - [2.4. Configuring ProxyKit's HttpClient](#24-configuring-proxykits-httpclient)
    - [2.5. Error handling](#25-error-handling)
    - [2.6. Testing](#26-testing)
    - [2.7. Load Balancing](#27-load-balancing)
      - [2.7.1. Weighted Round Robin](#271-weighted-round-robin)
    - [2.8. Typed Handlers](#28-typed-handlers)
  - [3. Recipes](#3-recipes)
  - [4. Making upstream servers reverse proxy friendly](#4-making-upstream-servers-reverse-proxy-friendly)
  - [5. Performance considerations](#5-performance-considerations)
  - [6. Note about serverless](#6-note-about-serverless)
  - [7. Comparison with Ocelot](#7-comparison-with-ocelot)
  - [8. How to build](#8-how-to-build)
  - [9. Contributing / Feedback / Questions](#9-contributing--feedback--questions)
  - [10. Articles, blogs and other external links](#10-articles-blogs-and-other-external-links)

<!-- /TOC -->

## 1. Quick Start

### 1.1. Install

ProxyKit is a `NetStandard2.0` package. Install into your ASP.NET Core project:

```bash
dotnet add package ProxyKit
```

### 1.2. Forward HTTP Requests

In your `Startup`, add the proxy service:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddProxy();
    ...
}
```

Forward HTTP requests to `upstream-server:5001`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001/")
        .AddXForwardedHeaders()
        .Send());
}
```

What is happening here?

 1. `context.ForwardTo(upstreamHost)` is an extension method on
    `HttpContext` that creates and initializes an `HttpRequestMessage` with
    the original request headers copied over, yielding a `ForwardContext`.
 2. `AddXForwardedHeaders` adds `X-Forwarded-For`, `X-Forwarded-Host`,
    `X-Forwarded-Proto` and `X-Forwarded-PathBase` headers to the upstream
    request.
 3. `Send` Sends the forward request to the upstream server and returns an
    `HttpResponseMessage`.
 4. The proxy middleware then takes the response and applies it to
    `HttpContext.Response`.

Note: `RunProxy` is [terminal] - anything added to the pipeline _after_
`RunProxy` will never be executed.

### 1.3. Forward WebSocket Requests

Forward WebSocket requests to `upstream-server:5002`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.UseWebSockets();
    app.UseWebSocketProxy(
        context => new Uri("ws://upstream-host:80/"),
        options => options.AddXForwardedHeaders());
}
```

What is happening here?

 1. `app.UseWebSockets()` must first be added otherwise websocket requests will
    never be handled by ProxyKit.
 2. The first parameter must return the URI of the upstream host with a scheme
    of `ws://`.
 3. The second parameter `options` allows you to do some customisation of the
    initial upstream requests such as adding some headers.

## 2. Core Features

### 2.1. Customising the upstream HTTP request

One can modify the upstream request headers prior to sending them to suit
customisation needs. ProxyKit doesn't add, remove, nor modify any headers by
default; one must opt in any behaviours explicitly.

In this example we will add a `X-Correlation-ID` header if the incoming request does not bear one:

```csharp
public const string XCorrelationId = "X-Correlation-ID";

public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context =>
    {
        var forwardContext = context.ForwardTo("http://upstream-server:5001/");
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
    public const string XCorrelationId = "X-Correlation-ID";
    
    public static ForwardContext ApplyCorrelationId(this ForwardContext forwardContext)
    {
        if (!forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
        {
            forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
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
        .ForwardTo("http://upstream-server:5001/")
        .ApplyCorrelationId()
        .Send());
}
```

### 2.2. Customising the upstream response

The response from an upstream server can be modified before it is sent to the
client. In this example we are removing a header:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(async context =>
    {
        var response = await context
            .ForwardTo("http://localhost:5001/")
            .Send();

        response.Headers.Remove("MachineID");

        return response;
    });
}
```

### 2.3. X-Forwarded Headers

#### 2.3.1. Client Sent X-Forwarded-Headers 

:warning: To mitigate against spoofing attacks and misconfiguration ProxyKit
does not copy `X-Forward-*` headers from the incoming request to the upstream
request by default. Copying them requires opting in; see _2.3.3 Copying
X-Forwarded headers_ below.

#### 2.3.2. Adding `X-Forwarded-*` Headers

Many applications will need to know what their "outside" host / URL is in order
to generate correct values. This is achieved using `X-Forwarded-*` and
`Forwarded` headers. ProxyKit supports applying `X-Forward-*` headers out of the
box (applying `Forwarded` headers support is on backlog). At the time of writing,
`Forwarded` is [not supported](https://github.com/aspnet/AspNetCore/issues/5978)
in ASP.NET Core.

To add `X-Forwarded-*` headers to the request to the upstream server:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001/")
        .AddXForwardedHeaders()
        .Send());
}
```

This will add `X-Forwarded-For`, `X-Forwarded-Host` and `X-Forwarded-Proto`
headers to the upstream request using values from `HttpContext`. If the proxy
middleware is hosted on a path and a `PathBase` exists on the request, then an
`X-Forwarded-PathBase` is also added.

#### 2.3.3. Copying `X-Forwarded` headers

Chaining proxies is a common pattern in more complex setups. In this case, if
the proxy is an "internal" proxy, you will want to copy the "X-Forwarded-*"
headers from previous proxy. To do so, use `CopyXForwardedHeaders()`:

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001/")
        .CopyXForwardedHeaders()
        .Send());
}
```

You may optionally also add the "internal" proxy details to the `X-Forwarded-*`
header values by combining `CopyXForwardedHeaders()` and
`AddXForwardedHeaders()` (*note the order is important*):

```csharp
public void Configure(IApplicationBuilder app)
{
    app.RunProxy(context => context
        .ForwardTo("http://upstream-server:5001/")
        .CopyXForwardedHeaders()
        .AddXForwardedHeaders()
        .Send());
}
```

### 2.4. Configuring ProxyKit's HttpClient

When adding the Proxy to your application's service collection, there is an
opportunity to configure the internal HttpClient. As
[`HttpClientFactory`](https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests)
is used, its builder is exposed for you to configure:

```csharp
services.AddProxy(httpClientBuilder => /* configure http client builder */);
```

Below are two examples of what you might want to do:

1. Configure the HTTP Client's timeout to 5 seconds:

    ```csharp
    services.AddProxy(httpClientBuilder =>
        httpClientBuilder.ConfigureHttpClient =
            client => client.Timeout = TimeSpan.FromSeconds(5));
    ```

2. Configure the primary `HttpMessageHandler`. This is typically used in testing
   to inject a test handler (see _Testing_ below). 

    ```csharp
    services.AddProxy(httpClientBuilder =>
        httpClientBuilder.ConfigurePrimaryHttpMessageHandler = 
            () => _testMessageHandler);
    ```

### 2.5. Error handling

When `HttpClient` throws, the following logic applies:

1. When upstream server is not reachable, then `503 ServiceUnavailable` is returned.
2. When upstream server is slow and client timeouts, then `504 GatewayTimeout` is
   returned.

Not all exception scenarios and variations are caught, which may result in a
`InternalServerError` being returned to your clients. Please create an issue if
a scenario is missing.

### 2.6. Testing

As ProxyKit is a standard ASP.NET Core middleware, it can be tested using the
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

`RoutingMessageHandler` is an `HttpMessageHandler` that will route requests
to specific hosts based on the origin it is configured with. For ProxyKit
to forward requests (in memory) to the upstream hosts, it needs to be configured
to use the `RoutingMessageHandler` as its primary `HttpMessageHandler`.

Full example can been viewed in [Recipe 6](src/Recipes/06_Testing.cs).

### 2.7. Load Balancing

Load balancing is a mechanism to decide which upstream server to forward the
request to. Out of the box, ProxyKit currently supports one type of
load balancing - Weighted Round Robin. Other types are planned.

#### 2.7.1. Weighted Round Robin

Round Robin simply distributes requests as they arrive to the next host in a
distribution list. With optional weighting, more requests are sent to the host with
the greater weight.

```csharp
public void Configure(IApplicationBuilder app)
{
    var roundRobin = new RoundRobin
    {
        new UpstreamHost("http://localhost:5001/", weight: 1),
        new UpstreamHost("http://localhost:5002/", weight: 2)
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

### 2.8. Typed Handlers

_New in version 2.1.0_

Instead of specifying a delegate, it is possible to use a typed handler. The
reason you may want to do this is when you want to better leverage dependency
injection.

Typed handlers must implement `IProxyHandler` that has a single method with same
signature as `HandleProxyRequest`. In this example our typed handler has a
dependency on an imaginary service to lookup hosts:

```csharp
public class MyTypedHandler : IProxyHandler
{
    private IUpstreamHostLookup _upstreamHostLookup;

    public MyTypeHandler(IUpstreamHostLookup upstreamHostLookup)
    {
        _upstreamHostLookup = upstreamHostLookup;
    }

    public Task<HttpResponseMessage> HandleProxyRequest(HttpContext context)
    {
        var upstreamHost = _upstreamHostLookup.Find(context);
        return context
            .ForwardTo(upstreamHost)
            .AddXForwardedHeaders()
            .Send();
    }
}
```

We then need to register our typed handler service:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddSingleton<MyTypedHandler>();
    ...
}
```

When adding the proxy to the pipeline, use the generic form:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    appInner.RunProxy<MyTypedHandler>());
    ...
}
```

## 3. Recipes

Recipes have moved to [own repo](https://github.com/proxykit/Recipes).

## 4. Making upstream servers reverse proxy friendly

Applications that are deployed behind a reverse proxy typically need to be
somewhat aware of that so they can generate correct URLs and paths when
responding to a browser. That is, they look at `X-Forward-*` / `Forwarded`
headers and use their values.

In ASP.NET Core, this means using the `ForwardedHeaders` middleware in your
application. Please refer to the [documentation][forwarded headers middleware]
for correct usage (and note the security advisory!).

**Note:** the Forwarded Headers middleware does not support
`X-Forwarded-PathBase`. This means if you proxy `http://example.com/foo/` to
`http://upstream-host/` the `/foo/` part is lost and absolute URLs cannot be
generated unless you configure your application's `PathBase` directly.

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
performs the same as the above (including calling `UseForwardedHeaders`):

```csharp
var options = new ForwardedHeadersOptions
{
   ...
};
app.UseXForwardedHeaders(options);
```

## 5. Performance considerations

According to TechEmpower's Web Framework Benchmarks, ASP.NET Core [is up there
with the fastest for plain
text](https://www.techempower.com/benchmarks/#section=data-r17&hw=ph&test=plaintext).
As ProxyKit simply captures headers and async copies request and response body
streams, it will be fast enough for most scenarios.

If absolute raw throughput is a concern for you, then
consider nginx or alternatives. For me being able to create flexible proxies
using C# is a reasonable tradeoff for the (small) performance cost. Note that
what your specific proxy (and its specific configuration) does will impact performance
so you should measure for yourself in your context.

On Windows, ProxyKit is ~3x faster than nginx. However, nginx has clearly
documented that [it has known
performance issues on Windows](https://nginx.org/en/docs/windows.html). Since
one wouldn't be running production nginx on Windows, this comparison is
academic.

Memory wise, ProxyKit maintained a steady ~20MB of RAM after processing millions
of requests for simple forwarding. Again, it depends on what your proxy does so
you should analyse and measure yourself.

## 6. Note about serverless

Whilst it is possible to run full ASP.NET Core web application in [AWS
Lambda] and [Azure Functions] it should be noted that Serverless systems are
message based and not stream based. Incoming and outgoing HTTP request messages
will be buffered and potentially encoded as Base64 if binary (so larger). This
means ProxyKit should only be used for API (json) proxying in production on
Serverless. (Though proxying other payloads is fine for dev / exploration /
quick'n'dirty purposes.)

## 7. Comparison with Ocelot

[Ocelot] is an API Gateway that also runs on ASP.NET Core. A key difference
between API Gateways and general Reverse Proxies is that the former tend to be
**message** based whereas a reverse proxy is **stream** based. That is, an API
Gateway will typically buffer every request and response message to be able
to perform transformations. This is fine for an API Gateway but not suitable for
a general reverse proxy performance wise nor for responses that are
chunked-encoded. See [Not Supported Ocelot docs][ocelot not supported].

Combining ProxyKit with Ocelot would give some nice options for a variety of
scenarios.

## 8. How to build

Requirements: .NET Core SDK 2.2.100 or later.

On Windows: 

```bash
.\build.cmd
```

On Linux: 
```bash
./build.sh
```

## 9. Contributing / Feedback / Questions

Any ideas for features, bugs or questions, please create an issue. Pull requests 
gratefully accepted but please create an issue for discussion first.

I can be reached on twitter at [@randompunter](https://twitter.com/randompunter)

## 10. Articles, blogs and other external links

- [An alternative way to secure SPAs (with ASP.NET Core, OpenID Connect, OAuth 2.0 and ProxyKit)](https://leastprivilege.com/2019/01/18/an-alternative-way-to-secure-spas-with-asp-net-core-openid-connect-oauth-2-0-and-proxykit/)

---

<sub>logo is [distribute](https://thenounproject.com/term/target/345443) by [ChangHoon Baek](https://thenounproject.com/changhoon.baek.50/) from [the Noun Project](https://thenounproject.com/).</sub>

[nuget badge]: https://img.shields.io/nuget/v/ProxyKit.svg
[nuget package]: https://www.nuget.org/packages/ProxyKit
[feedz badge]: https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Fdh%2Foss-ci%2Fshield%2FProxyKit%2Flatest
[feedz package]: https://f.feedz.io/dh/oss-ci/packages/ProxyKit/latest/download
[aspnet labs]: https://github.com/aspnet/AspLabs
[`httpclientfactory`]:  https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests
[terminal]: https://docs.microsoft.com/en-ie/dotnet/api/microsoft.aspnetcore.builder.runextensions.run?view=aspnetcore-2.1
[forwarded headers middleware]: https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-2.2
[aws lambda]: https://aws.amazon.com/blogs/developer/running-serverless-asp-net-core-web-apis-with-amazon-lambda/
[azure functions]: https://blog.wille-zone.de/post/serverless-webapi-hosting-aspnetcore-webapi-in-azure-functions/
[ocelot]: https://github.com/ThreeMammals/Ocelot
[ocelot not supported]: https://ocelot.readthedocs.io/en/latest/introduction/notsupported.html
