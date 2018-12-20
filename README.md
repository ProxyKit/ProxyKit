# ProxyKit [![Build Status][travis build]][project] [![NuGet][nuget badge]][nuget package]

A toolkit to create **reverse proxies** hosted in ASP.NET Core as middleware. This
allows focused code-first proxies that can be embedded in existing ASP.NET Core
applications or deployed as standalone applications. Deployable anywhere ASP.NET
Core is deployable such as Windows, Linux, Containers and Serverless (with
caveats).

Having built proxies many times before, I felt it is time make a package. Forked
from [ASP.NET labs][aspnet labs], it has been heavily modified with a different
API, to facilitate a wider variety of proxying scenarios (i.e. routing based on
a JWT claim) and interception of the proxy requests / responses for
customization of headers and (optionally) request / response bodies. It also
uses [`HttpClientFactory`] internally that will mitigate against dns caching
issues and handler lifecycles making it suitable for microservice / container
environments.

<!-- TOC depthFrom:2 -->

- [1. Quick Start](#1-quick-start)
- [2. XForwardedHeaders](#2-xforwardedheaders)
- [3. Making upstream applications reverse proxy friendly](#3-making-upstream-applications-reverse-proxy-friendly)
- [4. Error handling](#4-error-handling)
- [5. Testing](#5-testing)
- [6. Distribution](#6-distribution)
    - [6.1. Round Robin](#61-round-robin)
- [6. Further examples](#6-further-examples)
- [7. Performance overhead](#7-performance-overhead)
- [8. Note about Serverless](#8-note-about-serverless)
- [9. Comparison with Ocelot](#9-comparison-with-ocelot)
- [10. Contributing & Feedback](#10-contributing--feedback)

<!-- /TOC -->

## 1. Quick Start

ProxyKit is a `NetStandard2.0` package. Install into your ASP.NET Core project:

    dotnet add package ProxyKit

In your `Startup`, add the proxy services:

    public void ConfigureServices(IServiceCollection services)
    {
        ...
        services.AddProxy();
        ...
    }

Forward requests to `localhost:5001`:

    public void Configure(IApplicationBuilder app)
    {
        app.RunProxy(context => context
            .ForwardTo("http://localhost:5001")
            .Execute());
    }

What is happening here?

 1. `context.ForwardTo(upstreamHost)` is an extension method over the
    `HttpContext` that creates and initializes an `HttpRequestMessage`.
 2. `Execute` forwards the request to the upstream host and return an
    `HttpResponseMessage`.
 3. The proxy middleware then takes the response and applies it to
    `HttpContext.Response`.

Note: `RunProxy` is [terminal] - anything added to the pipeline after `RunProxy`
will never be executed.

## 2. XForwardedHeaders

Many applications will need to know what their "outside" url is in order to
generate correct urls so we need to tell them that. This is achieved using
`X-Forwarded` and `Forwarded` headers

`Forwarded` header support is on backlog. At time of writing, it is [not
supported](https://github.com/aspnet/AspNetCore/issues/5978) in ASP.NET Core.

## 3. Making upstream applications reverse proxy friendly

## 4. Error handling

## 5. Testing

## 6. Distribution

### 6.1. Round Robin

## 6. Further examples

## 7. Performance overhead

GetTypedHeaders


## 8. Note about Serverless

Whilst is it is possible to run full ASP.NET Core web application in [AWS
Lambda] and [Azure Functions] it should be noted that Serverless systems are
message based and not stream based. Incoming and outgoing HTTP request messages
will be buffered and potentially encoded as Base64 if binary (so larger). This
means ProxyKit should only be used for API (json) proxying in production on
Serverless. (Though proxing other payloads is fine for dev / exploration /
quick'n'dirty purposes.)

## 9. Comparison with Ocelot

[Ocelot] is an API Gateway also on ASP.NET Core, and  A key difference between API
Gateways and general Reverse Proxies is that the former tend to be **message**
based whereas a reverse proxy is **stream** based. That is, an API gateway will
typically buffer the every request and response message to be able to perform
transformations. This is fine for an API gateway but not suitable for a reverse
proxy performance wise nor for responses that are chunked-encoded. See [Not
Supported Ocelot docs][ocelot not supported].

Combining ProxyKit with Ocelot would give some nice options for a variety of
scenarios.

## 10. Contributing & Feedback

Any ideas for features, bugs or questions, please create an issue. Pull requests 
gratefully accepted.

[travis build]: https://travis-ci.org/damianh/ProxyKit.svg?branch=master
[project]: https://travis-ci.org/damianh/ProxyKit
[nuget badge]: https://img.shields.io/nuget/v/ProxyKit.svg
[nuget package]: https://www.nuget.org/packages/ProxyKit
[aspnet labs]: https://github.com/aspnet/AspLabs
[`httpclientfactory`]:  https://github.com/aspnet/Extensions/tree/master/src/HttpClientFactory
[terminal]: https://docs.microsoft.com/en-ie/dotnet/api/microsoft.aspnetcore.builder.runextensions.run?view=aspnetcore-2.1
[aws lambda]: https://aws.amazon.com/blogs/developer/running-serverless-asp-net-core-web-apis-with-amazon-lambda/
[azure functions]: https://blog.wille-zone.de/post/serverless-webapi-hosting-aspnetcore-webapi-in-azure-functions/
[ocelot]: https://github.com/ThreeMammals/Ocelot
[ocelot not supported]: https://ocelot.readthedocs.io/en/latest/introduction/notsupported.html