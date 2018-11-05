using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ProxyKit.DownstreamServer
{
    internal class Controller : ControllerBase
    {
        [HttpGet("/")]
        public string GetRoot()
        {
            return "root";
        }
    }
}