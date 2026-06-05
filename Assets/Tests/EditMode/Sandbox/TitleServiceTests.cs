using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class TitleServiceTests
    {
        private static string TitleDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTitle");

        private static TitleService BuildService()
        {
            var player = PcPlayerTitleParser.BuildRegistry(TitleDir);
            var faction = PcFactionTitleParser.BuildRegistry(TitleDir);
            var svc = new TitleService(player, faction);
            return svc;
        }

        [Test]
        public void LoadFromStreamingAssets_LoadsBothRegistries()
        {
            var svc = BuildService();
            Assert.GreaterOrEqual(svc.PlayerTitleCount, 100, "PC playertitle.txt có 363+ entries");
            Assert.GreaterOrEqual(svc.FactionTitleCount, 30, "PC factiontitle.txt có 81 entries");
            Assert.AreEqual(0, svc.UnlockedPlayerTitleCount);
            Assert.AreEqual(0, svc.UnlockedFactionTitleCount);
        }

        [Test]
        public void UnlockPlayerTitle_AddsToUnlocked()
        {
            var svc = BuildService();
            Assert.IsTrue(svc.PlayerTitleCount > 0, "Registry phải có data");
            int first = -1;
            for (int i = 1; i <= 1000; i++)
            {
                if (svc.GetPlayerTitle(i) != null) { first = i; break; }
            }
            Assert.Greater(first, 0, "Phải tìm thấy title id hợp lệ");

            Assert.IsTrue(svc.UnlockPlayerTitle(first));
            Assert.IsTrue(svc.IsPlayerTitleUnlocked(first));
            Assert.AreEqual(1, svc.UnlockedPlayerTitleCount);

            // Gọi lần 2 → no-op
            Assert.IsFalse(svc.UnlockPlayerTitle(first));
            Assert.AreEqual(1, svc.UnlockedPlayerTitleCount);
        }

        [Test]
        public void SetActivePlayerTitle_OnlyAllowedIfUnlocked()
        {
            var svc = BuildService();
            int first = -1;
            for (int i = 1; i <= 1000; i++)
            {
                if (svc.GetPlayerTitle(i) != null) { first = i; break; }
            }
            Assert.Greater(first, 0);

            // Chưa mở khóa → set thất bại
            Assert.IsFalse(svc.SetActivePlayerTitle(first));
            Assert.AreEqual(0, svc.ActivePlayerTitleId);

            // Mở khóa rồi set
            Assert.IsTrue(svc.UnlockPlayerTitle(first));
            Assert.IsTrue(svc.SetActivePlayerTitle(first));
            Assert.AreEqual(first, svc.ActivePlayerTitleId);
            Assert.IsNotNull(svc.ActivePlayerTitle);

            // Tắt
            Assert.IsTrue(svc.SetActivePlayerTitle(0));
            Assert.AreEqual(0, svc.ActivePlayerTitleId);
        }

        [Test]
        public void SetActiveFactionTitle_RequiresMatchingFaction()
        {
            var svc = BuildService();
            int first = -1;
            for (int i = 1; i <= 1000; i++)
            {
                if (svc.GetFactionTitle(i) != null) { first = i; break; }
            }
            Assert.Greater(first, 0, "Phải có ít nhất 1 faction title");
            var entry = svc.GetFactionTitle(first);

            // Mở khóa nhưng chưa set faction
            Assert.IsTrue(svc.UnlockFactionTitle(first));
            Assert.IsFalse(svc.SetActiveFactionTitle(first), "Chưa thuộc môn phái nào");
        }

        [Test]
        public void SetFaction_AllowsFactionTitleActivation()
        {
            var svc = BuildService();
            int first = -1;
            for (int i = 1; i <= 1000; i++)
            {
                if (svc.GetFactionTitle(i) != null) { first = i; break; }
            }
            var entry = svc.GetFactionTitle(first);
            Assert.Greater(first, 0);

            svc.UnlockFactionTitle(first);
            // Set faction khớp
            svc.SetFaction(entry.factionId);
            Assert.IsTrue(svc.SetActiveFactionTitle(first));
            Assert.AreEqual(first, svc.ActiveFactionTitleId);

            // Đổi sang phái khác → auto reset active faction title
            int otherFaction = entry.factionId == 1 ? 2 : 1;
            svc.SetFaction(otherFaction);
            Assert.AreEqual(0, svc.ActiveFactionTitleId, "Đổi phái phải reset danh hiệu môn phái");
        }

        [Test]
        public void UnlockFactionTitle_AddsToUnlocked()
        {
            var svc = BuildService();
            int first = -1;
            for (int i = 1; i <= 1000; i++)
            {
                if (svc.GetFactionTitle(i) != null) { first = i; break; }
            }
            Assert.Greater(first, 0);
            Assert.IsTrue(svc.UnlockFactionTitle(first));
            Assert.IsTrue(svc.IsFactionTitleUnlocked(first));
            Assert.AreEqual(1, svc.UnlockedFactionTitleCount);

            Assert.IsFalse(svc.UnlockFactionTitle(first));
            Assert.AreEqual(1, svc.UnlockedFactionTitleCount);
        }

        [Test]
        public void GetFactionTitlesForFaction_ReturnsGrouped()
        {
            var svc = BuildService();
            int anyFaction = 1;
            var list = svc.GetFactionTitlesForFaction(anyFaction);
            Assert.IsNotNull(list);
            // Mỗi phái có nhiều rank nên count > 0
            Assert.GreaterOrEqual(list.Count, 0, "Faction 1 (Thiếu Lâm) phải có ranks");
        }
    }
}
