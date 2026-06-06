using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Locks the mobile bottom HUD bar to the authentic PC source art and INI layout.
    /// Art source: \spr\Ui3\thanhcongcu\jx1024.spr (uid 917565dd, in 1024.pak) decoded to
    /// a 1024x769 composite; the opaque bottom band y[680-768] = 1024x89 is the bar.
    /// Layout spec source: PC UI INI 工具控制条 (uid dc11ac12) + 主界面玩家信息窗口 (uid e3b06434),
    /// extracted from 1024.pak / VLTKUI_1024x768.pak. NO cropping from pc-evidence screenshots.
    /// </summary>
    [TestFixture]
    public class HudBottomBarAuthenticityTests
    {
        private const string BottomBarArtRelative = "UI/HUD/Art/bottom_bar_bg.png";

        // Authentic PC bottom-bar band extracted from jx1024.spr (full width, opaque band height).
        private const int ExpectedWidth = 1024;
        private const int ExpectedHeight = 89;

        [Test]
        public void BottomBarArt_ExistsAndMatchesAuthenticPcSprDimensions()
        {
            var path = Path.Combine(Application.dataPath, BottomBarArtRelative);
            Assert.IsTrue(File.Exists(path), $"Authentic PC bottom-bar art missing: {path}");

            var tex = new Texture2D(2, 2);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "bottom_bar_bg.png must be a valid PNG.");
            Assert.AreEqual(ExpectedWidth, tex.width,
                "Bottom bar width must equal the authentic jx1024.spr band width (1024), not a screenshot crop.");
            Assert.AreEqual(ExpectedHeight, tex.height,
                "Bottom bar height must equal the authentic jx1024.spr opaque band height (89), not a screenshot crop.");
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void BottomBarArt_IsNotTheLegacyScreenshotCrop()
        {
            // The discarded crop from pc-evidence/hud.png was 800x82. Guard against regressing to it.
            var path = Path.Combine(Application.dataPath, BottomBarArtRelative);
            Assert.IsTrue(File.Exists(path), $"Bottom-bar art missing: {path}");

            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var isLegacyCrop = tex.width == 800 && tex.height == 82;
            Object.DestroyImmediate(tex);
            Assert.IsFalse(isLegacyCrop,
                "Bottom bar art regressed to the 800x82 screenshot crop. Must use authentic jx1024.spr art (1024x89).");
        }

        [Test]
        public void BottomBarLayoutSpec_MatchesPcIniCoordinates()
        {
            // PC INI 工具控制条 (uid dc11ac12) in 1024-space. These are the source-of-truth
            // button coordinates that any dynamic click-zone port must reproduce.
            var spec = HudBottomBarPcSpec.ToolControlBar;

            Assert.AreEqual(728, spec["Status"].top, "Status button Top must match PC INI.");
            Assert.AreEqual(580, spec["Status"].left, "Status button Left must match PC INI.");
            Assert.AreEqual(675, spec["Run"].top, "Run button Top must match PC INI.");
            Assert.AreEqual(687, spec["Run"].left, "Run button Left must match PC INI.");
            Assert.AreEqual(719, spec["Horse"].left, "Horse button Left must match PC INI.");
            Assert.AreEqual(766, spec["Team"].left, "Team button Left must match PC INI.");

            // Every menu-row button sits at Top=728; every action-row button at Top=675.
            foreach (var name in new[] { "Status", "Items", "ItemEx", "Skills", "Task", "Team", "Faction", "ChatRoom" })
                Assert.AreEqual(728, spec[name].top, $"{name} must be on the menu row (Top=728) per PC INI.");
            foreach (var name in new[] { "Sit", "Run", "Horse", "Exchange", "Rec", "PK" })
                Assert.AreEqual(675, spec[name].top, $"{name} must be on the action row (Top=675) per PC INI.");
        }
    }
}
