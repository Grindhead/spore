using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for WebSocketNetworkTransport helper methods and NullTransport baseline.
    /// Full socket tests require a running server (PlayMode / integration test).
    /// </summary>
    public class WebSocketTransportTests
    {
        // ---- access private parser helpers via reflection ----

        private static string ParseString(string json, string key) =>
            (string)typeof(WebSocketNetworkTransport)
                .GetMethod("ParseStringField", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { json, key });

        private static int ParseInt(string json, string key) =>
            (int)typeof(WebSocketNetworkTransport)
                .GetMethod("ParseIntField", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { json, key });

        private static bool ParseBool(string json, string key) =>
            (bool)typeof(WebSocketNetworkTransport)
                .GetMethod("ParseBoolField", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { json, key });

        private static IReadOnlyList<PublicLobbyInfo> ParsePublicLobbies(string encoded) =>
            (IReadOnlyList<PublicLobbyInfo>)typeof(WebSocketNetworkTransport)
                .GetMethod("ParsePublicLobbies", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, new object[] { encoded });

        // ---- ParseStringField ----

        [Test]
        public void ParseString_Extracts_Simple_Value()
        {
            string json = "{\"type\":\"created\",\"code\":\"ABCD12\"}";
            Assert.AreEqual("created", ParseString(json, "type"));
            Assert.AreEqual("ABCD12",  ParseString(json, "code"));
        }

        [Test]
        public void ParseString_Missing_Key_Returns_Null()
        {
            Assert.IsNull(ParseString("{\"type\":\"ping\"}", "code"));
        }

        [Test]
        public void ParseString_Empty_Json_Returns_Null()
        {
            Assert.IsNull(ParseString("{}", "type"));
        }

        // ---- ParseIntField ----

        [Test]
        public void ParseInt_Extracts_PlayerId()
        {
            Assert.AreEqual(3, ParseInt("{\"type\":\"joined\",\"playerId\":3}", "playerId"));
        }

        [Test]
        public void ParseInt_Extracts_Zero()
        {
            Assert.AreEqual(0, ParseInt("{\"playerId\":0}", "playerId"));
        }

        [Test]
        public void ParseInt_Missing_Key_Returns_Negative_One()
        {
            Assert.AreEqual(-1, ParseInt("{\"type\":\"ping\"}", "playerId"));
        }

        [Test]
        public void ParseInt_Extracts_HostPlayerId()
        {
            string json = "{\"type\":\"joined\",\"code\":\"XY1234\",\"playerId\":2,\"hostPlayerId\":0}";
            Assert.AreEqual(0, ParseInt(json, "hostPlayerId"));
            Assert.AreEqual(2, ParseInt(json, "playerId"));
        }

        [Test]
        public void ParseString_Extracts_LobbyId()
        {
            string json = "{\"type\":\"player_lobby\",\"playerId\":2,\"lobbyId\":\"cell-0_1\"}";
            Assert.AreEqual("player_lobby", ParseString(json, "type"));
            Assert.AreEqual("cell-0_1", ParseString(json, "lobbyId"));
            Assert.AreEqual(2, ParseInt(json, "playerId"));
        }

        [Test]
        public void ParsePublicLobbies_Extracts_Code_And_Player_Counts()
        {
            var lobbies = ParsePublicLobbies("ABC123,1,4,Tide%20Pool;XYZ789,3,4,Open%20Water");

            Assert.AreEqual(2, lobbies.Count);
            Assert.AreEqual("ABC123", lobbies[0].Code);
            Assert.AreEqual("Tide Pool", lobbies[0].ServerName);
            Assert.AreEqual(1, lobbies[0].Players);
            Assert.AreEqual(4, lobbies[0].MaxPlayers);
            Assert.AreEqual("XYZ789", lobbies[1].Code);
            Assert.AreEqual("Open Water", lobbies[1].ServerName);
            Assert.AreEqual(3, lobbies[1].Players);
        }

        [Test]
        public void ParsePublicLobbies_Empty_Returns_Empty_List()
        {
            Assert.AreEqual(0, ParsePublicLobbies("").Count);
        }

        // ---- ParseBoolField ----

        [Test]
        public void ParseBool_True_Returns_True()
        {
            Assert.IsTrue(ParseBool("{\"wasHost\":true}", "wasHost"));
        }

        [Test]
        public void ParseBool_False_Returns_False()
        {
            Assert.IsFalse(ParseBool("{\"wasHost\":false}", "wasHost"));
        }

        [Test]
        public void ParseBool_Missing_Key_Returns_False()
        {
            Assert.IsFalse(ParseBool("{\"type\":\"leave\"}", "wasHost"));
        }

        // ---- constructor / offline safety ----

        [Test]
        public void Constructor_Valid_Url_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() =>
            {
                var t = new WebSocketNetworkTransport("ws://localhost:7788");
                t.Dispose();
            });
        }

        [Test]
        public void Constructor_With_Token_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() =>
            {
                var t = new WebSocketNetworkTransport("ws://localhost:7788", "my-game-token");
                t.Dispose();
            });
        }

        [Test]
        public void IsConnected_False_Before_ConnectAsync()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            Assert.IsFalse(t.IsConnected);
            t.Dispose();
        }

        [Test]
        public void TryReceive_Returns_Null_When_Not_Connected()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            var data = t.TryReceive(out int from);
            Assert.IsNull(data);
            Assert.AreEqual(-1, from);
            t.Dispose();
        }

        [Test]
        public void Send_Does_Not_Throw_When_Not_Connected()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            Assert.DoesNotThrow(() => t.Send(0, new byte[] { 1, 2, 3 }));
            t.Dispose();
        }

        [Test]
        public void Broadcast_Does_Not_Throw_When_Not_Connected()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            Assert.DoesNotThrow(() => t.Broadcast(new byte[] { 4, 5, 6 }));
            t.Dispose();
        }

        [Test]
        public void Disconnect_Does_Not_Throw_When_Not_Connected()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            Assert.DoesNotThrow(() => t.Disconnect());
        }

        [Test]
        public void Dispose_Idempotent()
        {
            var t = new WebSocketNetworkTransport("ws://localhost:7788");
            Assert.DoesNotThrow(() => { t.Dispose(); t.Dispose(); });
        }
    }
}
