using System;
using System.Net;

namespace RestWebServer
{
    public class RestResponse
    {
        public HttpStatusCode StatusCode { get; }

        public string Payload { get; }

        public RestResponse(HttpStatusCode statusCode, string payload)
        {
            StatusCode = statusCode;
            Payload = payload;
        }
    }
}
