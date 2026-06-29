using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;  // ChatService, ChatChannel for T2.1 channel color tests

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

/// <summary>
/// T2.1 (port-pc-chat-bar-parity) — regression guard: verifies ChatService channel
/// colors match the PC TextColor values from uiconfig.ini / 7e20a7ac.ini before the
/// HudChatBarController renders them. These colors are PC-authentic (data layer) and
/// must not drift.
/// </summary>
[TestFixture, Category("Chat")]
public class ChatChannelColorFidelityTests
{
private static void AssertColorApprox(string label, Color actual, float r, float g, float b, float tol = 0.02f)
{
Assert.AreEqual(r, actual.r, tol, $"{label} R mismatch: expected ~{r}, got {actual.r}");
Assert.AreEqual(g, actual.g, tol, $"{label} G mismatch: expected ~{g}, got {actual.g}");
Assert.AreEqual(b, actual.b, tol, $"{label} B mismatch: expected ~{b}, got {actual.b}");
}

[Test]
public void ChannelColors_MatchPC_TextColorValues()
{
// PC CH_SYSTEM TextColor = 255,0,0 (red)
AssertColorApprox("System", ChatService.ChannelColor(ChatChannel.System), 1f, 0f, 0f);
// PC CH_TEAM TextColor = 64,190,255 (blue)
AssertColorApprox("Team", ChatService.ChannelColor(ChatChannel.Team), 0.251f, 0.745f, 1f);
// PC CH_WORLD TextColor = 146,255,143 (green)
AssertColorApprox("World", ChatService.ChannelColor(ChatChannel.World), 0.573f, 1f, 0.561f);
// PC CH_TONG TextColor = 255,244,0 (yellow)
AssertColorApprox("Guild", ChatService.ChannelColor(ChatChannel.Guild), 1f, 0.957f, 0f);
}
}

/// <summary>
/// T1.1 (port-pc-chat-bar-parity) — guard test: verifies all 15 new PC chat-bar
/// SPR PNGs exist in BOTH art roots (editor + StreamingAssets). Runtime loads via
/// StreamingAssets; staging only the editor copy is a silent load failure.
/// Art is pre-staged from decoded PC SPRs (hash-resolved from 7e20a7ac.ini paths).
/// </summary>
[TestFixture, Category("Chat")]
public class HudChatBarArtTests
{
    // The 15 PC chat-bar art pieces decoded from vltksource_new SPRs.
    private static readonly string[] NewChatBarPngs =
    {
        "chat_bar_top",              // 8fa68495 聊天条顶部改
        "chat_bar_bottom",           // bdf9af98 聊天条底部改
        "chat_bar_middle",           // 3483ec02 聊天条中部改
        "btn_chat_shadow",           // bcca4952 聊天条阴影按钮
        "btn_chat_channel_on",       // 3b255f40 频道开与关a
        "btn_chat_channel_off",      // 34fc44d5 频道开与关b
        "btn_chat_sys_toggle",       // 7c6eaab0 提示信息窗－开关
        "btn_chat_sys_up",           // b3e52a98 提示信息窗－上
        "btn_chat_sys_down",         // af1cbe4c 提示信息窗－下
        "btn_chat_scroll_thumb_pc",  // 23fe2a10 通用拖动条
        "chat_icon_self_pc",         // 50304af7 聊天频道图示－自己说
        "chat_icon_friend_pc",       // 2c66b90e 聊天频道图示－好友频道
        "chat_icon_stranger_pc",     // 69fbc7e6 聊天频道图示－密人频道
        "btn_chat_channel_friend",   // 7addeacc 主界面按钮-好友频道选择
        "btn_chat_channel_stranger", // 3be3a09f 主界面按钮-密人频道选择
    };

    [Test]
    public void NewChatBarPngs_ExistInBothArtRoots()
    {
        // Application.dataPath = <project>/Assets in EditMode.
        var editorArtDir = Path.Combine(Application.dataPath, "UI", "HUD", "Art");
        var streamingArtDir = Path.Combine(Application.dataPath, "StreamingAssets", "UI", "HUD", "Art");

        Assert.IsTrue(Directory.Exists(editorArtDir),
            $"Editor art dir missing: {editorArtDir}");
        Assert.IsTrue(Directory.Exists(streamingArtDir),
            $"StreamingAssets art dir missing: {streamingArtDir}");

        foreach (var name in NewChatBarPngs)
        {
            var editorPath = Path.Combine(editorArtDir, name + ".png");
            var streamingPath = Path.Combine(streamingArtDir, name + ".png");

            Assert.IsTrue(File.Exists(editorPath),
                $"Editor art missing: {name}.png at {editorPath}");
            Assert.IsTrue(File.Exists(streamingPath),
                $"StreamingAssets art missing: {name}.png at {streamingPath} " +
                "(runtime load will silently fail — stage to BOTH roots)");
        }
    }
}
