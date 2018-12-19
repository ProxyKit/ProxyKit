using System;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using ProxyKit.XForwardedMiddleware;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder
{
    public static class XForwardedHeadersExtensions
    {
        /// <summary>
        ///     Forwards proxied X-ForwardedFor headers onto current request.
        ///     This adds Microsoft.AspnetCore.HttpOverrides.ForwardedHeadersMiddleware
        ///     and adds support for X-Forwarded-PathBase.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseXForwardedHeaders(this IApplicationBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder
                .UseMiddleware<ForwardedHeadersMiddleware>(Array.Empty<object>())
                .UseMiddleware<XForwardedHeadersMiddleware>(Array.Empty<object>());
        }

        /// <summary>
        ///     Forwards proxied X-ForwardedFor headers onto current request.
        ///     This adds Microsoft.AspnetCore.HttpOverrides.ForwardedHeadersMiddleware
        ///     and adds support for X-Forwarded-PathBase.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="options">Enables the different forwarding options.</param>
        /// <returns></returns>
        public static IApplicationBuilder UseXForwardedHeaders(
            this IApplicationBuilder builder,
            ForwardedHeadersOptions options)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (options == null) throw new ArgumentNullException(nameof(options));

            return builder
                .UseMiddleware<ForwardedHeadersMiddleware>(Options.Create(options))
                .UseMiddleware<XForwardedHeadersMiddleware>(Array.Empty<object>());
        }
    }
}