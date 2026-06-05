// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Attrib/Missle/Event/CityWar service tests
// Vietnamese: Kiểm thử các service thuộc tính, đạn phép, sự kiện, thành chiến.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AttribConstServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_LoadsSections()
        {
            var svc = AttribConstService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 1, "AttribConst phải có ít nhất 1 section từ PcAttrib/*.ini");
        }

        [Test]
        public void GetSection_ReturnsEntries()
        {
            var svc = AttribConstService.LoadFromStreamingAssets();
            var firstSection = string.Empty;
            foreach (var s in svc.GetAllSections()) { firstSection = s; break; }
            Assert.IsFalse(string.IsNullOrEmpty(firstSection), "Phải có ít nhất 1 section");
            var entries = svc.GetSection(firstSection);
            Assert.IsNotNull(entries);
            Assert.GreaterOrEqual(entries.Count, 1, $"Section {firstSection} phải có ít nhất 1 entry");
            foreach (var e in entries)
            {
                Assert.AreEqual(firstSection, e.section);
                Assert.IsFalse(string.IsNullOrEmpty(e.key));
            }
        }

        [Test]
        public void GetAllSections_NonEmpty()
        {
            var svc = AttribConstService.LoadFromStreamingAssets();
            int n = 0;
            foreach (var _ in svc.GetAllSections()) n++;
            Assert.GreaterOrEqual(n, 1);
        }

        [Test]
        public void ResolveMagicCode_ReturnsIntOrNegativeOne()
        {
            var svc = AttribConstService.LoadFromStreamingAssets();
            string firstSection = string.Empty;
            foreach (var s in svc.GetAllSections()) { firstSection = s; break; }
            int code = svc.ResolveMagicCode(firstSection, "NonexistentKey");
            Assert.AreEqual(-1, code);
        }

        [Test]
        public void GetValue_AndGetInt_ReturnExpectedTypes()
        {
            var svc = AttribConstService.LoadFromStreamingAssets();
            var firstSection = string.Empty;
            foreach (var s in svc.GetAllSections()) { firstSection = s; break; }
            var entries = svc.GetSection(firstSection);
            if (entries.Count > 0)
            {
                var first = entries[0];
                var v = svc.GetValue(firstSection, first.key);
                Assert.IsFalse(string.IsNullOrEmpty(v));
            }
        }
    }

    public class MissleCatalogServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_LoadsMissles()
        {
            var svc = MissleCatalogService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 50, "Missle catalog phải có >= 50 entries từ missles.txt");
        }

        [Test]
        public void GetByMoveKind_FiltersCorrectly()
        {
            var svc = MissleCatalogService.LoadFromStreamingAssets();
            int firstKind = -1;
            foreach (var e in svc.GetAllMissles()) { firstKind = e.moveKind; break; }
            Assert.GreaterOrEqual(firstKind, 0);
            var list = svc.GetByMoveKind(firstKind);
            Assert.GreaterOrEqual(list.Count, 1);
            foreach (var e in list) Assert.AreEqual(firstKind, e.moveKind);
        }

        [Test]
        public void Count_Positive()
        {
            var svc = MissleCatalogService.LoadFromStreamingAssets();
            Assert.Greater(svc.Count, 0);
        }

        [Test]
        public void GetByFollowKind_FiltersCorrectly()
        {
            var svc = MissleCatalogService.LoadFromStreamingAssets();
            var firstEntry = default(PcMissleEntry);
            foreach (var e in svc.GetAllMissles()) { firstEntry = e; break; }
            Assert.IsNotNull(firstEntry);
            int kind = firstEntry.followKind;
            var list = svc.GetByFollowKind(kind);
            foreach (var e in list) Assert.AreEqual(kind, e.followKind);
        }
    }

    public class EventBonusServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_LoadsEvents()
        {
            var svc = EventBonusService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 1, "Event bonus phải có ít nhất 1 entry từ PcEvent/*");
        }

        [Test]
        public void GetEventBonuses_ReturnsForEvent()
        {
            var svc = EventBonusService.LoadFromStreamingAssets();
            var firstEvent = string.Empty;
            foreach (var e in svc.GetAllEvents()) { firstEvent = e; break; }
            if (string.IsNullOrEmpty(firstEvent))
            {
                Assert.Inconclusive("Không có event nào để test");
                return;
            }
            var list = svc.GetEventBonuses(firstEvent);
            Assert.GreaterOrEqual(list.Count, 1);
            foreach (var e in list) Assert.AreEqual(firstEvent, e.eventName);
        }

        [Test]
        public void MarkClaimed_PreventsDoubleClaim()
        {
            var svc = EventBonusService.LoadFromStreamingAssets();
            string firstEvent = string.Empty;
            foreach (var e in svc.GetAllEvents()) { firstEvent = e; break; }
            if (string.IsNullOrEmpty(firstEvent))
            {
                Assert.Inconclusive("Không có event nào");
                return;
            }
            var list = svc.GetEventBonuses(firstEvent);
            if (list.Count == 0) { Assert.Inconclusive("Event rỗng"); return; }
            var first = list[0];
            bool claimed1 = svc.MarkClaimed(first.eventName, first.fileName, first.lineIndex);
            bool claimed2 = svc.MarkClaimed(first.eventName, first.fileName, first.lineIndex);
            Assert.IsTrue(claimed1);
            Assert.IsFalse(claimed2);
            Assert.IsTrue(svc.IsClaimed(first.eventName, first.fileName, first.lineIndex));
        }

        [Test]
        public void GetEntriesForFile_FiltersByFileName()
        {
            var svc = EventBonusService.LoadFromStreamingAssets();
            string firstEvent = string.Empty;
            foreach (var e in svc.GetAllEvents()) { firstEvent = e; break; }
            if (string.IsNullOrEmpty(firstEvent)) { Assert.Inconclusive("no events"); return; }
            var all = svc.GetEventBonuses(firstEvent);
            if (all.Count == 0) { Assert.Inconclusive("empty event"); return; }
            string fileName = all[0].fileName;
            var filtered = svc.GetEntriesForFile(firstEvent, fileName);
            Assert.GreaterOrEqual(filtered.Count, 1);
            foreach (var e in filtered) Assert.AreEqual(fileName, e.fileName);
        }
    }

    public class CityWarServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_LoadsCities()
        {
            var svc = CityWarService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 1, "CityWar phải có ít nhất 1 khu vực từ citywar.ini");
        }

        [Test]
        public void CaptureCity_ChangesOwner()
        {
            var svc = CityWarService.LoadFromStreamingAssets();
            int cityId = -1;
            foreach (var s in svc.GetAllCityStates()) { cityId = s.cityId; break; }
            if (cityId <= 0) { Assert.Inconclusive("không có thành"); return; }
            int oldOwner = svc.GetCityState(cityId).ownerFaction;
            int newOwner = oldOwner == 0 ? 1 : 2;
            bool captured = svc.CaptureCity(cityId, newOwner);
            Assert.IsTrue(captured);
            Assert.AreEqual(newOwner, svc.GetCityState(cityId).ownerFaction);
            Assert.IsTrue(svc.IsOwnedBy(cityId, newOwner));
            // Capture lại bằng cùng owner trả về false
            Assert.IsFalse(svc.CaptureCity(cityId, newOwner));
        }

        [Test]
        public void GetCitiesOwnedBy_FiltersCorrectly()
        {
            var svc = CityWarService.LoadFromStreamingAssets();
            int cityId = -1;
            foreach (var s in svc.GetAllCityStates()) { cityId = s.cityId; break; }
            if (cityId <= 0) { Assert.Inconclusive("không có thành"); return; }
            svc.CaptureCity(cityId, 7); // Cái Bang
            int count = 0;
            foreach (var s in svc.GetCitiesOwnedBy(7)) count++;
            Assert.GreaterOrEqual(count, 1);
            foreach (var s in svc.GetCitiesOwnedBy(7)) Assert.AreEqual(7, s.ownerFaction);
        }

        [Test]
        public void ResetAll_RevertsToNeutral()
        {
            var svc = CityWarService.LoadFromStreamingAssets();
            int cityId = -1;
            foreach (var s in svc.GetAllCityStates()) { cityId = s.cityId; break; }
            if (cityId <= 0) { Assert.Inconclusive("không có thành"); return; }
            svc.CaptureCity(cityId, 1);
            svc.ResetAll();
            Assert.AreEqual(0, svc.GetCityState(cityId).ownerFaction);
        }

        [Test]
        public void OnCityCaptured_FiresEvent()
        {
            var svc = CityWarService.LoadFromStreamingAssets();
            int cityId = -1;
            foreach (var s in svc.GetAllCityStates()) { cityId = s.cityId; break; }
            if (cityId <= 0) { Assert.Inconclusive("không có thành"); return; }
            int fired = 0;
            svc.OnCityCaptured += (id, oldOwner, newOwner) => { fired++; };
            svc.CaptureCity(cityId, 3);
            Assert.AreEqual(1, fired);
        }
    }
}
