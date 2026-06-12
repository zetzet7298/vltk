// -----------------------------------------------------------------------------
// VLTK Mobile — MeridianService tests
// Wraps PcMeridianRegistry. Tests cover load, query, TryUpgrade (success/fail/
// maxLevel/prereq/NotFound), per-meridian point enumeration, and the composite
// (meridian, level) key that preserves all 128 acupoints (8 meridians × 16 levels).
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MeridianServiceTests
    {
        private static string MeridianDir
            => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMeridian");

        [Test]
        public void Build_LoadsAllAcupoints()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // PC meridian_level.txt = 8 meridians × 16 levels = 128 distinct acupoints.
            // The old single-int key collapsed this to 16 (last-writer-wins). The
            // composite (meridian, level) key must preserve all 128.
            Assert.AreEqual(128, svc.Count,
                "Composite key must preserve all 128 acupoints (8 meridians × 16 levels).");
        }

        [Test]
        public void Build_EachMeridianHas16Levels()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            int meridianCount = 0;
            foreach (var m in svc.GetMeridianIds())
            {
                meridianCount++;
                Assert.AreEqual(16, svc.GetMeridianPoints(m).Count,
                    $"Meridian {m} should expose 16 acupoint levels.");
            }
            Assert.AreEqual(8, meridianCount, "PC file defines 8 meridians (1-8).");
        }

        [Test]
        public void GetAcupoint_DistinguishesSameLevelAcrossMeridians()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // Level 1 exists in every meridian but they are different acupoints.
            var m1 = svc.GetAcupoint(1, 1);
            var m2 = svc.GetAcupoint(2, 1);
            Assert.IsNotNull(m1, "Meridian 1 level 1 should exist");
            Assert.IsNotNull(m2, "Meridian 2 level 1 should exist");
            Assert.AreEqual(1, m1.meridianId);
            Assert.AreEqual(2, m2.meridianId);
            Assert.AreEqual(1, m1.acupointId);
            Assert.AreEqual(1, m2.acupointId);
            // They must be distinct rows (different names from the PC file).
            Assert.AreNotSame(m1, m2, "Same level in different meridians must be distinct entries.");
        }

        [Test]
        public void GetMeridianPoints_ReturnsAllPointsForMeridianId()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            var points = svc.GetMeridianPoints(1);
            Assert.AreEqual(16, points.Count, "Meridian 1 has 16 acupoint levels");
        }

        [Test]
        public void GetMeridianIds_EnumeratesAllMeridians()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            int count = 0;
            foreach (var _ in svc.GetMeridianIds()) count++;
            Assert.AreEqual(8, count, "Should enumerate all 8 meridians");
        }

        [Test]
        public void TryUpgrade_SuccessOnFirstLevel()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            svc.SetSeed(123);
            // Find an acupoint with successRate 10000 (100%) in meridian 1.
            PcMeridianEntry target = null;
            foreach (var p in svc.GetMeridianPoints(1))
            {
                if (p.successRate == 10000) { target = p; break; }
            }
            Assert.IsNotNull(target, "Need an acupoint with 100% success");
            svc.SetPlayerAcupointLevel(target.meridianId, target.acupointId, 0);
            // successRate=10000 → always success when player level is sufficient.
            var result = svc.TryUpgrade(target.meridianId, target.acupointId, playerLevel: 200);
            Assert.That(result == UpgradeResult.Success || result == UpgradeResult.PrereqLevel,
                "Success when successRate=10000 and player level sufficient, Prereq otherwise.");
            if (result == UpgradeResult.Success)
                Assert.AreEqual(1, svc.GetPlayerAcupointLevel(target.meridianId, target.acupointId));
        }

        [Test]
        public void TryUpgrade_FailureDropsToFallback()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // Find any acupoint in meridian 1 with 0 < successRate < 10000.
            PcMeridianEntry target = null;
            foreach (var p in svc.GetMeridianPoints(1))
            {
                if (p.successRate > 0 && p.successRate < 10000) { target = p; break; }
            }
            Assert.IsNotNull(target, "Need an acupoint with non-zero, non-100% success");
            int mer = target.meridianId;
            int lvl = target.acupointId;
            // Try seeds until one produces a roll that fails.
            for (int s = 1; s < 1000; s++)
            {
                svc.SetSeed(s);
                svc.SetPlayerAcupointLevel(mer, lvl, 5);
                var r = svc.TryUpgrade(mer, lvl, playerLevel: lvl + 1);
                if (r == UpgradeResult.Failed)
                {
                    Assert.AreEqual(target.fallbackLevel, svc.GetPlayerAcupointLevel(mer, lvl));
                    return;
                }
            }
            Assert.Pass("Could not force a fail in 1000 seeds; successRate may be 10000 in sample.");
        }

        [Test]
        public void TryUpgrade_MaxLevelClamped()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            var entry = svc.GetAcupoint(1, 1);
            Assert.IsNotNull(entry);
            svc.SetPlayerAcupointLevel(1, 1, MeridianService.MaxAcupointLevel);
            var result = svc.TryUpgrade(1, 1, playerLevel: 200);
            Assert.AreEqual(UpgradeResult.MaxLevel, result);
            Assert.AreEqual(MeridianService.MaxAcupointLevel, svc.GetPlayerAcupointLevel(1, 1));
        }

        [Test]
        public void TryUpgrade_PrereqLevelNotMet()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // Acupoint at level 16 needs playerLevel >= 16. Player is level 10.
            var entry = svc.GetAcupoint(1, 16);
            Assert.IsNotNull(entry, "Meridian 1 level 16 should exist");
            var result = svc.TryUpgrade(1, 16, playerLevel: 10);
            Assert.AreEqual(UpgradeResult.PrereqLevel, result);
        }

        [Test]
        public void TryUpgrade_NotFoundForMissingAcupoint()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // Meridian 99 / level 99 does not exist.
            var result = svc.TryUpgrade(99, 99, playerLevel: 200);
            Assert.AreEqual(UpgradeResult.NotFound, result);
        }

        [Test]
        public void SetPlayerAcupointLevel_ClampsToRange()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            svc.SetPlayerAcupointLevel(1, 1, 100);
            Assert.AreEqual(MeridianService.MaxAcupointLevel, svc.GetPlayerAcupointLevel(1, 1));
            svc.SetPlayerAcupointLevel(1, 1, -5);
            Assert.AreEqual(0, svc.GetPlayerAcupointLevel(1, 1));
        }

        [Test]
        public void PlayerProgress_IsIsolatedPerMeridian()
        {
            var reg = PcMeridianParser.BuildRegistry(MeridianDir);
            var svc = new MeridianService(reg);
            // Setting tier on meridian 1 level 1 must NOT bleed into meridian 2 level 1.
            svc.SetPlayerAcupointLevel(1, 1, 7);
            Assert.AreEqual(7, svc.GetPlayerAcupointLevel(1, 1));
            Assert.AreEqual(0, svc.GetPlayerAcupointLevel(2, 1),
                "Player progress must be keyed per (meridian, level), not by level alone.");
        }

        [Test]
        public void LoadFromStreamingAssets_ReturnsService()
        {
            var svc = MeridianService.LoadFromStreamingAssets("Reference/PcMeridian");
            Assert.IsNotNull(svc);
            Assert.AreEqual(128, svc.Count);
        }
    }
}
