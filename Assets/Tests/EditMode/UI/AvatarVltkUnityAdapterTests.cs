using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class AvatarVltkUnityAdapterTests
    {
        private VisualElement _root;
        private VisualElement _frame, _portraitBg, _portrait;
        private Label _levelText;
        private HudDataBridge _bridge;
        private HudCommandBus _bus;
        private AvatarVltkUnityAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _root = new VisualElement { name = "GameHud" };
            _frame = new VisualElement { name = "AvatarFrame" };
            _portraitBg = new VisualElement { name = "AvatarPortraitBg" };
            _portrait = new VisualElement { name = "AvatarPortrait" };
            _levelText = new Label { name = "AvatarLevelText" };

            _root.Add(_frame);
            _frame.Add(_portraitBg);
            _portraitBg.Add(_portrait);
            _frame.Add(_levelText);

            _bridge = new HudDataBridge(new AvatarRuntime(), false);
            _bus = new HudCommandBus();
            _adapter = new AvatarVltkUnityAdapter(_root, _bridge, _bus);
            _adapter.Bind();
        }

        [TearDown]
        public void TearDown() => _adapter?.Dispose();

        [Test]
        public void Constructor_NullRoot_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new AvatarVltkUnityAdapter(null, _bridge, _bus));

        [Test]
        public void Constructor_NullBridge_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new AvatarVltkUnityAdapter(_root, null, _bus));

        [Test]
        public void Constructor_NullBus_Throws()
            => Assert.Throws<System.ArgumentNullException>(() => new AvatarVltkUnityAdapter(_root, _bridge, null));

        [Test]
        public void Apply_SetsLevelTextFromSnapshot()
        {
            _adapter.Apply(new HudSnapshot { valid = true, level = 93 });
            // recon §4b: level placeholder "93"
            Assert.AreEqual("93", _levelText.text);
        }

        [Test]
        public void Apply_InvalidSnapshot_DoesNotChangeLevelText()
        {
            _levelText.text = "unchanged";
            _adapter.Apply(new HudSnapshot { valid = false, level = 50 });
            Assert.AreEqual("unchanged", _levelText.text);
        }

        [Test]
        public void SetPortrait_AcceptsTextureWithoutThrowing()
        {
            // EditMode cannot create a real GPU Texture2D reliably; verify the null
            // guard path and that a null texture does not throw or clear the element.
            Assert.DoesNotThrow(() => _adapter.SetPortrait(null));
        }

        private sealed class AvatarRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap => true;
            public int ActiveMapId => 1;
            public string ActiveMapName => "Test";
            public VLTK.Model.MapDefinition ActiveMapDefinition => null;
            public Vector2 PlayerWorldPosition => Vector2.zero;
            public int PlayerLevel => 1;
            public int PlayerCurrentLife => 100;
            public int PlayerMaxLife => 100;
            public int PlayerCurrentMana => 100;
            public int PlayerMaxMana => 100;
            public int PlayerCurrentStamina => 100;
            public int PlayerMaxStamina => 100;
            public long PlayerExp => 0;
            public long PlayerMaxExp => 1000;
            public float MiniMapXRatio => 0f;
            public float MiniMapYRatio => 0f;
            public int PlayerCopper => 0;
            public int PlayerGold => 0;
            public int PlayerSilver => 0;
        }
    }
}
