using NUnit.Framework;
using static MtcgServerTests.Constants;

namespace MtcgServerTests
{
    public class PlayerLogonTest
    {
        private MtcgServer.MtcgServer _server;

        [SetUp]
        public void Setup()
        {
            _server = new MtcgServer.MtcgServer(null, null, null);
        }

        [Test]
        public void TestLogin()
        {
            var session = _server.Login(DemoUserName, DemoUserPass);

            Assert.NotNull(session);
        }

        [Test]
        public void TestLogout()
        {
            var session = _server.Login(DemoUserName, DemoUserPass);
            var logoutResult = _server.Logout(session);

            Assert.IsTrue(logoutResult);
        }
    }
}