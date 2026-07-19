using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillTimePerCastOnHorseTests
    {
        private static readonly IReadOnlyDictionary<int, int> CanonicalNonZeroHorseTicks =
            new Dictionary<int, int>
            {
                [19] = 5,
                [20] = 54,
                [40] = 27,
                [138] = 40,
                [164] = 25,
                [181] = 54,
                [392] = 27,
            };

        private static string CanonicalSlicePath => Path.Combine(
            Application.streamingAssetsPath,
            "Reference",
            "PcAllFactionLearnedDisplaySkills.txt");

        [Test]
        public void CanonicalSlice_Has114ColumnsAndExpectedHorseTimingOracle()
        {
            string[] lines = File.ReadAllLines(CanonicalSlicePath);
            string[] header = lines[0].Split('\t');

            Assert.AreEqual(114, header.Length);
            Assert.AreEqual(27, Array.IndexOf(header, "WaitTime"));
            Assert.AreEqual(28, Array.IndexOf(header, "IsSaveCd"));
            Assert.AreEqual(29, Array.IndexOf(header, "ClientSend"));
            Assert.AreEqual(30, Array.IndexOf(header, "SkillCostType"));
            Assert.AreEqual(31, Array.IndexOf(header, "CostValue"));
            Assert.AreEqual(32, Array.IndexOf(header, "TimePerCast"));
            Assert.AreEqual(33, Array.IndexOf(header, "TimePerCastOnHorse"));
            Assert.AreEqual(34, Array.IndexOf(header, "IsPhysical"));

            var rows = PcConfigParser.ParseSkillsLines(lines);
            Assert.AreEqual(242, rows.Count);
            var byId = rows.ToDictionary(row => row.skillId);
            Assert.IsTrue(byId.ContainsKey(720), "Blank trailing SkillDesc must not drop a canonical row");

            foreach (var expected in CanonicalNonZeroHorseTicks)
                Assert.AreEqual(expected.Value, byId[expected.Key].timePerCastOnHorse, $"SkillId {expected.Key}");

            Assert.AreEqual(235, rows.Count(row => row.timePerCastOnHorse == 0));
            Assert.AreEqual(7, rows.Count(row => row.timePerCastOnHorse != 0));
        }

        [Test]
        public void CanonicalSlice_HeaderMappedFieldsMatchEveryRawRowWithoutShift()
        {
            string[] lines = File.ReadAllLines(CanonicalSlicePath);
            string[] header = lines[0].Split('\t');
            var column = header.Select((name, index) => (name, index))
                .ToDictionary(item => item.name, item => item.index, StringComparer.Ordinal);
            var rawById = lines.Skip(1)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Split('\t'))
                .ToDictionary(cells => Int(cells[column["SkillId"]]));

            var parsed = PcConfigParser.ParseSkillsLines(lines);
            Assert.AreEqual(rawById.Count, parsed.Count);

            foreach (var skill in parsed)
            {
                string[] raw = rawById[skill.skillId];
                Assert.AreEqual(Int(raw[column["SkillCostType"]]), skill.skillCostType, $"SkillCostType {skill.skillId}");
                Assert.AreEqual(Int(raw[column["CostValue"]]), skill.cost, $"CostValue {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TimePerCast"]]), skill.timePerCast, $"TimePerCast {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TimePerCastOnHorse"]]), skill.timePerCastOnHorse, $"Horse {skill.skillId}");
                Assert.AreEqual(Int(raw[column["IsPhysical"]]) != 0, skill.isPhysical, $"IsPhysical {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TargetOnly"]]) != 0, skill.targetOnly, $"TargetOnly {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TargetEnemy"]]) != 0, skill.targetEnemy, $"TargetEnemy {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TargetAlly"]]) != 0, skill.targetAlly, $"TargetAlly {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TargetSelf"]]) != 0, skill.targetSelf, $"TargetSelf {skill.skillId}");
                Assert.AreEqual(Int(raw[column["TargetObj"]]) != 0, skill.targetObj, $"TargetObj {skill.skillId}");
                Assert.AreEqual(Int(raw[column["ByMissle"]]) != 0, skill.byMissile, $"ByMissle {skill.skillId}");
                Assert.AreEqual(Int(raw[column["IsUseAR"]]) != 0, skill.isUseAttackRating, $"IsUseAR {skill.skillId}");
                Assert.AreEqual(Int(raw[column["ReqLevel"]]), skill.reqLevel, $"ReqLevel {skill.skillId}");
                Assert.AreEqual(Int(raw[column["MaxLevel"]]), skill.maxLevel, $"MaxLevel {skill.skillId}");
                Assert.AreEqual(Int(raw[column["EqtLimit"]]), skill.equipLimit, $"EqtLimit {skill.skillId}");
                Assert.AreEqual(Int(raw[column["HorseLimit"]]), skill.horseLimit, $"HorseLimit {skill.skillId}");
                Assert.AreEqual(Int(raw[column["DoHurt"]]) != 0, skill.doHurt, $"DoHurt {skill.skillId}");
                Assert.AreEqual(Int(raw[column["WeaponSkill"]]) != 0, skill.weaponSkill, $"WeaponSkill {skill.skillId}");
            }
        }

        [Test]
        public void Legacy113ColumnPcSkills_RemainsHeaderCompatible()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Reference", "PcSkills.txt");
            string[] lines = File.ReadAllLines(path);
            string[] header = lines[0].Split('\t');

            Assert.AreEqual(113, header.Length);
            Assert.AreEqual(-1, Array.IndexOf(header, "IsSaveCd"));
            Assert.AreEqual(1216, PcConfigParser.ParseSkillsLines(lines).Count);
        }

        [Test]
        public void MissingRequiredHorseHeader_FailsClosed()
        {
            string[] lines = File.ReadAllLines(CanonicalSlicePath);
            string header = lines[0].Replace("TimePerCastOnHorse", "MissingHorseTiming");
            var rows = PcConfigParser.ParseSkillsLines(new[] { header, lines[1] });
            Assert.AreEqual(0, rows.Count);
        }

        private static int Int(string raw) =>
            int.TryParse(raw?.Trim(), out int value) ? value : 0;
    }
}
