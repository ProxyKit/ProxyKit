# ProxyKit [![Build Status][travis build]][project] [![NuGet][nuget badge]][nuget package]

A toolkit to create reverse proxies hosted in ASP.NET Core as middleware. This
allows focused code-first proxies that be embedded in existing applications or
deployed as standalone applications. Deployable anywhere ASP.NET Core is
deployable such as Windows, Linux, Containers and Serverless.

Originally Forked from [ASP.NET labs][aspnet labs], it has been heavily modified
with a different API, to facilitate a wider variety of proxying scenarios (i.e.
routing based on a JWT claim) and interception of the proxy requests and
responses for customization of headers and, optionally, response bodies. It also
uses [`HttpClientFactory`] internally that will mitigate against dns caching
issues and handler lifecycles making it suitable for microservice / container
environments.

Having built proxies many times before, I felt it is time make a package.

<!-- TOC depthFrom:2 -->

- [1. Quick Start](#1-quick-start)
- [2. Performance Overhead](#2-performance-overhead)
- [3. Comparison with Ocelot](#3-comparison-with-ocelot)
- [4. Acknoledgements](#4-acknoledgements)
- [5. Contributing](#5-contributing)

<!-- /TOC -->

## 1. Quick Start

## 2. Performance Overhead

## 3. Comparison with Ocelot

[Ocelot] is an API Gateway also on ASP.NET Core. A key difference between API
Gateways and general Reverse Proxies is that the former tend to be **message**
based whereas a reverse proxy is **stream** based. That is, an API gateway will
typically buffer the every request and response message to be able to perform
transformations. This is fine for an API gateway but not suitable for a reverse
proxy performance wise nor for responses that are chunked-encoded. See [Not
Supported Ocelot docs][ocelot not supported].

## 4. Acknoledgements

## 5. Contributing


[travis build]: https://travis-ci.org/damianh/ProxyKit.svg?branch=master
[project]: https://travis-ci.org/damianh/ProxyKit
[nuget badge]: https://img.shields.io/nuget/v/ProxyKit.svg
[nuget package]: https://www.nuget.org/packages/ProxyKit
[aspnet labs]: https://github.com/aspnet/AspLabs
[`httpclientfactory`]:  https://github.com/aspnet/Extensions/tree/master/src/HttpClientFactory
[ocelot]: https://github.com/ThreeMammals/Ocelot
[ocelot not supported]: https://ocelot.readthedocs.io/en/latest/introduction/notsupported.html