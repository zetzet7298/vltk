using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>Fail-closed provenance gate for missile 352 status-slot audio.</summary>
    public class TangMenImpactAudioParityTests
    {
        private const string PcFlightPath = "\\sound\\skill\\飘雪穿云.wav";
        private const string RelativePath = "sound/skill/飘雪穿云.wav";
        private const string Sha256 = "e0c82072b554cb3f69d82c4fe4b24dc106f9bf0d7cc4dfde96a9491e382fb39a";
        private const int ByteCount = 69204;

        private static string ProjectRoot => Directory.GetCurrentDirectory();
        private static string MissileTablePath => Path.Combine(ProjectRoot, "Assets", "StreamingAssets", "Reference", "PcAttrib", "missles1.txt");
        private static string RuntimePath => Path.Combine(ProjectRoot, "Assets", "StreamingAssets", "sound", "skill", "飘雪穿云.wav");

        [Test]
        public void Missile352_SndFile2IsFlight_AndSndFile4CollisionIsSilent()
        {
            var registry = PcMissileFullVisualRegistry.ParseFromFile(MissileTablePath);
            Assert.IsTrue(registry.TryGet(352, out var missile), "missile 352 missing from pinned PC table");
            Assert.AreEqual(PcFlightPath, missile.PrimaryFlight.soundPath, "SndFile2 / MS_DoFly");
            Assert.IsNotNull(missile.PrimaryCollision, "AnimFile4 / MS_DoCollision must remain inspectable");
            Assert.IsTrue(string.IsNullOrEmpty(missile.PrimaryCollision.soundPath),
                "SndFile4 is empty: no PC collision/impact WAV exists for missile 352");
        }

        [Test]
        public void FlightWav_IsExactAndReachableThroughAudioServiceStreamingPath()
        {
            Assert.IsTrue(File.Exists(RuntimePath), $"vendored PC flight WAV missing: {RuntimePath}");
            var bytes = File.ReadAllBytes(RuntimePath);
            Assert.AreEqual(ByteCount, bytes.Length);
            Assert.AreEqual(Sha256, Sha256Hex(bytes));
            CollectionAssert.AreEqual(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' }, Slice(bytes, 0, 4));
            CollectionAssert.AreEqual(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' }, Slice(bytes, 8, 4));
            Assert.AreEqual((ushort)1, BitConverter.ToUInt16(bytes, 20), "PCM format");
            Assert.AreEqual((ushort)2, BitConverter.ToUInt16(bytes, 22), "stereo");
            Assert.AreEqual((uint)22050, BitConverter.ToUInt32(bytes, 24), "sample rate");
            Assert.AreEqual((ushort)16, BitConverter.ToUInt16(bytes, 34), "bits/sample");

            var uri = new AudioService().ResolveStreamingAssetsUri(RelativePath);
            Assert.IsNotNull(uri);
            Assert.IsTrue(uri.Replace('\\', '/').EndsWith(RelativePath, StringComparison.Ordinal),
                "AudioService must load this exact logical path from StreamingAssets");
        }

        private static byte[] Slice(byte[] bytes, int offset, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(bytes, offset, result, 0, count);
            return result;
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
