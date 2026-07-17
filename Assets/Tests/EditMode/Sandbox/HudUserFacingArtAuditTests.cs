using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class HudUserFacingArtAuditTests
    {
        private static readonly string[] UserFacingChineseKeywords =
        {
            "关闭", "刷新", "技能", "战斗", "生活", "聊天频道", "主界面按钮", "好友－关闭", "好友－查找", "帮派－关闭", "离开队伍", "踢出队伍", "邀请加入", "队长移交"
        };

        [Test]
        public void HudArtCatalog_UserFacingChineseEntriesHaveVietnameseAssetOrLocalizationKey()
        {
            var artRoot = Path.Combine(Application.dataPath, "StreamingAssets/UI/HUD/Art");
            foreach (var pair in HudUserFacingArtCatalog.All)
            {
                Assert.IsTrue(HudUserFacingArtCatalog.ContainsCjk(pair.Value.chineseArtName), $"Audit entry should point to a Chinese PC asset: {pair.Key}");
                Assert.IsTrue(pair.Value.HasVietnameseAsset || pair.Value.HasLocalizationKey, $"{pair.Key} must resolve to a Vietnamese asset or localization key.");
                Assert.IsFalse(HudUserFacingArtCatalog.ContainsCjk(pair.Value.vietnameseArtName), $"Vietnamese asset name must not contain Chinese characters: {pair.Value.vietnameseArtName}");
                Assert.IsFalse(HudUserFacingArtCatalog.ContainsCjk(pair.Value.localizationKey), $"Localization key must not contain Chinese characters: {pair.Value.localizationKey}");
                Assert.IsFalse(string.IsNullOrWhiteSpace(pair.Value.vietnameseText), $"{pair.Key} must document Vietnamese user-facing text.");

                if (!pair.Value.HasVietnameseAsset)
                    continue;

                var png = Path.Combine(artRoot, HudUserFacingArtCatalog.VietnameseFolderName, pair.Value.vietnameseArtName + ".png");
                Assert.IsTrue(File.Exists(png), $"Missing Vietnamese HUD art for {pair.Key}: {png}");
            }
        }

        [Test]
        public void HudArtResolver_UsesVietnameseAssetForKnownBakedTextSprites()
        {
            const string artRoot = "StreamingAssets/UI/HUD/Art";
            Assert.AreEqual("StreamingAssets/UI/HUD/Art/vi/btn_close_skill_02_vi.png", Normalize(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "技能－关闭_02")));
            Assert.AreEqual("StreamingAssets/UI/HUD/Art/vi/btn_refresh_00_vi.png", Normalize(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "刷新列表_00")));
            Assert.AreEqual("StreamingAssets/UI/HUD/Art/vi/chat_main_world_vi.png", Normalize(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "主界面按钮-世界频道选择")));
            Assert.AreEqual("StreamingAssets/UI/HUD/Art/btn_pk.png", Normalize(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "btn_pk")));
        }

        [Test]
        public void HudArtAudit_NoKeywordMatchedChinesePngIsMissingCatalogEntry()
        {
            var artRoot = Path.Combine(Application.dataPath, "StreamingAssets/UI/HUD/Art");
            Assert.IsTrue(Directory.Exists(artRoot), $"HUD art root missing: {artRoot}");

            foreach (var png in Directory.GetFiles(artRoot, "*.png", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileNameWithoutExtension(png);
                if (!HudUserFacingArtCatalog.ContainsCjk(name) || !LooksUserFacing(name))
                    continue;

                Assert.IsTrue(HudUserFacingArtCatalog.TryGetReplacement(name, out _), $"User-facing HUD art with Chinese baked text needs Vietnamese asset/localization catalog entry: {png}");
            }
        }

        [Test]
        public void HudArtAuditManifest_ListsAllChineseNamedHudPngs()
        {
            var artRoot = Path.Combine(Application.dataPath, "StreamingAssets/UI/HUD/Art");
            var manifest = Path.Combine(artRoot, "hud_user_facing_art_audit.json");
            Assert.IsTrue(File.Exists(manifest), $"Missing HUD audit manifest: {manifest}");

            var json = File.ReadAllText(manifest);
            var actualChinesePngCount = 0;
            foreach (var png in Directory.GetFiles(artRoot, "*.png", SearchOption.TopDirectoryOnly))
            {
                if (!HudUserFacingArtCatalog.ContainsCjk(png))
                    continue;

                actualChinesePngCount++;
                Assert.IsTrue(json.Contains(Path.GetFileName(png)), $"Audit manifest is missing Chinese HUD PNG: {png}");
            }

            Assert.IsTrue(json.Contains($"\"totalChineseNamedPng\": {actualChinesePngCount}"), "Audit manifest totalChineseNamedPng is stale.");
        }

        [Test]
        public void PcVietnameseSkillPanelSprites_MatchPinnedPcExportDigests()
        {
            var root = Path.Combine(Application.dataPath, "UI/HUD/Art/vi");
            AssertFileSha256(root, "skill_panel_vi.png", "58404f664432b3be6dd6d13b15474fb69123ce896341ffaf4ed0fa244d85fe1c");
            AssertFileSha256(root, "skill_panel_combat_tab_vi.png", "f1652a508812515aba59928ecb765ea0ca367ca43548b1017baff02686e0cf48");
            AssertFileSha256(root, "btn_close_skill_00_vi.png", "a413b502a20f8d56fbb0e36e5569538009b2c64d917cc8278ab80fe3b6aba52a");
            AssertFileSha256(root, "btn_close_skill_01_vi.png", "f950b7118e1f073a4269043146f7ddad86e1ca543aaf43df6de5d7d3058f6067");
            AssertFileSha256(root, "btn_close_skill_02_vi.png", "1ce84f76877552a42018e11099201bccc26b8abcb9e9203db09741355c250c1c");
            AssertFileSha256(root, "skill_fight_tab_01_vi.png", "1d432f49b7918ff89516ff0cd378f7ca8995883e58032fafb9dcb9543225a6e4");
        }

        private static bool LooksUserFacing(string name)
        {
            foreach (var keyword in UserFacingChineseKeywords)
            {
                if (name.Contains(keyword))
                    return true;
            }
            return false;
        }

        private static void AssertFileSha256(string root, string fileName, string expectedSha256)
        {
            var path = Path.Combine(root, fileName);
            Assert.IsTrue(File.Exists(path), $"Missing PC Vietnamese skill-panel export: {path}");
            using var sha = SHA256.Create();
            using var stream = File.OpenRead(path);
            var actual = System.BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
            Assert.AreEqual(expectedSha256, actual, $"PC Vietnamese skill-panel export drifted: {fileName}");
        }

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}
