using System;
using System.Net;
using System.Threading;
using RestWebServer;

namespace RestWebServerLauncher
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var web = new WebServer(new IPEndPoint(IPAddress.Any, 2200));
            web.RegisterStaticRoute("GET", "/api/hello", _ => new RestResponse(HttpStatusCode.OK, "hello world"));
            web.RegisterStaticRoute("GET", "/api/echo", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Payload));
            web.RegisterResourceRoute("GET", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Resources[0]));
            web.RegisterResourceRoute("POST", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.Created, requestContext.Resources[0]));
            web.Start();

            Thread.CurrentThread.Join();
        }
    }
}
