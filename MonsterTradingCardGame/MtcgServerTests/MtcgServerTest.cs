using MtcgServer;
using NUnit.Framework;
using System;
using static MtcgServerTests.Constants;

namespace MtcgServerTests
{
    public class MtcgServerTest
    {
        private MtcgServer.MtcgServer _server;
        private Session _session;

        [SetUp]
        public void Setup()
        {
            _server = new MtcgServer.MtcgServer(null, null, null);
            _session = _server.Login(DemoUserName, DemoUserPass);
        }

        [Test]
        public void TestGetPlayer()
        {
            var player = _server.GetPlayer(_session);

            Assert.AreEqual(DemoUserId, player.ID);
            Assert.AreEqual(DemoUserName, player.Name);
        }
    }
}
