using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Verifies that all 5 chat-bar PC icon PNGs exist on disk, decode to the
    /// expected pixel dimensions, and produce a usable Texture2D via the same
    /// File.ReadAllBytes + LoadImage path that GameHudController.LoadChatIconDirectly
    /// relies on at runtime.
    /// </summary>
    [TestFixture, Category("Hud")]
    public class ChatBarIconLoadingTests
    {
        private static readonly string StreamingAssets =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Assets", "StreamingAssets"));

        private static readonly (string iconName, string element, int expectedW, int expectedH)[] Icons =
        {
            ("btn_options",     "ChatOptionsBtn",  20, 20),
            ("btn_chat_face",   "FaceBtn",         25, 25),
            ("btn_chat_send",   "SendBtnIcon",     20, 20),
            ("btn_friend",      "ChatFriendBtn",   20, 20),
            ("btn_treasure",    "ChatTreasureBtn", 74, 61),
        };

        [Test]
        public void AllChatIconPngs_ExistOnDisk()
        {
            var artDir = Path.Combine(StreamingAssets, "UI", "HUD", "Art");
            Assert.IsTrue(Directory.Exists(artDir), $"Art directory missing: {artDir}");

            foreach (var (name, _, _, _) in Icons)
            {
                var path = Path.Combine(artDir, name + ".png");
                Assert.IsTrue(File.Exists(path), $"Icon PNG not found: {path}");
            }
        }

        [Test]
        public void AllChatIconPngs_DecodeToExpectedDimensions()
        {
            var artDir = Path.Combine(StreamingAssets, "UI", "HUD", "Art");

            foreach (var (name, _, expectedW, expectedH) in Icons)
            {
                var path = Path.Combine(artDir, name + ".png");
                Assert.IsTrue(File.Exists(path), $"Icon PNG not found: {path}");

                var data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                var loaded = tex.LoadImage(data);

                Assert.IsTrue(loaded, $"LoadImage failed for {name}.png");
                Assert.AreEqual(expectedW, tex.width,
                    $"{name}.png width expected {expectedW} but got {tex.width}");
                Assert.AreEqual(expectedH, tex.height,
                    $"{name}.png height expected {expectedH} but got {tex.height}");
                Assert.AreEqual(TextureFormat.ARGB32, tex.format,
                    $"{name}.png should decode as ARGB32, got {tex.format}");

                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void AllChatIconPngs_HaveOpaquePixels()
        {
            var artDir = Path.Combine(StreamingAssets, "UI", "HUD", "Art");

            foreach (var (name, _, expectedW, expectedH) in Icons)
            {
                var path = Path.Combine(artDir, name + ".png");
                var data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);

                var pixels = tex.GetPixels32();
                var opaque = 0;
                foreach (var p in pixels)
                    if (p.a > 0) opaque++;

                Assert.Greater(opaque, 0,
                    $"{name}.png has zero opaque pixels — all transparent!");

                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void AllChatIconPngs_HaveExpectedOpaqueCount()
        {
            var artDir = Path.Combine(StreamingAssets, "UI", "HUD", "Art");

            // Known opaque pixel counts (after full-alpha fix). Updated 2026-06-20.
            var expectedOpaque = new (string name, int minOpaque, int maxOpaque)[]
            {
                ("btn_options",     200, 280),
                ("btn_chat_face",   150, 250),
                ("btn_chat_send",   200, 280),
                ("btn_friend",      220, 300),
                ("btn_treasure",   3000, 4000),
            };

            foreach (var (name, minOpaque, maxOpaque) in expectedOpaque)
            {
                var path = Path.Combine(artDir, name + ".png");
                var data = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(data);

                var pixels = tex.GetPixels32();
                var opaque = 0;
                foreach (var p in pixels)
                    if (p.a > 0) opaque++;

                Assert.GreaterOrEqual(opaque, minOpaque,
                    $"{name}.png has {opaque} opaque px, expected at least {minOpaque}");
                Assert.LessOrEqual(opaque, maxOpaque,
                    $"{name}.png has {opaque} opaque px, expected at most {maxOpaque}");

                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void LoadChatIconDirectly_ProducesUsableTexture()
        {
            // This simulates the exact code path used by the runtime
            // LoadChatIconDirectly() method in GameHudController.
            var artDir = Path.Combine(StreamingAssets, "UI", "HUD", "Art");

            foreach (var (name, element, expectedW, expectedH) in Icons)
            {
                var pngPath = Path.Combine(artDir, name + ".png");
                Assert.IsTrue(File.Exists(pngPath), $"PNG missing: {pngPath}");

                var data = File.ReadAllBytes(pngPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.IsTrue(tex.LoadImage(data),
                    $"{name}.png failed to load via LoadImage");

                Assert.AreEqual(expectedW, tex.width,
                    $"{name}.png width mismatch");
                Assert.AreEqual(expectedH, tex.height,
                    $"{name}.png height mismatch");

                // Verify the texture can be assigned as a style background
                var ve = new VisualElement();
                ve.style.backgroundImage = new StyleBackground(tex);
                var assignedTex = ve.style.backgroundImage.value.texture;
                Assert.IsNotNull(assignedTex,
                    $"{name}.png texture not assignable to VisualElement.backgroundImage");
                Assert.AreEqual(expectedW, assignedTex.width);
                Assert.AreEqual(expectedH, assignedTex.height);

                Object.DestroyImmediate(tex);
            }
        }
    }
}
