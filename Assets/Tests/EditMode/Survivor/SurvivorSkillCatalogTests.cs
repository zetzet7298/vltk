// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSkillCatalogTests
// Ticket 26 self-check: col map (faction col 70 ≠ 71 bug), encodings
// (UTF-8 / TCVN3 / GBK mojibake recovery), pack-hash parity vs staged SPRs,
// fail-closed lists, pool composition, real-data counts.
// Pure logic + guarded real-data reads — no scene, no PlayMode (spec Testing
// Decisions: seam duy nhất = EditMode pure-logic).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSkillCatalogTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static string[] NewRow(int cols = 113)
        {
            var a = new string[cols];
            for (int i = 0; i < cols; i++) a[i] = "";
            return a;
        }

        private static byte[] Latin1(string s) => Encoding.GetEncoding(28591).GetBytes(s);

        private static SurvivorSkillCatalog ParseOne(
            string[] cols, Func<string, bool> staged = null, string[] missileLines = null,
            string[] displayLines = null)
        {
            if (staged == null) staged = _ => true;
            var pc = Encoding.UTF8.GetBytes("header\tline\n" + string.Join("\t", cols));
            byte[] disp = displayLines == null
                ? Array.Empty<byte>()
                : Latin1("header\n" + string.Join("\n", displayLines));
            byte[] mis = missileLines == null
                ? Array.Empty<byte>()
                : Latin1("header\n" + string.Join("\n", missileLines));
            return SurvivorSkillParser.Parse(pc, disp, mis, staged);
        }

        private static string MissileLine(int id, string anim2)
        {
            var c = new string[57];
            for (int i = 0; i < c.Length; i++) c[i] = "";
            c[0] = id.ToString();
            c[2] = "1";
            c[10] = "16";
            c[11] = "12";
            c[18] = "5";
            c[32] = anim2;
            return string.Join("\t", c);
        }

        private static byte[] BytesFromHex(string hex)
        {
            var parts = hex.Split(' ', '\n');
            var bytes = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                bytes[i] = Convert.ToByte(parts[i], 16);
            return bytes;
        }

        private static string RefDir => Path.Combine(Application.dataPath, "StreamingAssets", "Reference");

        private static SurvivorSkillCatalog ParseReal(Func<string, bool> staged)
        {
            var pc = File.ReadAllBytes(Path.Combine(RefDir, "PcSkills.txt"));
            var disp = File.ReadAllBytes(Path.Combine(RefDir, "PcAllFactionLearnedDisplaySkills.txt"));
            var mis = File.ReadAllBytes(Path.Combine(RefDir, "PcAttrib", "missles.txt"));
            return SurvivorSkillParser.Parse(pc, disp, mis, staged);
        }

        private static HashSet<string> LoadStagedUids()
        {
            var root = Path.Combine(Application.dataPath, "..", "SpritesRuntime");
            var set = new HashSet<string>();
            if (Directory.Exists(root))
                foreach (var f in Directory.GetFiles(root, "*.spr"))
                    set.Add(Path.GetFileNameWithoutExtension(f).ToLowerInvariant());
            return set;
        }

        // ------------------------------------------------------------------
        // col map (ticket: 2→Id, 70→Faction, 19→Form, 26→IsMelee, 20→Child,
        // 6→PreCast, 58/60→Fan, 52/53→Req/Max, 71-110→LevelScaling)
        // ------------------------------------------------------------------

        [Test]
        public void ColumnMap_AllTicketFields()
        {
            var cols = NewRow();
            cols[0] = "Công kích vật lý"; // UTF-8 name col
            cols[2] = "42";
            cols[6] = "\\spr\\skill\\shaolin\\sl_01.spr";
            cols[11] = "0";
            cols[14] = "300";
            cols[19] = "7";
            cols[20] = "13";
            cols[26] = "0";
            cols[31] = "1.5";
            cols[33] = "1";
            cols[41] = "1";
            cols[52] = "10";
            cols[53] = "20";
            cols[58] = "3";   // Param1 fan angle step
            cols[60] = "40";  // Param2 fan offset
            cols[70] = "\\script\\skill\\shaolin.lua";
            cols[71] = "addphysicsdamage_p"; // LvlSetting1 (col 71 ≠ faction!)
            cols[72] = "5";   // LvlData1
            cols[73] = "attackratingenhance_p"; // LvlSetting2
            cols[110] = "9";  // LvlData20

            var cat = ParseOne(cols, _ => true, new[] { MissileLine(13, "\\spr\\skill\\x.spr") });
            Assert.AreEqual(1, cat.Skills.Count);
            var r = cat.Skills[0];
            Assert.AreEqual(42, r.Id, "col2 → Id");
            Assert.AreEqual("Công kích vật lý", r.Name, "col0 UTF-8 name");
            Assert.AreEqual("shaolin", r.Faction, "col70 → Faction (LvlSetScript)");
            Assert.AreEqual(7, r.Form, "col19 → Form");
            Assert.IsFalse(r.IsMelee, "col26 → IsMelee");
            Assert.AreEqual(13, r.ChildMissileId, "col20 → ChildMissileId");
            Assert.AreEqual(300, r.AttackRadius, "col14");
            Assert.AreEqual(1.5f, r.TimePerCast, 1e-4f, "col31 → TimePerCast");
            Assert.IsTrue(r.IsPhysical, "col33");
            Assert.IsTrue(r.SpawnsMissile, "col41 ByMissle");
            Assert.AreEqual(10, r.ReqLevel, "col52");
            Assert.AreEqual(20, r.MaxLevel, "col53");
            Assert.AreEqual(3, r.FanParam1, "col58 → Param1");
            Assert.AreEqual(40, r.FanParam2, "col60 → Param2");
            Assert.AreEqual("addphysicsdamage_p", r.LvlScripts[0], "col71 → LvlSetting1");
            Assert.AreEqual(5, r.LvlData[0], "col72 → LvlData1");
            Assert.AreEqual("attackratingenhance_p", r.LvlScripts[1], "col73 → LvlSetting2");
            Assert.AreEqual(9, r.LvlData[19], "col110 → LvlData20");
            Assert.IsNotNull(r.ChildMissile, "child 13 resolved");
            Assert.AreEqual(13, r.ChildMissile.Id);
            Assert.IsNotEmpty(r.PreCastSprUid, "staged stub → uid assigned");
        }

        [Test]
        public void Faction_UsesCol70_LvlSetScript_NotCol71()
        {
            // Regression vs Sandbox PcSkillFullParser.LvlSetScriptCol=71 bug
            // (col 71 thật ra là LvlSetting1 — effect script, không phải faction).
            var cols = NewRow();
            cols[2] = "4";
            cols[70] = "\\script\\skill\\shaolin.lua";
            cols[71] = "addphysicsdamage_p";

            var r = ParseOne(cols).Skills[0];
            Assert.AreEqual("shaolin", r.Faction);
            Assert.AreEqual("addphysicsdamage_p", r.LvlScripts[0]);
            Assert.AreNotEqual("addphysicsdamage_p", r.Faction);
        }

        [Test]
        public void FactionKey_Variants()
        {
            Assert.AreEqual("shaolin", SurvivorSkillParser.FactionKey("\\script\\skill\\shaolin.lua"));
            Assert.AreEqual("saolin", SurvivorSkillParser.FactionKey("\\script\\skill\\saolin\\xxx.lua"));
            Assert.AreEqual("special", SurvivorSkillParser.FactionKey("\\script\\skill\\special\\bomb.lua"));
            Assert.AreEqual("npc", SurvivorSkillParser.FactionKey("/script/skill/npc.lua"));
            Assert.AreEqual("", SurvivorSkillParser.FactionKey(""));
        }

        [Test]
        public void Pool_PlayerTenFactions_Vs_BossNpc()
        {
            string[] factions = { "shaolin", "saolin", "tangmen", "tangmeng", "cuiyan", "emei",
                "tianwang", "kunlun", "wudu", "wudang", "tianren", "gaibang" };
            foreach (var f in factions)
            {
                var cols = NewRow();
                cols[2] = "1";
                cols[70] = "\\script\\skill\\" + f + ".lua";
                Assert.AreEqual(SurvivorSkillPool.Player, ParseOne(cols).Skills[0].Pool, f);
            }
            foreach (var f in new[] { "special", "npc", "partner", "battles", "shipin", "gmskills", "" })
            {
                var cols = NewRow();
                cols[2] = "1";
                if (f.Length > 0) cols[70] = "\\script\\skill\\" + f + ".lua";
                Assert.AreEqual(SurvivorSkillPool.BossNpc, ParseOne(cols).Skills[0].Pool, f);
            }
        }

        // ------------------------------------------------------------------
        // encodings
        // ------------------------------------------------------------------

        [Test]
        public void TcVn3_Decode_KnownNameBytes()
        {
            // "Thiếu Lâm côn pháp" — real bytes từ PcAllFactionLearnedDisplaySkills.txt row id=4.
            var bytes = BytesFromHex("54 68 69 d5 75 20 4c a9 6d 20 63 ab 6e 20 70 68 b8 70");
            Assert.AreEqual("Thiếu Lâm côn pháp", SkillTextCodec.DecodeTcvn3(bytes));
        }

        [Test]
        public void MojibakePath_ReverseTcvn3_RecoversOriginalGbkBytes()
        {
            // id 15 PreCastSpr: mobile copy = TCVN3 mojibake của GBK gốc (PC).
            const string moji = "\\spr\\skill\\ẫÙÁệ\\sl_05_²ằả¯ÃữÍừệọ.spr";
            const string expectedGbk = "\\spr\\skill\\少林\\sl_05_不动明王咒.spr";
            var bytes = SkillTextCodec.PathToHashBytes(moji);
            var gbk = Encoding.GetEncoding("GB2312").GetString(bytes);
            Assert.AreEqual(expectedGbk, gbk, "reverse-TCVN3 → GBK bytes");
            Assert.AreEqual(
                JxPathHash.ComputePathUidHex(JxPathHash.EncodePath(expectedGbk), true),
                JxPathHash.ComputePathUidHex(bytes, true),
                "hash của mojibake-recovered bytes == hash của GBK-encode chuẩn");
        }

        [Test]
        public void Hash_KnownStagedPath_MatchesRealSprUid()
        {
            // missles.txt id 1 AnimFile2 raw GBK bytes — verified staged tại
            // /SpritesRuntime/55542141.spr. Dùng bytes hex gốc (không encode lại:
            // .NET cp936 encode dual-mapping char lệch GBK gốc, vd 峨 EFFA vs B6EB).
            var raw = HexToBytes(
                "5c7370725c736b696c6c5cb6ebe1d25c6d61675f656d5f30325fcbc4cff3cdacb9e92e737072");
            Assert.AreEqual("55542141", JxPathHash.ComputePathUidHex(raw, true));
        }

        // Convert.FromHexString không có trong Unity .NET Standard 2.1 → tự parse.
        private static byte[] HexToBytes(string hex)
        {
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)(HexVal(hex[i * 2]) * 16 + HexVal(hex[i * 2 + 1]));
            return bytes;
        }

        private static int HexVal(char c) => c <= '9' ? c - '0' : char.ToLowerInvariant(c) - 'a' + 10;

        [Test]
        public void SplitRows_BytePreserving()
        {
            var raw = Latin1("a\tb\xC0\xFF\nc\td");
            var lines = SurvivorSkillParser.SplitRows(raw);
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual((char)0xC0, lines[0][3]);
            Assert.AreEqual((char)0xFF, lines[0][4]);
        }

        // ------------------------------------------------------------------
        // fail-closed
        // ------------------------------------------------------------------

        [Test]
        public void FailClosed_UnstagedPreCast_EmptyUid_AndListed()
        {
            var cols = NewRow();
            cols[2] = "7";
            cols[6] = "\\spr\\skill\\shaolin\\sl_01.spr";
            var cat = ParseOne(cols, _ => false);
            var r = cat.Skills[0];
            Assert.IsEmpty(r.PreCastSprUid, "unstaged → không gán (fail-closed)");
            Assert.AreEqual(1, cat.PreCastNonEmpty);
            Assert.AreEqual(0, cat.PreCastStaged);
            Assert.AreEqual(1, cat.FailClosedNoPreCastStaged.Count);
            Assert.AreEqual(7, cat.FailClosedNoPreCastStaged[0].SkillId);
        }

        [Test]
        public void FailClosed_ChildMissingRow_And_NoAnimFile()
        {
            // child 999: không có row trong missles → ChildMissile null.
            var colsA = NewRow();
            colsA[2] = "1";
            colsA[20] = "999";
            var catA = ParseOne(colsA, _ => true, new[] { MissileLine(13, "\\spr\\skill\\x.spr") });
            Assert.IsNull(catA.Skills[0].ChildMissile, "child missing row → fail-closed");
            Assert.AreEqual(1, catA.FailClosedNoChildMissileRow.Count);

            // child 20: có row nhưng AnimFile2 trống (PC cũng không visual) → behavior giữ, visual bỏ.
            var colsB = NewRow();
            colsB[2] = "2";
            colsB[20] = "20";
            var catB = ParseOne(colsB, _ => true, new[] { MissileLine(20, "") });
            var rb = catB.Skills[0];
            Assert.IsNotNull(rb.ChildMissile, "row tồn tại → behavior vẫn có");
            Assert.IsEmpty(rb.ChildMissile.AnimFileUid, "no AnimFile2 → không gán sprite");
            Assert.AreEqual(16f, rb.ChildMissile.LifeTime, 1e-4f);
            Assert.AreEqual(12f, rb.ChildMissile.Speed, 1e-4f);
            Assert.AreEqual(5, rb.ChildMissile.ResponseSkill);
            Assert.AreEqual(1, catB.FailClosedNoChildAnimFile.Count);
            Assert.AreEqual("20", catB.FailClosedNoChildAnimFile[0].Path);
        }

        [Test]
        public void FailClosed_ChildAnimUnstaged_BehaviorKept_VisualDropped()
        {
            var cols = NewRow();
            cols[2] = "3";
            cols[20] = "13";
            var cat = ParseOne(cols, _ => false, new[] { MissileLine(13, "\\spr\\skill\\x.spr") });
            var r = cat.Skills[0];
            Assert.IsNotNull(r.ChildMissile);
            Assert.IsEmpty(r.ChildMissile.AnimFileUid);
            Assert.AreEqual(1, cat.FailClosedNoChildAnimStaged.Count);
            Assert.AreEqual(0, cat.ChildVisualResolved);
        }

        // ------------------------------------------------------------------
        // supply subset + display ids
        // ------------------------------------------------------------------

        [Test]
        public void SupplyTags_HealBombAura_Classification()
        {
            // heal: LvlSetting1 = lifereplenish_v / lifemax_v
            foreach (var s in new[] { "lifereplenish_v", "lifemax_v" })
            {
                var c = NewRow(); c[2] = "1"; c[71] = s;
                Assert.AreEqual(SurvivorSupplyTag.Heal, ParseOne(c).Skills[0].SupplyTag, s);
            }
            // bomb: physicsdamage_v hoặc path *bomb.lua
            var b1 = NewRow(); b1[2] = "2"; b1[71] = "physicsdamage_v";
            Assert.AreEqual(SurvivorSupplyTag.Bomb, ParseOne(b1).Skills[0].SupplyTag);
            var b2 = NewRow(); b2[2] = "3"; b2[70] = "\\script\\skill\\special\\bomb.lua";
            Assert.AreEqual(SurvivorSupplyTag.Bomb, ParseOne(b2).Skills[0].SupplyTag);
            // aura: IsAura col 11 = 1 (ưu tiên trước heal/bomb)
            var a = NewRow(); a[2] = "4"; a[11] = "1"; a[71] = "lifereplenish_v";
            Assert.AreEqual(SurvivorSupplyTag.Aura, ParseOne(a).Skills[0].SupplyTag);
            // none
            var n = NewRow(); n[2] = "5"; n[71] = "physicsenhance_p";
            Assert.AreEqual(SurvivorSupplyTag.None, ParseOne(n).Skills[0].SupplyTag);
        }

        [Test]
        public void DisplayIds_FlagInDisplayFile()
        {
            var cols = NewRow();
            cols[2] = "4";
            // display rows cần ≥ 72 cột (parser yêu cầu đủ col faction 71)
            var d1 = NewRow(114); d1[2] = "3"; d1[0] = "tên";
            var d2 = NewRow(114); d2[2] = "4"; d2[0] = "tên";
            var cat = ParseOne(cols, null, null, new[] { string.Join("\t", d1), string.Join("\t", d2) });
            Assert.AreEqual(2, cat.DisplayFileRows);
            var r = cat.Skills[0];
            Assert.IsTrue(r.InDisplayFile, "id 4 nằm trong display file");
        }

        // ------------------------------------------------------------------
        // real data (guarded — files phải có trong StreamingAssets/Reference)
        // ------------------------------------------------------------------

        [Test]
        public void RealData_FullPipeline_Counts()
        {
            if (!File.Exists(Path.Combine(RefDir, "PcSkills.txt")))
            { Assert.Ignore("Reference/PcSkills.txt missing"); return; }

            var staged = LoadStagedUids();
            var cat = ParseReal(uid => staged.Contains(uid));

            Assert.AreEqual(1215, cat.Skills.Count, "PcSkills.txt data rows (1216 − 1 dup id 521)");
            Assert.AreEqual(476, cat.PlayerPoolCount, "10 phái + GBK variants (research ~452 dùng col 71 sai)");
            Assert.AreEqual(242, cat.DisplayFileRows, "display file rows");
            Assert.AreEqual(441, cat.MissileRows, "missles.txt rows");
            Assert.AreEqual(357, cat.PreCastNonEmpty, "skills có PreCastSpr");
            Assert.IsTrue(cat.PreCastStaged > 200, $"precast staged thực tế {cat.PreCastStaged} (fail-closed ok nếu thấp, nhưng phải > 0)");

            // unique ids — data PC thật có 1 dup (521): dedupe giữ row đầu, dup được track
            var seen = new HashSet<int>();
            foreach (var s in cat.Skills) Assert.IsTrue(seen.Add(s.Id), $"dup id {s.Id}");
            CollectionAssert.Contains(cat.DuplicateIds, 521, "data PC lặp id 521 phải được track");

            // display ids ⊆ PcSkills ids
            var ids = seen;
            foreach (var s in cat.Skills)
                if (s.InDisplayFile) Assert.IsTrue(s.Pool == SurvivorSkillPool.Player, $"display skill {s.Id} ngoài player pool");
        }

        [Test]
        public void RealData_FailClosedChildLists()
        {
            if (!File.Exists(Path.Combine(RefDir, "PcSkills.txt")))
            { Assert.Ignore("Reference/PcSkills.txt missing"); return; }

            var cat = ParseReal(_ => true);

            // child 1083/1084/1087/1088: có trong PcSkills nhưng KHÔNG có row trong missles.txt
            // (AGENTS.md: child missile không AnimFile vd 20/408/274/1083-1088 → fail-closed đúng, PC cũng không visual).
            foreach (var s in cat.Skills)
            {
                if (s.ChildMissileId == 1083 || s.ChildMissileId == 1084 ||
                    s.ChildMissileId == 1087 || s.ChildMissileId == 1088)
                    Assert.IsNull(s.ChildMissile, $"skill {s.Id} child {s.ChildMissileId} phải fail-closed");
            }
            Assert.IsTrue(cat.FailClosedNoChildMissileRow.Count > 0, "phải có entry child-missing");

            // child 20/274/408: có row nhưng AnimFile2 trống → no-anim-file list.
            var paths = new HashSet<string>();
            foreach (var e in cat.FailClosedNoChildAnimFile) paths.Add(e.Path);
            CollectionAssert.Contains(paths, "20", "child 20 no AnimFile2");
            CollectionAssert.Contains(paths, "274", "child 274 no AnimFile2");
            CollectionAssert.Contains(paths, "408", "child 408 no AnimFile2");
        }

        [Test]
        public void RealData_SupplyAndFormCounts()
        {
            if (!File.Exists(Path.Combine(RefDir, "PcSkills.txt")))
            { Assert.Ignore("Reference/PcSkills.txt missing"); return; }

            var cat = ParseReal(_ => true);
            int heal = 0, bomb = 0, aura = 0, form12 = 0, melee = 0, form7 = 0, childNonzero = 0;
            foreach (var s in cat.Skills)
            {
                if (s.SupplyTag == SurvivorSupplyTag.Heal) heal++;
                if (s.SupplyTag == SurvivorSupplyTag.Bomb) bomb++;
                if (s.SupplyTag == SurvivorSupplyTag.Aura) aura++;
                if (s.Form == 12) form12++;
                if (s.IsMelee) melee++;
                if (s.Form == 7) form7++;
                if (s.ChildMissileId != 0) childNonzero++;
            }
            Assert.AreEqual(52, heal, "LvlSetting1 lifereplenish_v/lifemax_v — 65 raw trừ 13 aura-overlap (aura ưu tiên)");
            Assert.AreEqual(30, bomb, "physicsdamage_v 28 + bomb.lua 2");
            Assert.AreEqual(41, aura, "IsAura=1");
            Assert.AreEqual(22, form12, "MisslesForm=12 melee");
            Assert.AreEqual(104, melee, "IsMelee=1");
            Assert.AreEqual(651, form7, "MisslesForm=7 đạn chủ đạo (652 raw − 1 dup id 521 dedupe)");
            Assert.AreEqual(675, childNonzero, "child missile khác 0");
        }

        [Test]
        public void RealData_DisplayNames_MatchPcSkills()
        {
            if (!File.Exists(Path.Combine(RefDir, "PcSkills.txt")))
            { Assert.Ignore("Reference/PcSkills.txt missing"); return; }

            var cat = ParseReal(_ => true);
            var byId = new Dictionary<int, string>();
            foreach (var s in cat.Skills) byId[s.Id] = s.Name;

            var dispLines = SurvivorSkillParser.SplitRows(
                File.ReadAllBytes(Path.Combine(RefDir, "PcAllFactionLearnedDisplaySkills.txt")));
            int matched = 0, total = 0;
            for (int i = 1; i < dispLines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(dispLines[i])) continue;
                var cols = dispLines[i].Split('\t');
                if (cols.Length < 2) continue;
                int id = int.TryParse(cols[2], out var v) ? v : 0;
                if (id <= 0) continue;
                total++;
                var dispName = SkillTextCodec.DecodeTcvn3(cols[0]);
                Assert.IsFalse(dispName.Contains('\uFFFD'), $"U+FFFD trong display name id {id}");
                Assert.IsTrue(byId.ContainsKey(id), $"display id {id} không có trong PcSkills");
                if (Normalize(byId[id]) == Normalize(dispName)) matched++;
            }
            Assert.AreEqual(242, total, "display rows");
            Assert.AreEqual(242, matched, $"tên display (TCVN3) khớp PcSkills (UTF-8) sau normalize — thực khớp {matched}");
        }

        private static string Normalize(string s)
        {
            var sb = new StringBuilder(s.Length);
            bool prevSpace = false;
            foreach (var ch in s.Trim())
            {
                if (char.IsWhiteSpace(ch))
                {
                    if (!prevSpace) sb.Append(' ');
                    prevSpace = true;
                }
                else
                {
                    sb.Append(char.ToLowerInvariant(ch));
                    prevSpace = false;
                }
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------
        // SkillDef SO mapping
        // ------------------------------------------------------------------

        [Test]
        public void SkillDef_FromRow_CopiesFields()
        {
            var row = new SkillRow
            {
                Id = 42,
                Name = "X",
                Faction = "shaolin",
                Pool = SurvivorSkillPool.Player,
                Form = 12,
                IsMelee = true,
                ChildMissileId = 13,
                ChildMissile = new MissileVisualInfo { Id = 13, AnimFileUid = "abc12345", MoveKind = 1, Speed = 12f, LifeTime = 16f, ResponseSkill = 5 },
                PreCastSprUid = "deadbeef",
                FanParam1 = 3,
                ReqLevel = 10,
                MaxLevel = 20,
                SupplyTag = SurvivorSupplyTag.Heal,
                InDisplayFile = true,
            };
            var def = SkillDef.FromRow(row);
            Assert.AreEqual(42, def.Id);
            Assert.AreEqual("shaolin", def.Faction);
            Assert.AreEqual(SurvivorSkillPool.Player, def.Pool);
            Assert.AreEqual(12, def.Form);
            Assert.IsTrue(def.IsMelee);
            Assert.AreEqual("abc12345", def.ChildMissile.AnimFileUid);
            Assert.AreEqual("deadbeef", def.PreCastSprUid);
            Assert.AreEqual(3, def.FanParam1);
            Assert.AreEqual(20, def.MaxLevel);
            Assert.AreEqual(SurvivorSupplyTag.Heal, def.SupplyTag);
            Assert.IsTrue(def.InDisplayFile);
            var def2 = SkillDef.FromRow(new SkillRow { Id = 1, ChildMissile = null });
            Assert.IsNull(def2.ChildMissile, "child null → null (fail-closed giữ)");
        }
    }
}
