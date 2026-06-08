using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Locks the mobile HUD to PC-derived visual pixels while allowing a mobile-first layout.
    /// Sources: /pc-evidence/pc_hud.png crops plus verified PC top-bar SPR art.
    /// </summary>
    [TestFixture]
    public class HudBottomBarAuthenticityTests
    {
        private const string ArtRoot = "UI/HUD/Art";

        [Test]
        public void PcDerivedHudArt_ExistsWithExpectedDimensions()
        {
            AssertTextureSize("top_status_strip.png", 552, 17);
            AssertTextureSize("bar_hp_fill.png", 106, 11);
            AssertTextureSize("bar_mp_fill.png", 106, 11);
            AssertTextureSize("bar_stamina_fill.png", 106, 11);
            AssertTextureSize("bar_exp_fill.png", 106, 11);
            AssertTextureSize("btn_primary_attack.png", 42, 42);
            AssertTextureSize("btn_skill_empty_pc.png", 42, 42);
            AssertTextureSize("btn_treasure.png", 58, 58);
        }

        [Test]
        public void MobileCombatButtonCrops_PreservePcScreenshotPixels()
        {
            var pc = LoadTexture(Path.GetFullPath(Path.Combine(Application.dataPath, "../pc-evidence/pc_hud.png")));
            var attack = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_primary_attack.png"));
            var empty = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_skill_empty_pc.png"));
            var treasure = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_treasure.png"));

            AssertPixelsEqual(attack.GetPixel(20, 20), pc.GetPixel(374 + 20, 526 + 20), "primary attack crop must stay PC-derived");
            AssertPixelsEqual(empty.GetPixel(20, 20), pc.GetPixel(416 + 20, 526 + 20), "empty skill crop must stay PC-derived");
            AssertPixelsEqual(treasure.GetPixel(29, 29), pc.GetPixel(742 + 29, 502 + 29), "treasure crop must stay PC-derived");

            Object.DestroyImmediate(pc);
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(treasure);
        }

        [Test]
        public void HudPanelAndTextureImport_KeepResponsivePixelPerfectSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/HUD/HudPanelSettings.asset");
            Assert.NotNull(settings, "HudPanelSettings asset must exist.");
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, settings.scaleMode);
            Assert.AreEqual(new Vector2Int(1280, 720), settings.referenceResolution);
            Assert.AreEqual(PanelScreenMatchMode.Shrink, settings.screenMatchMode);

            AssertCriticalTextureImport("Assets/UI/HUD/Art/top_status_strip.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_primary_attack.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_skill_empty_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_treasure.png");
        }

        private static void AssertCriticalTextureImport(string assetPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Assert.NotNull(tex, assetPath + " must import as a Texture2D.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            Assert.NotNull(importer, assetPath + " importer must exist.");
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, assetPath + " must preserve NPOT size.");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, assetPath + " must not blur/compress HUD pixels.");
            Assert.AreEqual(FilterMode.Point, importer.filterMode, assetPath + " must remain pixel crisp.");
        }

        private static void AssertTextureSize(string file, int width, int height)
        {
            var path = Path.Combine(Application.dataPath, ArtRoot, file);
            var tex = LoadTexture(path);
            Assert.AreEqual(width, tex.width, file + " width");
            Assert.AreEqual(height, tex.height, file + " height");
            Object.DestroyImmediate(tex);
        }

        private static Texture2D LoadTexture(string path)
        {
            Assert.IsTrue(File.Exists(path), "Missing HUD art: " + path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "Invalid PNG: " + path);
            return tex;
        }

        private static void AssertPixelsEqual(Color a, Color b, string message)
        {
            const float epsilon = 1f / 255f;
            Assert.LessOrEqual(Mathf.Abs(a.r - b.r), epsilon, message + " (r)");
            Assert.LessOrEqual(Mathf.Abs(a.g - b.g), epsilon, message + " (g)");
            Assert.LessOrEqual(Mathf.Abs(a.b - b.b), epsilon, message + " (b)");
            Assert.LessOrEqual(Mathf.Abs(a.a - b.a), epsilon, message + " (a)");
        }
    }
}
