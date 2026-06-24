// -----------------------------------------------------------------------------
// VLTK Mobile — JX skill slots / combat slots state (port of KgameWorldVN.cpp)
//
// Nguồn:
//  - Main/Left skill: setattackSprInfo(skillIdx, genre, iconPath, isSave)
//    → mainattackSkill = skillIdx; Player.SetLeftSkill; save ACC ini skill_<name>
//    → {left/genre/path}. Validation: skillIdx > 0 && skillIdx < MAX_SKILL.
//  - Aux/Right skill slots: auxiliarySkillData[MAX_FUZHUSKILL_COUNT=8]
//    ("tong so skill phim phu" = tổng số skill phím phụ). Mỗi slot:
//    m_skillidx, _nextUseTime (cooldown ms), timeLoopLayer (overlay), cdLabel.
//  - MAX_SKILL = 2000 (SkillDef.h). MAX_FUZHUSKILL_COUNT = 8 (KgameWorld.h).
//  - Swap/reorder slot: auxiliaryskillCallback + drag; ACC ini "right" persist.
//  - Cooldown overlay: timeLoopLayer->setScaleY(remainingMs * nPer); về 0 khi hết.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Logic assignment/cooldown/
// swap/persist là phần verify được; icon texture (SPR) là asset layer riêng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.UI.JxCocos
{
    /// <summary>Loại slot kỹ năng (main attack vs auxiliary shortcut).</summary>
    public enum JxSkillSlotKind
    {
        Main = 0,        // left/main attack skill (setattackSprInfo)
        Auxiliary = 1,   // right/auxiliary shortcut slot (auxiliarySkillData)
    }

    /// <summary>1 ô combat skill (main hoặc auxiliary).</summary>
    public sealed class JxSkillSlot
    {
        public JxSkillSlotKind Kind;
        /// <summary>Vị trí slot: Main=0; Auxiliary=0..7.</summary>
        public int SlotIndex;
        /// <summary>ID skill gắn (m_skillidx). 0 = rỗng.</summary>
        public int SkillId;
        /// <summary>Genre skill (uGenre, nguồn ini genre).</summary>
        public int Genre;
        /// <summary>Đường dẫn SPR icon (icoPath → path ini).</summary>
        public string IconPath = string.Empty;
        /// <summary>Thời điểm (ms epoch) hết cooldown (m_nextUseTime). 0 = sẵn sàng.</summary>
        public long NextUseTimeMs;
    }

    /// <summary>
    /// State thuần cho 9 combat skill slots (1 main + 8 auxiliary). Verify được
    /// trong EditMode. Assignment/cooldown/swap/persist theo nguồn KgameWorldVN.
    /// </summary>
    public sealed class JxSkillSlotState
    {
        /// <summary>8 ô auxiliary (nguồn: MAX_FUZHUSKILL_COUNT).</summary>
        public const int AuxiliarySlotCount = 8;

        /// <summary>MAX_SKILL (nguồn: SkillDef.h). skillIdx phải &gt; 0 && &lt; này.</summary>
        public const int MaxSkill = 2000;

        private readonly JxSkillSlot _main = new() { Kind = JxSkillSlotKind.Main, SlotIndex = 0 };
        private readonly JxSkillSlot[] _aux = new JxSkillSlot[AuxiliarySlotCount];

        public JxSkillSlotState()
        {
            for (int i = 0; i < AuxiliarySlotCount; i++)
                _aux[i] = new JxSkillSlot { Kind = JxSkillSlotKind.Auxiliary, SlotIndex = i };
        }

        /// <summary>Ô main/left attack skill.</summary>
        public JxSkillSlot Main => _main;

        /// <summary>Truy cập ô auxiliary theo index [0..7].</summary>
        public JxSkillSlot Aux(int index)
        {
            if (index < 0 || index >= AuxiliarySlotCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _aux[index];
        }

        /// <summary>Tất cả ô auxiliary (chỉ-đọc snapshot).</summary>
        public IReadOnlyList<JxSkillSlot> AuxiliarySlots => _aux;

        // ---- Assignment (port) ----

        /// <summary>
        /// setattackSprInfo: gán main/left skill. Validation nguồn:
        /// skillIdx &gt; 0 && skillIdx &lt; MAX_SKILL. Trả về false nếu skillId sai.
        /// </summary>
        public bool AssignMain(int skillId, int genre, string iconPath)
        {
            if (!IsValidSkillId(skillId)) return false;
            _main.SkillId = skillId;
            _main.Genre = genre;
            _main.IconPath = iconPath ?? string.Empty;
            return true;
        }

        /// <summary>
        /// Gán auxiliary slot (index 0..7). Validation nguồn: skillIdx hợp lệ.
        /// skillId=0 = xóa slot. Trả về false nếu index/skillId sai.
        /// </summary>
        public bool AssignAux(int slotIndex, int skillId, int genre, string iconPath)
        {
            if (slotIndex < 0 || slotIndex >= AuxiliarySlotCount) return false;
            if (skillId != 0 && !IsValidSkillId(skillId)) return false;
            _aux[slotIndex].SkillId = skillId;
            _aux[slotIndex].Genre = skillId == 0 ? 0 : genre;
            _aux[slotIndex].IconPath = skillId == 0 ? string.Empty : (iconPath ?? string.Empty);
            return true;
        }

        /// <summary>Clear auxiliary slot (skillId=0).</summary>
        public bool ClearAux(int slotIndex) => AssignAux(slotIndex, 0, 0, null);

        /// <summary>
        /// Swap 2 auxiliary slot (drag-reorder). Cả 2 phải hợp lệ index. Trả về
        /// false nếu index sai. Hoán đổi toàn bộ {SkillId, Genre, IconPath}.
        /// </summary>
        public bool SwapAux(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= AuxiliarySlotCount) return false;
            if (slotB < 0 || slotB >= AuxiliarySlotCount) return false;
            if (slotA == slotB) return true;
            var a = _aux[slotA]; var b = _aux[slotB];
            (a.SkillId, b.SkillId) = (b.SkillId, a.SkillId);
            (a.Genre, b.Genre) = (b.Genre, a.Genre);
            (a.IconPath, b.IconPath) = (b.IconPath, a.IconPath);
            return true;
        }

        /// <summary>Validation nguồn: skillIdx &gt; 0 && skillIdx &lt; MAX_SKILL.</summary>
        public static bool IsValidSkillId(int skillId) => skillId > 0 && skillId < MaxSkill;

        // ---- Cooldown (port) ----

        /// <summary>Đặt cooldown cho auxiliary slot: _nextUseTime = now + durationMs.</summary>
        public void SetAuxCooldown(int slotIndex, long nowMs, long durationMs)
        {
            if (slotIndex < 0 || slotIndex >= AuxiliarySlotCount) return;
            _aux[slotIndex].NextUseTimeMs = durationMs <= 0 ? 0 : nowMs + durationMs;
        }

        /// <summary>Đặt cooldown cho main skill.</summary>
        public void SetMainCooldown(long nowMs, long durationMs)
        {
            _main.NextUseTimeMs = durationMs <= 0 ? 0 : nowMs + durationMs;
        }

        /// <summary>Slot đang cooldown không (now &lt; _nextUseTime)?</summary>
        public static bool IsOnCooldown(JxSkillSlot slot, long nowMs) =>
            slot != null && slot.NextUseTimeMs > 0 && nowMs < slot.NextUseTimeMs;

        /// <summary>Thời gian cooldown còn lại (ms). &lt;= 0 = sẵn sàng.</summary>
        public static long CooldownRemainingMs(JxSkillSlot slot, long nowMs) =>
            slot == null ? 0 : Math.Max(0, slot.NextUseTimeMs - nowMs);

        /// <summary>
        /// Tỉ lệ cooldown còn lại [0..1] cho overlay (timeLoopLayer setScaleY).
        /// Trả về 0 khi sẵn sàng, 1 khi vừa bắt đầu cooldown (nếu duration>0).
        /// </summary>
        public static float CooldownFraction(JxSkillSlot slot, long nowMs, long totalDurationMs)
        {
            if (slot == null || totalDurationMs <= 0 || !IsOnCooldown(slot, nowMs)) return 0f;
            return (float)CooldownRemainingMs(slot, nowMs) / totalDurationMs;
        }

        /// <summary>Slot rỗng không (skillId==0)?</summary>
        public static bool IsEmpty(JxSkillSlot slot) => slot == null || slot.SkillId == 0;

        // ---- Persist (port ACC ini, model dạng dict — controller làm IO thật) ----

        /// <summary>
        /// Snapshot dạng key/value theo ACC ini nguồn: key "skill_&lt;name&gt;" →
        /// {left, genre, path, right}. right = comma-list "slotIdx:skillId:genre:path".
        /// LoadFromIni đảo ngược. Controller ghi/đọc file ACC thật.
        /// </summary>
        public Dictionary<string, string> SaveSnapshot(string charName)
        {
            var dict = new Dictionary<string, string>(4);
            string key = "skill_" + (charName ?? string.Empty);
            dict[key + ".left"] = _main.SkillId.ToString();
            dict[key + ".genre"] = _main.Genre.ToString();
            dict[key + ".path"] = _main.IconPath ?? string.Empty;
            var right = new List<string>(AuxiliarySlotCount);
            for (int i = 0; i < AuxiliarySlotCount; i++)
            {
                var s = _aux[i];
                if (s.SkillId != 0)
                    right.Add(i + ":" + s.SkillId + ":" + s.Genre + ":" + (s.IconPath ?? string.Empty));
            }
            dict[key + ".right"] = string.Join("|", right);
            return dict;
        }

        /// <summary>Load từ snapshot dict (đảo ngược SaveSnapshot). Bỏ qua key lỗi.</summary>
        public void LoadSnapshot(string charName, IReadOnlyDictionary<string, string> dict)
        {
            if (dict == null) return;
            string key = "skill_" + (charName ?? string.Empty);
            if (dict.TryGetValue(key + ".left", out var left) && int.TryParse(left, out int leftId))
                _main.SkillId = IsValidSkillId(leftId) ? leftId : 0;
            if (dict.TryGetValue(key + ".genre", out var g) && int.TryParse(g, out int genre))
                _main.Genre = genre;
            if (dict.TryGetValue(key + ".path", out var p))
                _main.IconPath = p ?? string.Empty;
            // reset aux trước khi load
            for (int i = 0; i < AuxiliarySlotCount; i++) { _aux[i].SkillId = 0; _aux[i].Genre = 0; _aux[i].IconPath = string.Empty; }
            if (dict.TryGetValue(key + ".right", out var right) && !string.IsNullOrEmpty(right))
            {
                foreach (var entry in right.Split('|'))
                {
                    if (string.IsNullOrEmpty(entry)) continue;
                    var parts = entry.Split(':');
                    if (parts.Length < 4) continue;
                    if (!int.TryParse(parts[0], out int idx)) continue;
                    if (!int.TryParse(parts[1], out int sid)) continue;
                    if (!int.TryParse(parts[2], out int sgenre)) continue;
                    string spath = parts[3] ?? string.Empty;
                    AssignAux(idx, sid, sgenre, spath);
                }
            }
        }
    }
}
