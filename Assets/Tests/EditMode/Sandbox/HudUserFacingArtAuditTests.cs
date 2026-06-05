using System.IO;
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

        private static bool LooksUserFacing(string name)
        {
            foreach (var keyword in UserFacingChineseKeywords)
            {
                if (name.Contains(keyword))
                    return true;
            }
            return false;
        }

        private static string Normalize(string path) => path.Replace('\\', '/');
    }
}
