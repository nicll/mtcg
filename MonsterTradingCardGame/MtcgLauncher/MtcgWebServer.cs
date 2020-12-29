﻿using MtcgLauncher.Models;
using MtcgServer;
using MtcgServer.CardRequirements;
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
                if (!TryGetObject<UserCredentialsModel>(ctx, out var creds))
                    return new RestResponse(HttpStatusCode.BadRequest, "User credentials not properly defined.");

                var session = await _server.Register(creds.Username, creds.Password);

                if (session is null)
                    return new RestResponse(HttpStatusCode.BadRequest, "User already exists.");

                return new RestResponse(HttpStatusCode.OK, CreateHeader(("Authorization", session.Token.ToString("N"))), "Successfully registered account.");
            });

            // Login user
            _web.RegisterStaticRoute("POST", "/login", async ctx =>
            {
                if (!TryGetObject<UserCredentialsModel>(ctx, out var creds))
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

            // Get any player stack
            _web.RegisterResourceRoute("GET", "/users/%/stack", async ctx =>
            {
                var username = ctx.Resources[0];

                if (await _server.GetPlayer(username) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Stack));

                return new RestResponse(HttpStatusCode.NotFound, "Player was not found.");
            });

            // Get any player deck
            _web.RegisterResourceRoute("GET", "/users/%/deck", async ctx =>
            {
                var username = ctx.Resources[0];

                if (await _server.GetPlayer(username) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Deck));

                return new RestResponse(HttpStatusCode.NotFound, "Player was not found.");
            });

            // Get own stack
            _web.RegisterStaticRoute("GET", "/stack", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (await _server.GetPlayer(new Session(token)) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Stack));

                return new RestResponse(HttpStatusCode.Unauthorized, "Player not logged in.");
            });

            // Get own deck
            _web.RegisterStaticRoute("GET", "/deck", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (await _server.GetPlayer(new Session(token)) is Player player)
                    return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(player.Deck));

                return new RestResponse(HttpStatusCode.Unauthorized, "Player not logged in.");
            });

            // Edit deck
            _web.RegisterStaticRoute("POST", "/deck", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (!TryGetObject<Guid[]>(ctx, out var cardIds))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid deck specification.");

                if (await _server.SetDeck(new Session(token), cardIds))
                    return new RestResponse(HttpStatusCode.OK, "Successfully updated deck.");

                return new RestResponse(HttpStatusCode.BadRequest, "Invalid session, not exactly five cards or invalid card ID.");
            });

            // Battling
            _web.RegisterStaticRoute("POST", "/battle", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (await _server.InvokeBattle(new Session(token)) is not BattleResult result)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid session or invalid deck configuration.");

                return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(result));
            });

            // View card store ("trading")
            _web.RegisterStaticRoute("GET", "/store/cards", async _ =>
                new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(await _server.GetAvailableStoreCards())));

            // View card store ("trading") eligible entries
            _web.RegisterStaticRoute("GET", "/store/cards/eligible", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (!TryGetObject<Guid>(ctx, out var cardId))
                    return new RestResponse(HttpStatusCode.BadRequest, "Incorrectly formatted card ID.");

                var result = await _server.GetEligibleStoreCards(new Session(token), cardId);

                if (result is null)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid session, invalid card or card not in stack");

                return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(result));
            });

            // Push to card store ("trading push")
            _web.RegisterStaticRoute("POST", "/store/cards", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (!TryGetObject<PushCardStoreModel>(ctx, out var pushModel))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid specification.");

                List<ICardRequirement> translatedReqs = new(pushModel.Requirements?.Count ?? 0);

                foreach (var req in pushModel.Requirements ?? Array.Empty<CardRequirementModel>())
                {
                    switch (req.RequirementType)
                    {
                        case RequirementType.ElementType when req.RequiredElement.HasValue:
                            translatedReqs.Add(new ElementTypeRequirement() { Type = req.RequiredElement.Value });
                            break;

                        case RequirementType.IsMonsterCard:
                            translatedReqs.Add(new IsMonsterCardRequirement());
                            break;

                        case RequirementType.IsSpellCard:
                            translatedReqs.Add(new IsSpellCardRequirement());
                            break;

                        case RequirementType.MinimumDamage when req.MinimumDamage.HasValue:
                            translatedReqs.Add(new MinimumDamageRequirement() { MinimumDamage = req.MinimumDamage.Value });
                            break;

                        default:
                            return new RestResponse(HttpStatusCode.BadRequest, "Unknown requirement type or missing value.");
                    }
                }

                if (!await _server.PushCardToStore(new Session(token), pushModel.CardId, translatedReqs))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid session or card ID.");

                return new RestResponse(HttpStatusCode.OK, "Successfully pushed card to store.");
            });

            // Trade with card store ("trading")
            _web.RegisterStaticRoute("POST", "/store/cards", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                if (!TryGetObject<CardTradeModel>(ctx, out var tradeModel))
                    return new RestResponse(HttpStatusCode.BadRequest, "Incorrectly formatted trade model.");

                if (!await _server.TradeCards(new Session(token), tradeModel.OwnCard, tradeModel.OtherCard))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid session or card specifiction.");

                return new RestResponse(HttpStatusCode.OK, "Successfully traded cards.");
            });

            // Get packages
            _web.RegisterStaticRoute("GET", "/store/packages", async _ =>
                new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(await _server.GetAllPackages())));

            // Get affordable packages (max price in route)
            _web.RegisterResourceRoute("GET", "/store/packages/max/%", async ctx =>
            {
                if (!int.TryParse(ctx.Resources[0], out var maxPrice))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid maximum price.");

                return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(await _server.GetAffordablePackages(maxPrice)));
            });

            // Get affordable packages for player
            _web.RegisterStaticRoute("GET", "/store/packages/affordable", async ctx =>
            {
                if (!ctx.Headers.TryGetValue("Authorization", out var sessionStr))
                    return new RestResponse(HttpStatusCode.Unauthorized, "No authorization supplied.");

                if (!Guid.TryParse(sessionStr, out var token))
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid authorization supplied.");

                var result = await _server.GetAffordablePackages(new Session(token));

                if (result is null)
                    return new RestResponse(HttpStatusCode.BadRequest, "Invalid session.");

                return new RestResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(result));
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
