// -----------------------------------------------------------------------------
// VLTK Mobile — JX Buff icon row state (port of KuiStateSkillControlVN.cpp)
//
// Nguồn: client/Classes/vn/gameui/KuiStateSkillControlVN.cpp (148 L).
//  - Lưới icon buff active từ m_StateSkillList (m_SkillID>0 && m_LeftTime>0).
//  - 10 cột/row; icon = skillBuffData[skillId].buffPath (fallback SPR).
//  - Label đếm ngược:
//      float time = m_LeftTime / 18;          // 18 ticks = 1 giây
//      if (m_LeftTime <= 18)        → "N/A"
//      else if (time/3600 > 1)      → "%dh"  (Nh = (int)time / 3600)
//      else if (time/60 > 1)        → "%dm"  (Nm = (int)time / 60)
//      else                         → "%ds"  (Ns = (int)time)
//  - Màu (0,255,54) xanh lá, stroke đen width 2.
//  - Layout: nStartX=origin.x+13, nStartY=height+origin.y-87, offX=26, offY=-36.
//  - Ẩn khi không có buff nào (nCountX==0 && nCountY==0 → setVisible(false)).
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Logic countdown là phần
// verify được; icon texture (SPR) là asset layer riêng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>Một buff active trên player (m_StateSkillList entry).</summary>
    public sealed class JxBuff
    {
        public int SkillId;
        /// <summary>Tick còn lại (18 ticks = 1 giây). Nguồn: m_LeftTime.</summary>
        public int LeftTime;
        /// <summary>Đường dẫn SPR icon. Nguồn: skillBuffData[skillId].buffPath.</summary>
        public string BuffPath = string.Empty;
        /// <summary>Tên buff (dịch, hiển thị tooltip).</summary>
        public string Name = string.Empty;
    }

    /// <summary>State thuần cho JX buff icon row. Verify được trong EditMode.</summary>
    public sealed class JxBuffRowState
    {
        /// <summary>18 ticks = 1 giây (nguồn: time = m_LeftTime / 18).</summary>
        public const int TicksPerSecond = 18;

        /// <summary>10 cột mỗi row (nguồn: if (nCountX &gt; 9) wrap).</summary>
        public const int GridColumns = 10;

        /// <summary>Offset X giữa 2 icon (nguồn: nOffSetX = 26).</summary>
        public const float IconSpacingX = 26f;

        /// <summary>Offset Y giữa 2 row (nguồn: nOffSetY = -36, đi xuống).</summary>
        public const float IconSpacingY = -36f;

        /// <summary>Start X (nguồn: nStartX = origin.x + 13).</summary>
        public const float StartOffsetX = 13f;

        /// <summary>Start Y từ đáy màn hình (nguồn: nStartY = height + origin.y - 87).</summary>
        public const float StartOffsetYFromTop = 87f;

        /// <summary>Màu countdown (nguồn: ccc3(0, 255, 54)).</summary>
        public static readonly Color CountdownColor = new(0f / 255f, 255f / 255f, 54f / 255f, 1f);

        private readonly List<JxBuff> _buffs = new();
        private bool _isOpen = true;

        /// <summary>Row đang mở/hiển thị (nguồn: isOpen).</summary>
        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        /// <summary>Danh sách buff active (chỉ-đọc).</summary>
        public IReadOnlyList<JxBuff> Buffs => _buffs;

        /// <summary>Layer có nhìn thấy không — ẩn khi không có buff (nguồn draw_ cuối).</summary>
        public bool IsVisible => _isOpen && _buffs.Count > 0;

        // ---- API port ----

        /// <summary>Thêm 1 buff active. Giữ thứ tự thêm (= thứ tự list source).</summary>
        public void AddBuff(JxBuff buff)
        {
            if (buff != null) _buffs.Add(buff);
        }

        /// <summary>Thay toàn bộ danh sách buff (upData).</summary>
        public void SetBuffs(IEnumerable<JxBuff> buffs)
        {
            _buffs.Clear();
            if (buffs != null) _buffs.AddRange(buffs);
        }

        public void Clear() => _buffs.Clear();

        /// <summary>Tick (giảm 1 tất cả buff, bỏ buff hết). Mô phỏng scheduleUpdate.</summary>
        public void Tick()
        {
            for (int i = _buffs.Count - 1; i >= 0; i--)
            {
                _buffs[i].LeftTime -= 1;
                if (_buffs[i].LeftTime <= 0) _buffs.RemoveAt(i);
            }
        }

        // ---- Countdown text (port-critical, verify được) ----

        /// <summary>
        /// Label đếm ngược theo nguồn draw_. Quy tắc chính xác từ C++:
        ///   leftTime &lt;= 18       → "N/A"
        ///   time/3600 &gt; 1 (float)→ "Nh" với Nh = (int)time / 3600 (int div)
        ///   time/60 &gt; 1 (float)  → "Nm" với Nm = (int)time / 60
        ///   else                  → "Ns" với Ns = (int)time
        /// trong đó time = leftTime / 18 (giây float).
        /// </summary>
        public static string CountdownText(int leftTime)
        {
            if (leftTime <= TicksPerSecond) return "N/A";

            float time = leftTime / (float)TicksPerSecond;
            int intTime = (int)time; // (int)time — truncate
            if (time / 3600f > 1f)
                return (intTime / 3600) + "h";
            if (time / 60f > 1f)
                return (intTime / 60) + "m";
            return intTime + "s";
        }

        /// <summary>Countdown text cho 1 buff entry.</summary>
        public static string CountdownText(JxBuff buff) =>
            buff == null ? "N/A" : CountdownText(buff.LeftTime);

        // ---- Grid layout (port) ----

        /// <summary>(column, row) cho buff thứ index. Wrap tại 10 cột.</summary>
        public static (int col, int row) GridCell(int index)
        {
            int col = index % GridColumns;
            int row = index / GridColumns;
            return (col, row);
        }
    }
}
