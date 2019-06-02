using System.Text;
using Microsoft.AspNetCore.Http;

namespace ProxyKit.Recipe.Simple
{
    internal static class HttpRequestExtensions
    {
        internal static string AsHtml(this HttpRequest request)
        {
            var html = new StringBuilder();
            html.Append("<table><tr><th align=\"left\">Header</th><th align=\"left\">Value</th><tr>");
            html.Append($"<tr><td>Host</td><td>{request.Host}</td></tr>");
            html.Append($"<tr><td>Path</td><td>{request.Path}</td></tr>");
            foreach (var (key, value) in request.Headers)
            {
                html.Append($"<tr><td>{key}</td><td>{value}</td></tr>");
            }
            html.Append("</table>");
            return html.ToString();
        }
    }
}