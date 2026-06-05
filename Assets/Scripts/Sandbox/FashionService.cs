// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.x Fashion runtime service
// Wraps PcFashionRegistry. PC source: settings/fashion/fashion.txt.
// Quản lý thời trang: lookup theo slot, kiểm tra điều kiện mặc.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Thời Trang: lookup theo slot, giới tính, kiểm tra điều kiện trang bị.
    /// </summary>
    public class FashionService
    {
        public const string LogTag = "Fashion";
        public const string DefaultStreamingDir = "Reference/PcFashion";

        public const int SexMale = 0;
        public const int SexFemale = 1;

        private PcFashionRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public FashionService() { }
        public FashionService(PcFashionRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcFashionRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Fashion registry rỗng");
        }

        public PcFashionEntry GetFashion(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcFashionEntry> GetBySlot(int slot)
            => _reg != null ? _reg.GetBySlot(slot) : Array.Empty<PcFashionEntry>();

        public IReadOnlyList<PcFashionEntry> GetForSex(int sex)
            => _reg != null ? _reg.GetForSex(sex) : Array.Empty<PcFashionEntry>();

        public IReadOnlyList<PcFashionEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcFashionEntry>();

        public bool CanEquip(int fashionId, int playerLevel, int playerSex, int vipLevel)
        {
            var entry = GetFashion(fashionId);
            if (entry == null) return false;
            if (playerLevel < entry.requiredLevel) return false;
            if (entry.requiredVipLevel > vipLevel) return false;
            if (entry.requiredSex >= 0 && entry.requiredSex != playerSex) return false;
            return true;
        }

        public string GetSlotName(int slot)
        {
            switch (slot)
            {
                case 0: return "Tóc";
                case 1: return "Mặt";
                case 2: return "Thân";
                case 3: return "Tay";
                case 4: return "Chân";
                case 5: return "Áo choàng";
                case 6: return "Vũ khí";
                default: return "Khác";
            }
        }

        public static FashionService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcFashionParser.BuildRegistry(dir);
            return new FashionService(reg);
        }
    }
}
