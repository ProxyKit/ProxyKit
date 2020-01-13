using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class WeightedRoundRobin
{
    #region WeightedRoundRobin
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
    #endregion
}