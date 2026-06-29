using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("HUD")]
    public class MobileHudLayoutTests
    {
        private static string AssetPath(string relative) => Path.Combine(Application.dataPath, relative);

        [Test]
        public void S3_QuickSlotsAndTopGapMenu_ArePresentInHudUxml()
        {
            var uxml = File.ReadAllText(AssetPath("UI/HUD/GameHud.uxml"));

            Assert.That(uxml, Does.Contain("name=\"QuickSlots\""));
            Assert.That(uxml, Does.Contain("name=\"QuickSlot1\""));
            Assert.That(uxml, Does.Contain("name=\"QuickSlot2\""));
            Assert.That(uxml, Does.Contain("name=\"QuickSlot3\""));

            Assert.That(uxml, Does.Contain("name=\"TopGapCluster\""));
            Assert.That(uxml, Does.Contain("name=\"TopGapMenuRow\""));
            foreach (var buttonName in new[]
            {
                "BtnStatus", "BtnItems", "BtnItemEx", "BtnSkills", "BtnQuest",
                "BtnTeam", "BtnFaction", "BtnChatRoom", "BtnTreasure"
            })
            {
                Assert.That(uxml, Does.Contain($"name=\"{buttonName}\""), $"Missing relocated top-gap button {buttonName}");
            }

            Assert.That(uxml, Does.Not.Contain("name=\"BottomPanel\""), "PC-replica bottom strip should stay removed");
        }

        [Test]
        public void S3_QuickSlotsUsePcChromeAndNoNthChildSelector()
        {
            var uss = File.ReadAllText(AssetPath("UI/HUD/GameHud.uss"));

            Assert.That(uss, Does.Contain(".hud-quick-slot-1"));
            Assert.That(uss, Does.Contain("btn_quick_item_1_pc.png"));
            Assert.That(uss, Does.Contain("btn_quick_item_2_pc.png"));
            Assert.That(uss, Does.Contain("btn_quick_item_3_pc.png"));
            Assert.That(uss, Does.Not.Contain("nth-child"), "Unity USS support for nth-child is unsafe here; use explicit classes");
        }

        [Test]
        public void S3_ControllerBindsQuickSlotsAndRelocatedMenuIcons()
        {
            var controller = File.ReadAllText(AssetPath("Scripts/UI/GameHudController.cs"));

            Assert.That(controller, Does.Contain("RegisterClick(root, \"QuickSlot1\""));
            Assert.That(controller, Does.Contain("RegisterClick(root, \"QuickSlot2\""));
            Assert.That(controller, Does.Contain("RegisterClick(root, \"QuickSlot3\""));
            Assert.That(controller, Does.Contain("OnQuickSlotClick"));
            Assert.That(controller, Does.Contain("{ \"BtnTreasure\", \"btn_treasure\" }"));
        }

        [Test]
        public void S3_ActionTogglesHaveActiveRings()
        {
            var uxml = File.ReadAllText(AssetPath("UI/HUD/GameHud.uxml"));
            var uss = File.ReadAllText(AssetPath("UI/HUD/GameHud.uss"));
            var controller = File.ReadAllText(AssetPath("Scripts/UI/GameHudController.cs"));

            foreach (var ringName in new[] { "ActionBtnRunRing", "ActionBtnHorseRing", "ActionBtnSitRing" })
                Assert.That(uxml, Does.Contain($"name=\"{ringName}\""), $"Missing active-state ring {ringName}");

            Assert.That(uss, Does.Contain(".hud-action-toggle-ring"));
            Assert.That(uss, Does.Contain(".hud-action-btn.toggle-on > .hud-action-toggle-ring"));
            Assert.That(controller, Does.Contain("EnableInClassList(\"toggle-on\""));
            Assert.That(controller, Does.Contain("SetActionToggleRing(_actionBtnRun, runOn)"));
            Assert.That(controller, Does.Contain("SetActionToggleRing(_actionBtnSit, sitOn)"));
            Assert.That(controller, Does.Contain("SetActionToggleRing(_actionBtnHorse, horseOn)"));
        }
    }
}
