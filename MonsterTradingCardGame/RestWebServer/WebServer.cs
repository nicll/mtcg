using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RestWebServer
{
    /// <summary>
    /// A basic REST-capable web server using <see cref="TcpListener"/>.
    /// </summary>
    public class WebServer
    {
        private readonly TcpListener _listener;
        private readonly Thread _listenerThread;
        private volatile bool _listening;
        private readonly Dictionary<string, Dictionary<string, RequestHandler>> _staticHandlers;
        private readonly Dictionary<string, Dictionary<string, RequestHandler>> _resourceHandlers;

        /// <summary>
        /// Used for marking a resource part in a URL.
        /// </summary>
        public const string ResourceMarker = "%";

        /// <summary>
        /// Encapsulates a handler for a REST request.
        /// </summary>
        /// <param name="requestContext">Context of the request.</param>
        /// <returns>Response to the request.</returns>
        public delegate RestResponse RequestHandler(RequestContext requestContext);

        /// <summary>
        /// Initializes a new instance of the <see cref="WebServer"/> class with no registered routes.
        /// </summary>
        /// <param name="localEP"></param>
        public WebServer(IPEndPoint localEP)
        {
            _listener = new TcpListener(localEP);
            _listenerThread = new Thread(Run) { Name = "Listener Thread" };
            _listening = false;
            _staticHandlers = new Dictionary<string, Dictionary<string, RequestHandler>>();
            _resourceHandlers = new Dictionary<string, Dictionary<string, RequestHandler>>();
        }

        /// <summary>
        /// Starts listening for incoming connections and processing them.
        /// </summary>
        public void Start()
        {
            _listening = true;
            _listener.Start();
            _listenerThread.Start();
        }

        /// <summary>
        /// Stops listening for incoming connections.
        /// Any connections that have already started processing will continue.
        /// Connections that are currently waiting in the queue will be abandoned.
        /// </summary>
        public void Stop()
        {
            _listener.Stop();
            _listening = false;
        }

        /// <summary>
        /// Registers a handler for a static route.
        /// Static routes do not contain resources.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="route">The path of the URL.</param>
        /// <param name="handler">The handler for the request.</param>
        public void RegisterStaticRoute(string verb, string route, RequestHandler handler)
        {
            if (_listening)
                throw new InvalidOperationException("Cannot add route while server is listening.");

            if (_staticHandlers.TryGetValue(route, out var methodDict) && methodDict.ContainsKey(verb))
                throw new ArgumentException("This route has already been registered.");

            if (!_staticHandlers.ContainsKey(route))
                _staticHandlers[route] = new Dictionary<string, RequestHandler>();

            _staticHandlers[route][verb] = handler;
        }

        /// <summary>
        /// Registers a handler for a route that references resources.
        /// Resources are marked with <see cref="ResourceMarker"/>.
        /// </summary>
        /// <param name="verb">The HTTP verb.</param>
        /// <param name="route">The path of the URL.</param>
        /// <param name="handler">The handler for the request.</param>
        public void RegisterResourceRoute(string verb, string route, RequestHandler handler)
        {
            if (_listening)
                throw new InvalidOperationException("Cannot add route while server is listening.");

            if (_resourceHandlers.TryGetValue(route, out var methodDict) && methodDict.ContainsKey(verb))
                throw new ArgumentException("This route has already been registered.");

            if (!_resourceHandlers.ContainsKey(route))
                _resourceHandlers[route] = new Dictionary<string, RequestHandler>();

            _resourceHandlers[route][verb] = handler;
        }

        private void Run()
        {
            Trace.TraceInformation("Listening thread has started listening for incoming connections.");

            while (_listening)
            {
                var connection = _listener.AcceptTcpClient();
                new Thread(() => RunSingle(connection)) { Name = "Processing Thread" }.Start();
            }

            Trace.TraceInformation("Listening thread has stopped listening for incoming connections.");
        }

        private void RunSingle(TcpClient connection)
        {
            try
            {
                Trace.TraceInformation($"Starting to process incoming request from {connection.Client.RemoteEndPoint}.");
                // reader and writer automatically get closed
                using var reader = new StreamReader(connection.GetStream());
                using var writer = new StreamWriter(connection.GetStream());

                var firstLine = reader.ReadLine();

                // check if stream immediately ends
                if (firstLine is null)
                {
                    WriteHttpErrorToStream(writer, HttpStatusCode.BadRequest);
                    return;
                }

                var firstLineParts = firstLine.Split(' ');

                // check for correct elements in header
                if (firstLineParts.Length != 3 || firstLineParts[2] != "HTTP/1.1")
                {
                    WriteHttpErrorToStream(writer, HttpStatusCode.BadRequest);
                    return;
                }

                var verb = firstLineParts[0];
                var route = firstLineParts[1];

                // check if the route is known
                if (!(GetBestHandlerForRoute(route) is (var methodDict, var resources)))
                {
                    WriteHttpErrorToStream(writer, HttpStatusCode.NotFound);
                    return;
                }

                // check if the method (verb) is allowed on the resource
                if (!methodDict.TryGetValue(verb, out var handler))
                {
                    WriteHttpErrorToStream(writer, HttpStatusCode.MethodNotAllowed);
                    return;
                }

                // gather headers
                var headers = new Dictionary<string, string>();
                int requestLength = 0;
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();

                    // empty line separates header from body
                    if (String.IsNullOrEmpty(line))
                        break;

                    var parts = line.Split(':');

                    // check for invalid length
                    if (parts.Length < 2)
                        continue;

                    // check for length of request
                    if (String.Compare(parts[0], "Content-Length", StringComparison.InvariantCultureIgnoreCase) == 0
                        && !int.TryParse(parts[1], out requestLength))
                    {
                        WriteHttpErrorToStream(writer, HttpStatusCode.LengthRequired);
                        return;
                    }

                    headers[parts[0]] = String.Join(":", parts[1..]).Trim(' ');
                }

                string body = string.Empty;
                if (requestLength > 0)
                {
                    // rest of request is body/payload
                    var bodyBuffer = new char[requestLength];
                    //var bodyBuffer = new byte[requestLength];
                    reader.ReadBlock(bodyBuffer);
                    //reader.BaseStream.Read(bodyBuffer);
                    body = new string(bodyBuffer);
                    //body = reader.CurrentEncoding.GetString(bodyBuffer);
                }

                var requestContext = new RequestContext(verb, route, resources, headers, body);

                // invoke handler and send response
                try
                {
                    var response = handler(requestContext);
                    writer.WriteLine("HTTP/1.1 " + (int)response.StatusCode + " " + response.StatusCode);
                    foreach (var header in response.Headers)
                        writer.WriteLine(header.Key + ": " + header.Value);
                    writer.WriteLine("Content-Length: " + writer.Encoding.GetByteCount(response.Payload));
                    writer.WriteLine();
                    writer.Write(response.Payload);
                    writer.Flush();
                }
                catch (Exception e)
                {
                    WriteHttpErrorToStream(writer, HttpStatusCode.InternalServerError);
                    Trace.TraceError(e.ToString());
                    Debug.Fail("Exception occurred during processing.",
                        $"Request: {verb} {route}" + Environment.NewLine +
                        $"Headers: {String.Join(Environment.NewLine, headers.Select(h => $"{h.Key}: {h.Value}"))}" + Environment.NewLine +
                        $"Payload: {body}");
                }
            }
            catch (IOException e)
            {
                Trace.TraceError(e.ToString());
                Debug.Fail(e.Message, e.ToString());
            }
            finally
            {
                connection.Close();
                Trace.TraceInformation("Finished processing request.");
            }
        }

        private static void WriteHttpErrorToStream(StreamWriter writer, HttpStatusCode statusCode)
        {
            Trace.TraceWarning("Client-side error; StatusCode=" + statusCode);
            writer.WriteLine("HTTP/1.1 " + (int)statusCode + " " + statusCode);
            writer.WriteLine("Content-Length: 0");
            writer.WriteLine();
            writer.Flush();
        }

        private (Dictionary<string, RequestHandler> methodDict, string[] resources)? GetBestHandlerForRoute(string route)
        {
            // check whether the route is a known static route
            if (_staticHandlers.TryGetValue(route, out var methodDict))
                return (methodDict, new string[] { });

            // check whether the route is a known resource route
            // e.g. GET /api/user/testuser/inventory should match GET /api/user/%/inventory
            var parts = route.Split('/'); // parts to compare against
            foreach (var rroute in _resourceHandlers.Keys)
            {
                var rparts = rroute.Split('/'); // parts that should be matched

                string[]? resources = MatchRoutes(parts, rparts); // resources returned if routes matched

                if (resources != null) // matching route found
                    return (_resourceHandlers[rroute], resources);
            }

            return null; // not known

            // helper method for comparing routes
            static string[]? MatchRoutes(string[] actual, string[] template)
            {
                // skip if lengths don't match
                if (actual.Length != template.Length)
                    return null;

                List<string> resources = new List<string>();

                // check each part that isn't referencing a resource
                for (int i = 0; i < actual.Length; ++i)
                {
                    if (template[i] == ResourceMarker)
                        resources.Add(actual[i]); // save resources
                    else if (actual[i] != template[i])
                        return null; // routes don't match
                }

                return resources.ToArray();
            }
        }
    }
}
