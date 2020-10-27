using RestWebServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace RestWebServerLauncher
{
    /// <summary>
    /// Implements a small server that can be used for storing and retrieving text messages.
    /// </summary>
    public class MessageServer
    {
        private readonly Dictionary<int, string> _messages;
        private readonly WebServer _web;
        private volatile int _count;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageServer"/> class with routes accessible over HTTP
        /// for listing, reading, writing, updating and deleting messages.
        /// </summary>
        public MessageServer()
        {
            _messages = new Dictionary<int, string>();
            _web = new WebServer(new IPEndPoint(IPAddress.Any, 2200));
            _count = 0;

            _web.RegisterStaticRoute("GET", "/messages", _ => new RestResponse(HttpStatusCode.OK, ListMessages()));
            _web.RegisterStaticRoute("POST", "/messages", ctx => new RestResponse(HttpStatusCode.Created, AddMessage(ctx.Payload)));
            _web.RegisterResourceRoute("PUT", "/messages/%", ctx =>
            {
                int id = ParseIntOrZero(ctx.Resources[0]);

                if (id <= 0)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid message id.");

                bool created = UpdateMessage(id, ctx.Payload);
                return new RestResponse(created ? HttpStatusCode.Created : HttpStatusCode.OK, string.Empty);
            });
            _web.RegisterResourceRoute("DELETE", "/messages/%", ctx =>
            {
                int id = ParseIntOrZero(ctx.Resources[0]);

                if (id <= 0)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid message id.");

                bool deleted = RemoveMessage(id);
                return new RestResponse(deleted ? HttpStatusCode.OK : HttpStatusCode.NotFound, string.Empty);
            });
            _web.RegisterResourceRoute("GET", "/messages/%", ctx =>
            {
                int id = ParseIntOrZero(ctx.Resources[0]);

                if (id <= 0)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid message id.");

                var text = GetMessage(id);
                return new RestResponse(text is null ? HttpStatusCode.NotFound : HttpStatusCode.OK, text ?? string.Empty);
            });
        }

        /// <summary>
        /// Starts listening for incoming requests.
        /// </summary>
        public void Start()
            => _web.Start();

        /// <summary>
        /// Stops listening for incoming requests.
        /// </summary>
        public void Stop()
            => _web.Stop();

        private string ListMessages()
            => "[" + String.Join(',', _messages.Select(kvp => $"\"{kvp.Key}\":\"{kvp.Value}\"")) + "]"; // non-attempt at json

        private string? GetMessage(int id)
            => _messages.ContainsKey(id) ? _messages[id] : null;

        private string AddMessage(string text)
        {
            lock (_messages)
            {
                while (_messages.ContainsKey(++_count)) { }
                _messages[_count] = text;
                return _count.ToString();
            }
        }

        private bool UpdateMessage(int id, string text)
        {
            bool created;

            lock (_messages)
            {
                created = !_messages.ContainsKey(id);
                _messages[id] = text;
            }

            return created;
        }

        private bool RemoveMessage(int id)
        {
            lock (_messages)
                return _messages.Remove(id);
        }

        /// <summary>
        /// Attempts to parse an <see cref="Int32"/>.
        /// If parsing fails, 0 is returned.
        /// </summary>
        /// <param name="input">Input text to parse.</param>
        /// <returns>Converted integer or 0.</returns>
        private static int ParseIntOrZero(string input)
        {
            int.TryParse(input, out var num);
            return num;
        }
    }
}
