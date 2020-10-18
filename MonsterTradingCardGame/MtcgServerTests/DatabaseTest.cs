using NUnit.Framework;
using System;
using static MtcgServerTests.Constants;

namespace MtcgServerTests
{
    public class DatabaseTest
    {
        private MtcgServer.IDatabase _db;

        [SetUp]
        public void Setup()
        {
            _db = new MtcgServer.PostgreSqlDatabase();
        }

        [Test]
        public void TestSearchPlayer()
        {
            var playerId = _db.SearchPlayer(DemoUserName);

            Assert.AreEqual(DemoUserId, playerId);
        }

        [Test]
        public void TestReadPlayer()
        {
            var player = _db.ReadPlayer(DemoUserId);

            Assert.AreEqual(DemoUserId, player.ID);
            Assert.AreEqual(DemoUserName, player.Name);
        }
    }
}
