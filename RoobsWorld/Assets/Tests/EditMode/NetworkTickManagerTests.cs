using System;
using NUnit.Framework;
using RoobsWorld.Core;
using RoobsWorld.Multiplayer;
using RoobsWorld.Stages.Cell;
using UnityEngine;

namespace RoobsWorld.Tests
{
    /// <summary>
    /// EditMode tests for NetworkTickManager helper methods and RemotePlayerCell colour palette.
    /// MonoBehaviour-dependent tests (Update loop, FindObjects) require PlayMode.
    /// </summary>
    public class NetworkTickManagerTests
    {
        [SetUp]
        public void SetUp() => EventBus.Clear();

        [TearDown]
        public void TearDown() => EventBus.Clear();

        // ---- Prepend / StripTag ----

        [Test]
        public void Prepend_Adds_Tag_Byte_At_Front()
        {
            var payload = new byte[] { 0xAA, 0xBB };
            var packet  = NetworkTickManager.Prepend(NetworkTickManager.TagSession, payload);
            Assert.AreEqual(NetworkTickManager.TagSession, packet[0]);
            Assert.AreEqual(0xAA, packet[1]);
            Assert.AreEqual(0xBB, packet[2]);
        }

        [Test]
        public void Prepend_Length_Is_Payload_Plus_One()
        {
            var payload = new byte[10];
            var packet  = NetworkTickManager.Prepend(NetworkTickManager.TagInput, payload);
            Assert.AreEqual(11, packet.Length);
        }

        [Test]
        public void StripTag_Returns_Payload_Without_First_Byte()
        {
            var packet  = new byte[] { NetworkTickManager.TagSession, 0x01, 0x02, 0x03 };
            var payload = NetworkTickManager.StripTag(packet);
            Assert.AreEqual(3, payload.Length);
            Assert.AreEqual(0x01, payload[0]);
            Assert.AreEqual(0x03, payload[2]);
        }

        [Test]
        public void StripTag_SingleByte_Packet_Returns_Empty()
        {
            var result = NetworkTickManager.StripTag(new byte[] { 0x01 });
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void StripTag_Null_Returns_Empty()
        {
            var result = NetworkTickManager.StripTag(null);
            Assert.AreEqual(0, result.Length);
        }

        [Test]
        public void GetTag_Returns_First_Byte()
        {
            Assert.AreEqual(NetworkTickManager.TagSession, NetworkTickManager.GetTag(new byte[] { 0x01, 0xFF }));
            Assert.AreEqual(NetworkTickManager.TagInput,   NetworkTickManager.GetTag(new byte[] { 0x02, 0x00 }));
        }

        [Test]
        public void GetTag_Null_Returns_Zero()
        {
            Assert.AreEqual(0, NetworkTickManager.GetTag(null));
        }

        // ---- Prepend / StripTag round-trip ----

        [Test]
        public void Prepend_Then_StripTag_Recovers_Payload()
        {
            var original = new byte[] { 10, 20, 30, 40, 50 };
            var packet   = NetworkTickManager.Prepend(NetworkTickManager.TagSession, original);
            var stripped = NetworkTickManager.StripTag(packet);
            CollectionAssert.AreEqual(original, stripped);
        }

        // ---- BroadcastInterval constant ----

        [Test]
        public void BroadcastInterval_Is_Positive_And_At_Most_100ms()
        {
            Assert.Greater(NetworkTickManager.BroadcastInterval, 0f);
            Assert.LessOrEqual(NetworkTickManager.BroadcastInterval, 0.1f);
        }

        // ---- RemotePlayerCell colour palette ----

        [Test]
        public void GetColorForPlayer_Zero_Returns_Green()
        {
            var c = RemotePlayerCell.GetColorForPlayer(0);
            Assert.Greater(c.g, c.r);
            Assert.Greater(c.g, c.b);
        }

        [Test]
        public void GetColorForPlayer_Distinct_For_Players_1_2_3()
        {
            var c1 = RemotePlayerCell.GetColorForPlayer(1);
            var c2 = RemotePlayerCell.GetColorForPlayer(2);
            var c3 = RemotePlayerCell.GetColorForPlayer(3);
            Assert.AreNotEqual(c1, c2);
            Assert.AreNotEqual(c2, c3);
            Assert.AreNotEqual(c1, c3);
        }

        [Test]
        public void GetColorForPlayer_OutOfRange_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() => RemotePlayerCell.GetColorForPlayer(-1));
            Assert.DoesNotThrow(() => RemotePlayerCell.GetColorForPlayer(99));
        }

        // ---- packet tag constants are distinct ----

        [Test]
        public void Tag_Constants_Are_Distinct()
        {
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagInput);
            Assert.AreNotEqual(NetworkTickManager.TagSession, NetworkTickManager.TagVoice);
            Assert.AreNotEqual(NetworkTickManager.TagInput, NetworkTickManager.TagVoice);
        }

        [Test]
        public void ProximityVoiceChat_Volume_Fades_By_Distance()
        {
            Assert.AreEqual(1f, ProximityVoiceChat.CalculateVolume(0f));
            Assert.AreEqual(1f, ProximityVoiceChat.CalculateVolume(ProximityVoiceChat.FullVolumeRadius));
            Assert.AreEqual(0f, ProximityVoiceChat.CalculateVolume(ProximityVoiceChat.VoiceRadius));

            float mid = (ProximityVoiceChat.FullVolumeRadius + ProximityVoiceChat.VoiceRadius) * 0.5f;
            Assert.Greater(ProximityVoiceChat.CalculateVolume(mid), 0f);
            Assert.Less(ProximityVoiceChat.CalculateVolume(mid), 1f);
        }

        // ---- GenomeChecksum integration (confirm stable through serialization) ----

        [Test]
        public void GenomeChecksum_Stable_After_Prepend_StripTag()
        {
            // Simulate: host builds PlayerNetState, embeds checksum; client reads it back.
            var genome = new Genome { Speed = 4f, Size = 3f, Attack = 2f };
            uint checksum = GenomeChecksum.Compute(genome);

            var state = new PlayerNetState
            {
                PlayerId       = 1,
                PosX           = 5f,
                PosY           = -3f,
                GenomeChecksum = checksum,
                Mass           = 2.5f,
                IsAlive        = true
            };

            // Serialize the full session containing this player
            var session = new SessionNetState
            {
                Tick    = 42u,
                Players = new[] { state }
            };
            var payload  = NetworkSerializer.Serialize(session);
            var packet   = NetworkTickManager.Prepend(NetworkTickManager.TagSession, payload);
            var stripped = NetworkTickManager.StripTag(packet);
            var decoded  = NetworkSerializer.DeserializeSession(stripped);

            Assert.AreEqual(checksum, decoded.Players[0].GenomeChecksum);
        }

        // ---- NullNetworkTransport: Activate with null session is safe ----

        [Test]
        public void Activate_With_Null_Session_Does_Not_Throw()
        {
            var go  = new GameObject("TestTick");
            var mgr = go.AddComponent<NetworkTickManager>();
            Assert.DoesNotThrow(() => mgr.Activate(null, null, null));
            mgr.Deactivate();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
