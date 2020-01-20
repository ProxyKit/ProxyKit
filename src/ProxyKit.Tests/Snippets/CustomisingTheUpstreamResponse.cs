using Microsoft.AspNetCore.Builder;
using ProxyKit;

public class CustomisingTheUpstreamResponse
{
    #region CustomisingTheUpstreamResponse
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
    #endregion
}