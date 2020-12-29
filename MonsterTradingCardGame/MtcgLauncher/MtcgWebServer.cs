using MtcgLauncher.Models;
using MtcgServer;
using Newtonsoft.Json;
using RestWebServer;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace MtcgLauncher
{
    internal class MtcgWebServer
    {
        private readonly IWebServer _web;
        private readonly MtcgServer.MtcgServer _server;

        public MtcgWebServer(IWebServer webServer, IDatabase database, IBattleHandler battleHandler)
        {
            _web = webServer;
            _server = new MtcgServer.MtcgServer(database, battleHandler);
        }

        /// <summary>
        /// Registers necessary routes for executing MTCG.
        /// This method may only be called once per instance.
        /// </summary>
        public void RegisterRoutes()
        {
            // Register user
            _web.RegisterStaticRoute("POST", "/register", async ctx =>
            {
                if (!TryGetObject<UserCredentials>(ctx, out var creds))
                    return new RestResponse(HttpStatusCode.BadRequest, "User credentials not properly defined.");

                var session = await _server.Register(creds.Username, creds.Password);

                if (session is null)
                    return new RestResponse(HttpStatusCode.BadRequest, "User already exists.");

                return new RestResponse(HttpStatusCode.OK, CreateHeader(("Authorization", session.Token.ToString("N"))), "Successfully registered account.");
            });

            // Login user
            _web.RegisterStaticRoute("POST", "/login", async ctx =>
            {
                if (!TryGetObject<UserCredentials>(ctx, out var creds))
                    return new RestResponse(HttpStatusCode.BadRequest, "User credentials not properly defined.");

                var session = await _server.Login(creds.Username, creds.Password);

                if (session is null)
                    return new RestResponse(HttpStatusCode.BadRequest, "User was not found or wrong password.");

                return new RestResponse(HttpStatusCode.OK, CreateHeader(("Authorization", session.Token.ToString("N"))), "Successfully logged in.");
            });

            // Logout user
            _web.RegisterStaticRoute("POST", "/logout", ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return Task.FromResult(new RestResponse(HttpStatusCode.BadRequest, "No authorization supplied."));

                if (!Guid.TryParse(sessionStr, out var token))
                    return Task.FromResult(new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied."));

                if (!_server.Logout(new Session(token)))
                    return Task.FromResult(new RestResponse(HttpStatusCode.NotFound, "Not logged in."));

                return Task.FromResult(new RestResponse(HttpStatusCode.OK, "Successfully logged out."));
            });

            // Get player "profile"
            _web.RegisterResourceRoute("GET", "/users/%", async ctx =>
            {
                var username = ctx.Resources[0];

                if (await _server.GetPlayer(username) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player));

                return new RestResponse(HttpStatusCode.NotFound, "Player was not found.");
            });

            // Get player stack
            _web.RegisterResourceRoute("GET", "/users/%/stack", async ctx =>
            {
                var username = ctx.Resources[0];

                if (await _server.GetPlayer(username) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Stack));

                return new RestResponse(HttpStatusCode.NotFound, "Player was not found.");
            });

            // Get player deck
            _web.RegisterResourceRoute("GET", "/users/%/deck", async ctx =>
            {
                var username = ctx.Resources[0];

                if (await _server.GetPlayer(username) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Deck));

                return new RestResponse(HttpStatusCode.NotFound, "Player was not found.");
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

        private static bool TryGetObject<T>(RequestContext context, [MaybeNullWhen(false)] out T result)
        {
            try
            {
                result =  JsonConvert.DeserializeObject<T>(context.Payload);
                return true;
            }
            catch (JsonException)
            {
                result = default;
                return false;
            }
        }

        private static T GetObject<T>(RequestContext context)
            => JsonConvert.DeserializeObject<T>(context.Payload);

        private static Dictionary<string, string> CreateHeader(params (string key, string value)[] entries)
        {
            Dictionary<string, string> dict = new();

            foreach (var entry in entries)
                dict[entry.key] = entry.value;

            return dict;
        }
    }
}
