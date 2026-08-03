// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillParser
// Parses the three JX sources into SkillRow records and resolves SPR staging
// fail-closed (ticket 26). Pure logic: input = raw bytes, output = catalog;
// staged lookup injected as delegate so EditMode tests stay IO-free.
//
// Column map (PcSkills.txt, 0-indexed, VERIFIED against the real header):
//   2→Id, 70→Faction (LvlSetScript — NOT 71; col 71 = LvlSetting1, the
//   Sandbox PcSkillFullParser.LvlSetScriptCol=71 bug we do NOT copy),
//   19→Form, 26→IsMelee, 20→ChildMissileId, 6→PreCastSprUid,
//   58/60→Fan Param1/2 (1/64 vòng), 52/53→Req/MaxLevel, 71-110→LevelScaling
//   (LvlSetting1-20 at 71,73..109; LvlData1-20 at 72,74..110).
// Display file (114 cols) has its own layout: SkillId=2, Name=0, Faction=71.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VLTK.Survivor
{
    public static class SurvivorSkillParser
    {
        // --- PcSkills.txt columns (0-indexed) ---
        public const int ColName = 0;
        public const int ColSkillId = 2;
        public const int ColPreCastSpr = 6;
        public const int ColIsAura = 11;
        public const int ColAttackRadius = 14;
        public const int ColMisslesForm = 19;
        public const int ColChildSkillId = 20;
        public const int ColChildSkillNum = 22; // magic misslenum → PC m_nChildSkillNum (fan count, ticket 27)
        public const int ColIsMelee = 26;
        public const int ColTimePerCast = 31;
        public const int ColIsPhysical = 33;
        public const int ColByMissle = 41;
        public const int ColReqLevel = 52;
        public const int ColMaxLevel = 53;
        public const int ColParam1 = 58;
        public const int ColParam2 = 60;
        public const int ColLvlSetScript = 70;
        public const int ColLvlSetting1 = 71;
        public const int ColLevelUpScript = 111;
        public const int ColSkillDesc = 112;

        // --- Display file columns (its own 114-col layout) ---
        public const int DisplayColName = 0;
        public const int DisplayColSkillId = 2;
        public const int DisplayColPreCastSpr = 6;
        public const int DisplayColFaction = 71;

        // --- missles.txt columns ---
        public const int MissileColId = 0;
        public const int MissileColMoveKind = 2;
        public const int MissileColLifeTime = 10;
        public const int MissileColSpeed = 11;
        public const int MissileColResponseSkill = 18;
        public const int MissileColAnimFile2 = 32; // primary visual; AnimFile1 (29) luôn trống

        /// <summary>10 phái + GBK subdir variants (research: ~452 chính phái, thực tế 476).</summary>
        public static readonly string[] PlayerFactions =
        {
            "shaolin", "saolin", "tangmen", "tangmeng", "cuiyan", "emei",
            "tianwang", "kunlun", "wudu", "wudang", "tianren", "gaibang",
        };

        private sealed class MissileRow
        {
            public int Id;
            public int MoveKind;
            public float Speed;
            public float LifeTime;
            public int ResponseSkill;
            public byte[] AnimFile2Bytes = Array.Empty<byte>();
        }

        public static SurvivorSkillCatalog Parse(
            byte[] pcSkillsBytes, byte[] displayBytes, byte[] missilesBytes,
            Func<string, bool> isStaged)
        {
            var catalog = new SurvivorSkillCatalog();
            var missiles = ParseMissiles(missilesBytes);
            catalog.MissileRows = missiles.Count;

            // Display file = byte-preserving TCVN3/GBK: col6 PreCastSpr giữ nguyên
            // GBK bytes gốc → nguồn ưu tiên khi hash (không phụ thuộc transcode).
            var display = ParseDisplayTable(displayBytes);
            catalog.DisplayFileRows = display.Ids.Count;

            var lines = SplitRows(pcSkillsBytes);
            bool headerSkipped = false;
            var seenIds = new HashSet<int>();
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < ColLvlSetScript + 1) continue;

                var row = ParseSkillRow(cols, display.Ids);
                if (row == null) continue;

                // Data PC có row trùng id (vd 521 × 2) → dedupe giữ row đầu, fail-closed deterministic.
                if (!seenIds.Add(row.Id)) { catalog.DuplicateIds.Add(row.Id); continue; }

                display.PreCastBytes.TryGetValue(row.Id, out byte[] rawPreCastBytes);
                ResolvePreCast(row, rawPreCastBytes, isStaged, catalog);
                ResolveChildMissile(row, missiles, isStaged, catalog);

                catalog.Skills.Add(row);
            }
            return catalog;
        }

        // ------------------------------------------------------------------
        // Row parsing
        // ------------------------------------------------------------------

        private static SkillRow ParseSkillRow(string[] cols, HashSet<int> displayIds)
        {
            int id = Int(cols, ColSkillId);
            if (id <= 0 && string.IsNullOrWhiteSpace(Str(cols, ColName))) return null;

            var row = new SkillRow
            {
                Id = id,
                Name = SkillTextCodec.DecodeUtf8Name(Str(cols, ColName)),
                Desc = SkillTextCodec.DecodeUtf8Name(Str(cols, ColSkillDesc)),
                Faction = FactionKey(Str(cols, ColLvlSetScript)),
                Form = Int(cols, ColMisslesForm),
                IsMelee = Int(cols, ColIsMelee) > 0,
                SpawnsMissile = Int(cols, ColByMissle) > 0,
                IsAura = Int(cols, ColIsAura) > 0,
                ChildMissileId = Int(cols, ColChildSkillId),
                ChildSkillNum = Int(cols, ColChildSkillNum),
                PreCastPath = Str(cols, ColPreCastSpr),
                FanParam1 = Int(cols, ColParam1),
                FanParam2 = Int(cols, ColParam2),
                ReqLevel = Int(cols, ColReqLevel),
                MaxLevel = Int(cols, ColMaxLevel),
                AttackRadius = Int(cols, ColAttackRadius),
                TimePerCast = Float(cols, ColTimePerCast),
                IsPhysical = Int(cols, ColIsPhysical) > 0,
            };

            for (int i = 0; i < 20; i++)
            {
                row.LvlScripts[i] = Str(cols, ColLvlSetting1 + i * 2);
                row.LvlData[i] = Int(cols, ColLvlSetting1 + 1 + i * 2);
            }

            row.Pool = IsPlayerFaction(row.Faction) ? SurvivorSkillPool.Player : SurvivorSkillPool.BossNpc;
            row.InDisplayFile = displayIds.Contains(row.Id);
            row.SupplyTag = ClassifySupply(cols);
            return row;
        }

        /// <summary>
        /// Faction = LvlSetScript (col 70), first path segment after \script\skill\
        /// (e.g. shaolin.lua → shaolin; saolin\xxx.lua → saolin; special\xxx.lua → special).
        /// </summary>
        public static string FactionKey(string lvlSetScript)
        {
            if (string.IsNullOrEmpty(lvlSetScript)) return "";
            var segs = lvlSetScript.Replace('/', '\\').Split('\\');
            string key = segs.Length > 3 ? segs[3] : "";
            if (key.EndsWith(".lua", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(0, key.Length - 4);
            return key;
        }

        public static bool IsPlayerFaction(string faction)
        {
            for (int i = 0; i < PlayerFactions.Length; i++)
                if (PlayerFactions[i] == faction) return true;
            return false;
        }

        private static SurvivorSupplyTag ClassifySupply(string[] cols)
        {
            if (Int(cols, ColIsAura) > 0) return SurvivorSupplyTag.Aura;
            string script = Str(cols, ColLvlSetting1); // col 71 = LvlSetting1
            if (script == "lifereplenish_v" || script == "lifemax_v") return SurvivorSupplyTag.Heal;
            if (script == "physicsdamage_v" || Str(cols, ColLvlSetScript).Contains("bomb.lua"))
                return SurvivorSupplyTag.Bomb;
            return SurvivorSupplyTag.None;
        }

        // ------------------------------------------------------------------
        // Fail-closed resolution
        // ------------------------------------------------------------------

        private static void ResolvePreCast(SkillRow row, byte[] rawDisplayBytes, Func<string, bool> isStaged, SurvivorSkillCatalog catalog)
        {
            if (row.PreCastPath.Length == 0 && (rawDisplayBytes == null || rawDisplayBytes.Length == 0)) return;
            catalog.PreCastNonEmpty++;
            // Ưu tiên GBK bytes gốc từ display file (byte-preserving) — .NET cp936
            // encode lệch GBK gốc cho char dual-mapping (vd 峨 EFFA vs B6EB), nên
            // không encode lại từ chuỗi. Fallback = PcSkills path: file là UTF-8
            // transcode → decode UTF-8 trước (nếu không mojibake bị double-mangle
            // qua latin1), rồi reverse-TCVN3 (mojibake) / GB2312 (proper-Chinese).
            byte[] bytes = rawDisplayBytes != null && rawDisplayBytes.Length > 0
                ? rawDisplayBytes
                : SkillTextCodec.PathToHashBytes(SkillTextCodec.DecodeUtf8Name(row.PreCastPath));
            string uid = TryStaged(bytes, isStaged);
            if (uid != null)
            {
                row.PreCastSprUid = uid;
                catalog.PreCastStaged++;
            }
            else
            {
                catalog.FailClosedNoPreCastStaged.Add(new SkillFailEntry
                { SkillId = row.Id, Detail = "precast unstaged", Path = row.PreCastPath });
            }
        }

        private static void ResolveChildMissile(SkillRow row, Dictionary<int, MissileRow> missiles,
            Func<string, bool> isStaged, SurvivorSkillCatalog catalog)
        {
            if (row.ChildMissileId == 0) return;
            if (!missiles.TryGetValue(row.ChildMissileId, out var m))
            {
                catalog.FailClosedNoChildMissileRow.Add(new SkillFailEntry
                { SkillId = row.Id, Detail = "child missile missing from missles.txt", Path = row.ChildMissileId.ToString() });
                return;
            }

            // Behavior always carried (P2 cast logic); visual fail-closed.
            row.ChildMissile = new MissileVisualInfo
            {
                Id = m.Id,
                MoveKind = m.MoveKind,
                Speed = m.Speed,
                LifeTime = m.LifeTime,
                ResponseSkill = m.ResponseSkill,
            };

            if (m.AnimFile2Bytes.Length == 0)
            {
                // PC cũng không có visual (vd child 20/408/274/1083-1088) → fail-closed đúng, không bug.
                catalog.FailClosedNoChildAnimFile.Add(new SkillFailEntry
                { SkillId = row.Id, Detail = "child missile has no AnimFile2", Path = row.ChildMissileId.ToString() });
                return;
            }

            string uid = TryStaged(m.AnimFile2Bytes, isStaged);
            if (uid != null)
            {
                row.ChildMissile.AnimFileUid = uid;
                catalog.ChildVisualResolved++;
            }
            else
            {
                catalog.FailClosedNoChildAnimStaged.Add(new SkillFailEntry
                { SkillId = row.Id, Detail = "child missile anim unstaged", Path = row.ChildMissileId.ToString() });
            }
        }

        /// <summary>Signed hash first (PC pack hash), unsigned fallback — mirrors SprRuntimeService probing both.</summary>
        private static string TryStaged(byte[] bytes, Func<string, bool> isStaged)
        {
            string signed = JxPathHash.ComputePathUidHex(bytes, signedBytes: true);
            if (signed != null && isStaged(signed)) return signed;
            string unsigned = JxPathHash.ComputePathUidHex(bytes, signedBytes: false);
            if (unsigned != null && isStaged(unsigned)) return unsigned;
            return null;
        }

        // ------------------------------------------------------------------
        // Table readers
        // ------------------------------------------------------------------

        private static Dictionary<int, MissileRow> ParseMissiles(byte[] missilesBytes)
        {
            var map = new Dictionary<int, MissileRow>();
            bool headerSkipped = false;
            foreach (var line in SplitRows(missilesBytes))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MissileColAnimFile2 + 1) continue;
                int id = Int(cols, MissileColId);
                if (id <= 0) continue;
                map[id] = new MissileRow
                {
                    Id = id,
                    MoveKind = Int(cols, MissileColMoveKind),
                    Speed = Float(cols, MissileColSpeed),
                    LifeTime = Float(cols, MissileColLifeTime),
                    ResponseSkill = Int(cols, MissileColResponseSkill),
                    AnimFile2Bytes = SkillTextCodec.Latin1Bytes(Str(cols, MissileColAnimFile2)),
                };
            }
            return map;
        }

        private sealed class DisplayTable
        {
            public HashSet<int> Ids = new HashSet<int>();
            public Dictionary<int, byte[]> PreCastBytes = new Dictionary<int, byte[]>();
        }

        private static DisplayTable ParseDisplayTable(byte[] displayBytes)
        {
            var table = new DisplayTable();
            bool headerSkipped = false;
            foreach (var line in SplitRows(displayBytes))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < DisplayColFaction + 1) continue;
                int id = Int(cols, DisplayColSkillId);
                if (id <= 0) continue;
                table.Ids.Add(id);
                var precast = Str(cols, DisplayColPreCastSpr);
                if (precast.Length > 0)
                    table.PreCastBytes[id] = SkillTextCodec.Latin1Bytes(precast);
            }
            return table;
        }

        /// <summary>Byte-preserving split: latin1 (1 byte = 1 char) so raw GBK path bytes survive.</summary>
        public static string[] SplitRows(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<string>();
            string text = Encoding.GetEncoding(28591).GetString(data); // Latin1 — Encoding.Latin1 không có trong Unity .NET Standard 2.1
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd('\r');
            return lines;
        }

        // ------------------------------------------------------------------
        // Cell accessors
        // ------------------------------------------------------------------

        private static string Str(string[] cols, int index)
        {
            return index >= 0 && index < cols.Length ? cols[index] : "";
        }

        private static int Int(string[] cols, int index)
        {
            string s = Str(cols, index);
            if (s.Length == 0) return 0;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static float Float(string[] cols, int index)
        {
            string s = Str(cols, index);
            if (s.Length == 0) return 0f;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : 0f;
        }
    }
}
