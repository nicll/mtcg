using System;
using System.Collections.Generic;

namespace RestWebServer
{
    public class RequestContext
    {
        public string Verb { get; }

        public string Path { get; }

        public string Query { get; }

        public string Fragment { get; }

        public string[] Resources { get; }

        public Dictionary<string, string> Headers { get; }

        public string Payload { get; }

        public RequestContext(string verb, string route, string[] resources, Dictionary<string, string> headers, string payload)
        {
            Verb = verb;
            Headers = headers;
            Resources = resources;
            Payload = payload;

            Path = route;
            Query = string.Empty;
            Fragment = string.Empty;

            int queryBegin = route.IndexOf('?');
            int fragmentBegin = route.IndexOf('#');

            if (queryBegin != -1)
            {
                Path = route[..queryBegin]; // route.Substring(0, queryBegin)
                Query = route[queryBegin..(fragmentBegin != -1 ? fragmentBegin : ^0)];
                // route.Substring(queryBegin, fragmentBegin != -1 ? fragmentBegin - queryBegin : route.Length - queryBegin);
            }

            if (fragmentBegin != -1)
            {
                if (queryBegin == -1)
                    Path = route[..fragmentBegin]; // route.Substring(0, fragmentBegin)

                Fragment = route[fragmentBegin..]; // route.Substring(fragmentBegin, route.Length - fragmentBegin)
            }
        }
    }
}
