using NUnit.Framework;
using UnityEngine;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class HudDataParityTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            // Ensure HudDataService loads
            HudDataService.Instance.LoadData();
        }

        [Test]
        public void HudDataService_IsSuccessfullyLoaded()
        {
            Assert.IsTrue(HudDataService.Instance.IsLoaded, "HudDataService failed to load JSON data.");
        }

        [Test]
        public void BuffList_LoadsCorrectly_AndContainsExpectedBuffs()
        {
            // Test some basic buffs from Skills.txt / SkillState.ini
            var buff15 = HudDataService.Instance.GetBuff(15);
            Assert.IsNotNull(buff15, "Buff 15 (Bất động Minh Vương) should exist.");
            Assert.AreEqual("Bất động Minh Vương", buff15.name);

            var buff42 = HudDataService.Instance.GetBuff(42);
            Assert.IsNotNull(buff42, "Buff 42 (Kim Chung Tráo) should exist.");
            Assert.AreEqual("Kim Chung Tráo", buff42.name);

            var buff157 = HudDataService.Instance.GetBuff(157);
            Assert.IsNotNull(buff157, "Buff 157 (Tọa Vọng Vô Ngã) should exist.");
            Assert.AreEqual("Tọa Vọng Vô Ngã", buff157.name);
        }

        [Test]
        public void EmoteList_LoadsCorrectly_AndContainsExpectedEmotes()
        {
            var emotes = HudDataService.Instance.GetEmoteList();
            Assert.IsNotNull(emotes);
            Assert.Greater(emotes.Count, 0, "Emote list should not be empty.");

            // Test first emote :)
            var first = emotes[0];
            Assert.AreEqual(1, first.id);
            Assert.AreEqual(":)", first.text);
            Assert.AreEqual("Mỉm cười", first.tip);

            // Test second emote :D
            var second = emotes[1];
            Assert.AreEqual(2, second.id);
            Assert.AreEqual(":D", second.text);
            Assert.AreEqual("cười lớn", second.tip);
        }

        [Test]
        public void RankingTitles_LoadsCorrectly_AndContainsExpectedTitles()
        {
            var titleWorld = HudDataService.Instance.GetRankingTitle(10287);
            Assert.IsNotNull(titleWorld, "Ranking title ID 10287 should exist.");
            Assert.AreEqual("Thập đại cao thủ thế giới", titleWorld.name);

            var titleCaiBang = HudDataService.Instance.GetRankingTitle(10283);
            Assert.IsNotNull(titleCaiBang, "Ranking title ID 10283 should exist.");
            Assert.AreEqual("Cái Bang", titleCaiBang.name);
        }

        [Test]
        public void Factions_LoadsCorrectly_AndContainsExpectedAbbrev()
        {
            var factionCb = HudDataService.Instance.GetFaction("cb");
            Assert.IsNotNull(factionCb, "Faction 'cb' should exist.");
            Assert.AreEqual("Cái Bang", factionCb.nameVi);
            Assert.AreEqual(124, factionCb.placeholderSkillId);

            var factionTm = HudDataService.Instance.GetFaction("tm");
            Assert.IsNotNull(factionTm, "Faction 'tm' should exist.");
            Assert.AreEqual("Đường Môn", factionTm.nameVi);
            Assert.AreEqual(48, factionTm.placeholderSkillId);

            var factionEm = HudDataService.Instance.GetFaction("em");
            Assert.IsNotNull(factionEm, "Faction 'em' should exist.");
            Assert.AreEqual("Nga My", factionEm.nameVi);
            Assert.AreEqual(93, factionEm.placeholderSkillId);
        }

        [Test]
        public void MapColors_LoadsCorrectly_AndParsesExpectedColors()
        {
            // SelfPlayerColor=0,255,0
            Color selfPlayer = HudDataService.Instance.GetMapColor("SelfPlayerColor", Color.white);
            Assert.AreEqual(0f, selfPlayer.r);
            Assert.AreEqual(1f, selfPlayer.g);
            Assert.AreEqual(0f, selfPlayer.b);

            // TeammateColor=0,255,0
            Color teammate = HudDataService.Instance.GetMapColor("TeammateColor", Color.white);
            Assert.AreEqual(0f, teammate.r);
            Assert.AreEqual(1f, teammate.g);
            Assert.AreEqual(0f, teammate.b);
        }

        [Test]
        public void InfoStrings_LoadsCorrectly_AndContainsExpectedMessages()
        {
            string msg1 = HudDataService.Instance.GetInfoString(1);
            Assert.AreEqual("Hiện đang kết nối với máy chủ", msg1);

            string msg2 = HudDataService.Instance.GetInfoString(2);
            Assert.AreEqual("Kết nối máy chủ thất bại. Xin kiểm tra lại đường truyền.", msg2);
        }
    }
}
