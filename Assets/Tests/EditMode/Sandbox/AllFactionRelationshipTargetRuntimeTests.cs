using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public sealed class AllFactionRelationshipTargetRuntimeTests
    {
        private sealed class Slice
        {
            public string name;
            public string resource;
            public string sourceTxt;
            public string sourceProvenance;
            public string txtSha;
            public string provenanceSha;
            public int[] targets;
            public int[] fieldTargets;
        }

        private sealed class Link
        {
            public string slice;
            public int parent;
            public string column;
            public int target;
        }

        private static readonly Slice[] Slices =
        {
            new Slice { name = "EMei", resource = "PcEMeiRelationshipTargets", sourceTxt = "SKL-EM-PROOF-001/PcEMeiRelationshipTargets.txt", sourceProvenance = "SKL-EM-PROOF-001/PcEMeiRelationshipTargets.provenance.json", txtSha = "86d535432340b1d9223a0e9f7c6ccc3de3c8ebe60556932136b8181ce2d9ee8e", provenanceSha = "37e9d89a99689b6c5cf137cae361ff34d890432fb1a64441c64a83059010bdfe", targets = new[] { 243, 329, 331, 1089, 1115 }, fieldTargets = new[] { 243, 329, 331, 1089, 1115 } },
            new Slice { name = "CuiYan", resource = "PcCuiYanRelationshipTargets", sourceTxt = "SKL-CY-PROOF-001/PcCuiYanRelationshipTargets.txt", sourceProvenance = "SKL-CY-PROOF-001/PcCuiYanRelationshipTargets.provenance.json", txtSha = "71811809f8eaf4e23948b6cc06cbaa37386556ce7a1e1694b4ba16c71fbd96dd", provenanceSha = "55483818ec1a367821bafc693ca86edfed6d51f61e447823c12853d54c3d7f5c", targets = new[] { 398, 112, 338, 1064, 1093, 1102 }, fieldTargets = new[] { 398, 112, 338, 1064, 1093, 1102 } },
            new Slice { name = "TianRen", resource = "PcTianRenRelationshipTargets", sourceTxt = "SKL-TR-PROOF-001/PcTianRenRelationshipTargets.txt", sourceProvenance = "SKL-TR-PROOF-001/PcTianRenRelationshipTargets.provenance.json", txtSha = "e16596686a733590ba0364a31b0acaa65ff4353fde343c47ac0971dbee2724f6", provenanceSha = "054d19b1c40a19c2bb2723c41b44d01a25d1e3c6417149c6805eb438a84cc044", targets = new[] { 192, 363, 723, 1131 }, fieldTargets = new[] { 723, 1131 } },
            new Slice { name = "WuDu", resource = "PcWuDuRelationshipTargets", sourceTxt = "SKL-WDU-PROOF-001/PcWuDuRelationshipTargets.txt", sourceProvenance = "SKL-WDU-PROOF-001/PcWuDuRelationshipTargets.provenance.json", txtSha = "24897cafb9872e02c0e38fc6865c04690a00ea5e7438d1d9fc48ce84bbd12c75", provenanceSha = "a9facaeb44a7496e290372db6150cd1c71764a20b1bd0638f39c79408fb80496", targets = new[] { 354, 383, 1094, 1095 }, fieldTargets = new[] { 354, 383, 1094, 1095 } },
            new Slice { name = "WuDang", resource = "PcWuDangRelationshipTargets", sourceTxt = "SKL-WD-PROOF-001/PcWuDangRelationshipTargets.txt", sourceProvenance = "SKL-WD-PROOF-001/PcWuDangRelationshipTargets.provenance.json", txtSha = "43cd59abc3aa3d298104eb79c64f9c0013f2fa6d9550ce5f27fbb49ac1a91046", provenanceSha = "0ab7a99a65e3a0bc3bea952934c1af4696a1ec8bee4f12f6464811245c519ebf", targets = new[] { 371, 738, 1107 }, fieldTargets = new[] { 371, 738, 1107 } },
        };

        private static readonly Link[] ParentLinks =
        {
            new Link { slice = "EMei", parent = 82, column = "StartSkillId", target = 243 },
            new Link { slice = "EMei", parent = 328, column = "StartSkillId", target = 329 },
            new Link { slice = "EMei", parent = 380, column = "StartSkillId", target = 331 },
            new Link { slice = "EMei", parent = 1061, column = "StartSkillId", target = 1089 },
            new Link { slice = "EMei", parent = 1114, column = "FlySkillId", target = 1115 },
            new Link { slice = "CuiYan", parent = 102, column = "StartSkillId", target = 398 },
            new Link { slice = "CuiYan", parent = 111, column = "StartSkillId", target = 112 },
            new Link { slice = "CuiYan", parent = 337, column = "FlySkillId", target = 338 },
            new Link { slice = "CuiYan", parent = 1063, column = "CollidSkillId", target = 1064 },
            new Link { slice = "CuiYan", parent = 1065, column = "StartSkillId", target = 1102 },
            new Link { slice = "CuiYan", parent = 1065, column = "FlySkillId", target = 1093 },
            new Link { slice = "TianRen", parent = 148, column = "StartSkillId", target = 192 },
            new Link { slice = "TianRen", parent = 362, column = "VanishedSkillId", target = 363 },
            new Link { slice = "TianRen", parent = 715, column = "StartSkillId", target = 723 },
            new Link { slice = "TianRen", parent = 1075, column = "StartSkillId", target = 1131 },
            new Link { slice = "TianRen", parent = 1076, column = "VanishedSkillId", target = 363 },
            new Link { slice = "WuDu", parent = 353, column = "VanishedSkillId", target = 354 },
            new Link { slice = "WuDu", parent = 355, column = "CollidSkillId", target = 383 },
            new Link { slice = "WuDu", parent = 1066, column = "StartSkillId", target = 1094 },
            new Link { slice = "WuDu", parent = 1067, column = "CollidSkillId", target = 1095 },
            new Link { slice = "WuDang", parent = 368, column = "StartSkillId", target = 371 },
            new Link { slice = "WuDang", parent = 716, column = "StartSkillId", target = 738 },
            new Link { slice = "WuDang", parent = 1079, column = "StartSkillId", target = 1107 },
        };

        private static readonly int[] ExpectedSupportOnlyIds = { 112, 243, 329, 338, 354, 383, 398, 723, 738, 1064, 1089, 1093, 1094, 1095, 1102, 1107, 1115, 1131 };
        private static readonly int[] CrossOwnedExistingIds = { 192, 331, 337, 363, 371 };

        private static string ResourceRoot => Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "Reference", "PcRelationshipTargets");
        private static string StoryRoot => Path.Combine(Directory.GetCurrentDirectory(), "harness", "docs", "stories");

        [Test]
        public void PackagedSlices_AreExactStoryBytes()
        {
            foreach (var slice in Slices)
            {
                AssertExact(slice, ".txt", slice.txtSha, slice.sourceTxt);
                AssertExact(slice, ".provenance.json", slice.provenanceSha, slice.sourceProvenance);

                var bundled = UnityEngine.Resources.Load<TextAsset>("Reference/PcRelationshipTargets/" + slice.resource);
                Assert.IsNotNull(bundled, slice.name + " Resources slice missing");
                Assert.AreEqual(slice.txtSha, Sha256Hex(bundled.bytes), slice.name + " Resources bytes drifted");
            }
        }

        [Test]
        public void Catalog_ResolvesTargets_AndPreservesCanonicalFields()
        {
            foreach (var slice in Slices)
            {
                var catalog = CatalogFor(slice.name);
                var rows = ParseRows(File.ReadAllBytes(Path.Combine(ResourceRoot, slice.resource + ".txt")));
                foreach (int id in slice.targets)
                    Assert.IsNotNull(catalog.Resolve(id), slice.name + " target " + id + " must resolve");

                foreach (int id in slice.fieldTargets)
                    AssertFields(rows[id], catalog.Resolve(id), slice.name + " target " + id);
            }
        }

        [Test]
        public void LearnedSlices_LinkParentsToTargets_AndFullCatalogResolvesTargets()
        {
            var full = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            foreach (var link in ParentLinks)
            {
                var row = ParseRows(File.ReadAllBytes(LearnedSlicePath(link.slice)))[link.parent];
                Assert.AreEqual(link.target, Int(row, link.column), $"{link.slice} {link.parent}.{link.column}");
                Assert.IsNotNull(full.Resolve(link.target), $"runtime target {link.target} for {link.slice} {link.parent}.{link.column} must resolve");
            }
        }

        [Test]
        public void SupportTargets_DoNotEnterProgressionOrMaxAll()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var supportIds = NewlyRegisteredSupportOnlyIds(catalog);
            CollectionAssert.AreEquivalent(ExpectedSupportOnlyIds, supportIds, "support-only IDs must be only newly registered relationship targets");
            CollectionAssert.IsEmpty(CrossOwnedExistingIds.Intersect(supportIds).ToArray(), "cross-owned IDs must keep existing/learned semantics");
            foreach (var faction in new[] { CombatFaction.EMei, CombatFaction.CuiYan, CombatFaction.TianRen, CombatFaction.WuDu, CombatFaction.WuDang, CombatFaction.TangMen })
            {
                var granted = new PlayerProgressionState();
                granted.GrantFactionSkillPanelProgression(catalog, faction);
                var maxed = new PlayerProgressionState { faction = faction };
                maxed.MaxAllSkillLevels(catalog);

                foreach (int id in supportIds)
                {
                    Assert.IsFalse(granted.knownSkills.Contains(id), $"{faction} grant promoted support {id}");
                    Assert.IsFalse(granted.skillLevels.ContainsKey(id), $"{faction} grant leveled support {id}");
                    Assert.IsFalse(maxed.knownSkills.Contains(id), $"{faction} max-all promoted support {id}");
                    Assert.IsFalse(maxed.skillLevels.ContainsKey(id), $"{faction} max-all leveled support {id}");
                    Assert.IsFalse(granted.CanUpgradeSkill(catalog.Resolve(id)), $"{faction} can upgrade support {id}");
                }
            }
        }

        [Test]
        public void CrossFactionExistingDefinitions_AreNotOverwritten()
        {
            var full = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var emeiOnly = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: false, includeShaolin: false, includeTangMen: false,
                includeEMei: true, includeTianWang: false, includeWuDu: false,
                includeCuiYan: false, includeTianRen: false, includeKunLun: false);

            StringAssert.StartsWith("TM_Rel_", full.Resolve(331).nameNormalized, "TangMen 331 must win in full catalog");
            StringAssert.StartsWith("EM_Rel_", emeiOnly.Resolve(331).nameNormalized, "EMei 331 loads when no earlier owner exists");
        }

        private static void AssertExact(Slice slice, string extension, string sha, string sourceRelative)
        {
            var asset = File.ReadAllBytes(Path.Combine(ResourceRoot, slice.resource + extension));
            var source = File.ReadAllBytes(Path.Combine(StoryRoot, sourceRelative));
            Assert.AreEqual(sha, Sha256Hex(source), slice.name + " source" + extension + " drifted");
            Assert.AreEqual(sha, Sha256Hex(asset), slice.name + " asset" + extension + " drifted");
            CollectionAssert.AreEqual(source, asset, slice.name + " asset" + extension + " must equal story bytes");
        }

        private static int[] NewlyRegisteredSupportOnlyIds(SkillCatalog catalog)
        {
            return Slices.SelectMany(s => s.targets)
                .Distinct()
                .Where(id => catalog.Resolve(id)?.faction == CombatFaction.None)
                .Except(CrossOwnedExistingIds)
                .OrderBy(id => id)
                .ToArray();
        }

        private static SkillCatalog CatalogFor(string name)
        {
            return PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: name == "WuDang",
                includeShaolin: false,
                includeTangMen: false,
                includeEMei: name == "EMei",
                includeTianWang: false,
                includeWuDu: name == "WuDu",
                includeCuiYan: name == "CuiYan" || name == "TianRen",
                includeTianRen: name == "TianRen",
                includeKunLun: false);
        }

        private static string LearnedSlicePath(string name)
        {
            string dir = name == "EMei" ? "SKL-EM-PROOF-001" : name == "CuiYan" ? "SKL-CY-PROOF-001" : name == "TianRen" ? "SKL-TR-PROOF-001" : name == "WuDu" ? "SKL-WDU-PROOF-001" : "SKL-WD-PROOF-001";
            string file = name == "EMei" ? "PcEMeiSkills.txt" : name == "CuiYan" ? "PcCuiYanSkills.txt" : name == "TianRen" ? "PcTianRenSkills.txt" : name == "WuDu" ? "PcWuDuSkills.txt" : "PcWuDangSkills.txt";
            return Path.Combine(StoryRoot, dir, file);
        }

        private static Dictionary<int, Dictionary<string, string>> ParseRows(byte[] bytes)
        {
            var lines = Encoding.ASCII.GetString(bytes).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var header = lines[0].Split('\t');
            var rows = new Dictionary<int, Dictionary<string, string>>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                var cols = lines[i].Split('\t');
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int c = 0; c < header.Length && c < cols.Length; c++) row[header[c]] = cols[c].Trim();
                int id = Int(row, "SkillId", -1);
                if (id > 0) rows[id] = row;
            }
            return rows;
        }

        private static void AssertFields(Dictionary<string, string> row, SkillDefinition skill, string label)
        {
            Assert.AreEqual(Int(row, "SkillStyle"), (int)skill.skillStyle, label + " SkillStyle");
            Assert.AreEqual(Int(row, "MisslesForm"), (int)skill.missileForm, label + " MisslesForm");
            Assert.AreEqual(Int(row, "ChildSkillId"), skill.childSkillId, label + " ChildSkillId");
            Assert.AreEqual(Int(row, "ChildSkillLevel"), skill.childSkillLevel, label + " ChildSkillLevel");
            Assert.AreEqual(Int(row, "ChildSkillNum"), skill.childSkillNum, label + " ChildSkillNum");
            int startSkillId = ExpectedEventTarget(row, "StartSkillId");
            int flySkillId = ExpectedEventTarget(row, "FlySkillId");
            int collideSkillId = ExpectedEventTarget(row, "CollidSkillId");
            int vanishSkillId = ExpectedEventTarget(row, "VanishedSkillId");
            int eventSkillLevel = Int(row, "EventSkillLevel");
            Assert.AreEqual(startSkillId, skill.startSkillId, label + " StartSkillId");
            Assert.AreEqual(flySkillId, skill.flySkillId, label + " FlySkillId");
            Assert.AreEqual(flySkillId > 0 ? Int(row, "FlyEventTime") : 0, skill.flyEventTime, label + " FlyEventTime");
            Assert.AreEqual(collideSkillId, skill.collideSkillId, label + " CollidSkillId");
            Assert.AreEqual(vanishSkillId, skill.vanishSkillId, label + " VanishedSkillId");
            Assert.AreEqual(startSkillId > 0 ? eventSkillLevel : 0, skill.startSkillLevel, label + " EventSkillLevel/start");
            Assert.AreEqual(flySkillId > 0 ? eventSkillLevel : 0, skill.flySkillLevel, label + " EventSkillLevel/fly");
            Assert.AreEqual(collideSkillId > 0 ? eventSkillLevel : 0, skill.collideSkillLevel, label + " EventSkillLevel/collide");
            Assert.AreEqual(vanishSkillId > 0 ? eventSkillLevel : 0, skill.vanishSkillLevel, label + " EventSkillLevel/vanish");
            Assert.AreEqual(Int(row, "AttackRadius"), skill.attackRadius, label + " AttackRadius");
            Assert.AreEqual(Int(row, "CharAnimId"), skill.charAnimId, label + " CharAnimId");
            Assert.AreEqual(Int(row, "TimePerCastOnHorse"), skill.timePerCastOnHorse, label + " TimePerCastOnHorse");
            Assert.AreEqual(Int(row, "EqtLimit", -2), skill.equipLimit, label + " EqtLimit");
            Assert.AreEqual(Int(row, "HorseLimit"), skill.horseLimit, label + " HorseLimit");
            Assert.AreEqual(Int(row, "ByMissle") != 0, skill.byMissile, label + " ByMissle");
            Assert.AreEqual(Int(row, "IsMelee") != 0, skill.isMelee, label + " IsMelee");
            Assert.AreEqual(CombatFaction.None, skill.faction, label + " must stay support-only");
        }

        private static int ExpectedEventTarget(Dictionary<string, string> row, string column)
        {
            int target = Int(row, column);
            return target == 1091 || target == 1096 ? 0 : target;
        }

        private static int Int(Dictionary<string, string> row, string column, int fallback = 0)
        {
            return row.TryGetValue(column, out var raw) && int.TryParse(raw, out int value) ? value : fallback;
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
