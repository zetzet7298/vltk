using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.PlayMode
{
    public class GoldenSnapshotCapturePlayModeTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }

        [UnityTest]
        public IEnumerator Capture_RendersOnlySkillFxLayer_Deterministically_AndRejectsTransparentCapture()
        {
            _root = new GameObject("GoldenSnapshotCaptureTest");
            var visible = CreateSprite("VisibleSkillFx", 0);
            visible.transform.SetParent(_root.transform);
            yield return null;

            var first = GoldenSnapshotCaptureService.Capture("test_map", "visible", skillFxLayer: 0,
                skillId: 7, faction: "test_faction", frame: 1, tick: 2);
            var second = GoldenSnapshotCaptureService.Capture("test_map", "visible", skillFxLayer: 0,
                skillId: 7, faction: "test_faction", frame: 1, tick: 2);
            Assert.IsTrue(GoldenSnapshotComparer.TryValidate(first, out var error), error);
            Assert.IsTrue(first.signature.Any(bucket => bucket != 0), "SkillFx capture should contain rendered RGBA32 pixels.");
            CollectionAssert.AreEqual(first.signature, second.signature);

            visible.layer = 1;
            Assert.Throws<System.InvalidOperationException>(() => GoldenSnapshotCaptureService.Capture(
                "test_map", "empty", skillFxLayer: 0, skillId: 7, faction: "test_faction", frame: 1, tick: 2),
                "Transparent captures must fail closed.");
        }

        [UnityTest]
        public IEnumerator Capture_FocusesWorldEffect_AndRejectsExcludedLayerCapture()
        {
            _root = new GameObject("GoldenSnapshotOverlayLayerTest");
            var focus = new Vector2(96f, -64f);
            var overlayRoot = new GameObject("SkillVFX_Test");
            overlayRoot.transform.SetParent(_root.transform);
            var overlaySprite = CreateSprite("OverlayCreatedMissile", 0);
            overlaySprite.transform.SetParent(overlayRoot.transform);
            overlaySprite.transform.position = focus;
            SkillEffectWorldOverlay.StampLayerRecursively(overlayRoot, 8); // injected SkillFx layer for test

            var defaultSprite = CreateSprite("DefaultMustBeExcluded", 0);
            defaultSprite.transform.SetParent(_root.transform);
            defaultSprite.transform.position = focus;
            yield return null;

            var focused = GoldenSnapshotCaptureService.Capture("test_map", "focused", 8, focus,
                skillId: 7, faction: "test_faction", frame: 1, tick: 2);
            Assert.IsTrue(focused.signature.Any(bucket => bucket != 0), "focused non-default overlay visual must render");
            Assert.IsTrue(GoldenSnapshotComparer.TryValidate(focused, out var error), error);
            Assert.Throws<System.InvalidOperationException>(() => GoldenSnapshotCaptureService.Capture(
                "test_map", "origin", 8, Vector2.zero, skillId: 7, faction: "test_faction", frame: 1, tick: 2),
                "Fixed origin must fail when it captures no world-space visual.");

            SkillEffectWorldOverlay.StampLayerRecursively(overlayRoot, 0);
            Assert.Throws<System.InvalidOperationException>(() => GoldenSnapshotCaptureService.Capture(
                "test_map", "excluded", 8, focus, skillId: 7, faction: "test_faction", frame: 1, tick: 2),
                "Default-layer visuals must be excluded and transparent capture rejected.");
        }

        private static GameObject CreateSprite(string name, int layer)
        {
            var go = new GameObject(name) { layer = layer };
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            renderer.color = Color.white;
            go.transform.localScale = Vector3.one * 4f;
            return go;
        }
    }
}
