using NUnit.Framework;
using RestWebServer;
using System;
using System.Net;

namespace MtcgServerTests
{
    public class RestWebServerTest
    {
        private WebServer _web;

        [SetUp]
        public void SetupWebServer()
        {
            _web = new WebServer(new IPEndPoint(IPAddress.Any, 2200));
        }

        [Test]
        public void TestRegisterStaticRoute()
        {
            _web.RegisterStaticRoute("GET", "/api/echo", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Payload));
        }

        [Test]
        public void TestRegisterResourceRoute()
        {
            _web.RegisterResourceRoute("GET", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Resources[0]));
        }

        [Test]
        public void TestRegisterStaticRouteTwice()
        {
            Assert.Catch<ArgumentException>(() =>
            {
                _web.RegisterStaticRoute("GET", "/api/echo", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Payload));
                _web.RegisterStaticRoute("GET", "/api/echo", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Payload));
            });
        }

        [Test]
        public void TestRegisterResourceRouteTwice()
        {
            Assert.Catch<ArgumentException>(() =>
            {
                _web.RegisterResourceRoute("GET", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Resources[0]));
                _web.RegisterResourceRoute("GET", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Resources[0]));
            });
        }

        [Test]
        public void TestRegisterStaticRouteWhileListening()
        {
            _web.Start();
            Assert.Catch<InvalidOperationException>(() => _web.RegisterStaticRoute("GET", "/api/echo", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Payload)));
        }

        [Test]
        public void TestRegisterResourceRouteWhileListening()
        {
            _web.Start();
            Assert.Catch<InvalidOperationException>(() => _web.RegisterResourceRoute("GET", "/api/user/%", requestContext => new RestResponse(HttpStatusCode.OK, requestContext.Resources[0])));
        }
    }
}
