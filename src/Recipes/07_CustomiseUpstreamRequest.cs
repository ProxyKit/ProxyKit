using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ProxyKit.Recipies
{
    public class CustomiseUpstreamRequest : ExampleBase<CustomiseUpstreamRequest.Startup>
    {
        public class Startup
        {
            public void ConfigureServices(IServiceCollection services)
            {
                services.AddProxy();
            }

            public const string XCorrelationId = "X-Correlation-ID";

            public void Configure(IApplicationBuilder app)
            {
                // Inline
                app.RunProxy(context =>
                {
                    var forwardContext = context.ForwardTo("http://localhost:5001");
                    if (forwardContext.UpstreamRequest.Headers.Contains("X-Correlation-ID"))
                    {
                        forwardContext.UpstreamRequest.Headers.Add("X-Correlation-ID", Guid.NewGuid().ToString());
                    }
                    return forwardContext.Execute();
                });

                // Extension Method
                app.RunProxy(context => context
                    .ForwardTo("http://localhost:5001")
                    .ApplyXForwardedHeaders()
                    .ApplyCorrelationId()
                    .Execute());
            }
        }
    }

    public static class CorrelationIdExtensions
    {
        public const string XCorrelationId = "X-Correlation-ID";

        public static ForwardContext ApplyCorrelationId(this ForwardContext forwardContext)
        {
            if (forwardContext.UpstreamRequest.Headers.Contains(XCorrelationId))
            {
                forwardContext.UpstreamRequest.Headers.Add(XCorrelationId, Guid.NewGuid().ToString());
            }
            return forwardContext;
        }
    }
}
