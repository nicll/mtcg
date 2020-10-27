using System;
using System.Collections.Generic;
using System.Net;

namespace RestWebServer
{
    public class RestResponse
    {
        public HttpStatusCode StatusCode { get; }

        public IDictionary<string, string> Headers { get; }

        public string Payload { get; }

        public RestResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, string>();
            Payload = string.Empty;
        }

        public RestResponse(HttpStatusCode statusCode, string payload)
        {
            StatusCode = statusCode;
            Headers = new Dictionary<string, string>();
            Payload = payload;
        }

        public RestResponse(HttpStatusCode statusCode, IDictionary<string, string> headers)
        {
            StatusCode = statusCode;
            Headers = headers;
            Payload = string.Empty;
        }

        public RestResponse(HttpStatusCode statusCode, IDictionary<string, string> headers, string payload)
        {
            StatusCode = statusCode;
            Headers = headers;
            Payload = payload;
        }
    }
}
