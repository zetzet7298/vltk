// -----------------------------------------------------------------------------
// VLTK Mobile — ST Killer (PK Rules) runtime service
// Source: PC settings/killer.ini.
// Quản lý quy tắc PK theo map (disable / normal / full / faction_only).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý quy tắc PK (Killer) theo map.
    /// PkType: 0=disable, 1=normal, 2=full, 3=faction_only.
    /// </summary>
    public class KillerService
    {
        private PcKillerRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public KillerService() { }
        public KillerService(PcKillerRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcKillerRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn("Killer", "Killer registry rỗng");
        }

        public static KillerService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference");
            var reg = PcKillerParser.BuildRegistry(root);
            return new KillerService(reg);
        }

        public PcKillerEntry GetRule(int ruleId) => _reg != null ? _reg.Get(ruleId) : null;
        public IReadOnlyList<PcKillerEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcKillerEntry>();
        public IReadOnlyList<PcKillerEntry> All
            => _reg != null ? _reg.All : System.Array.Empty<PcKillerEntry>();

        /// <summary>Cho phép PK tại map không? Map không có rule hoặc pkType=0 → false.</summary>
        public bool CanPk(int mapId)
        {
            var rules = GetByMap(mapId);
            if (rules == null || rules.Count == 0) return false;
            foreach (var r in rules)
            {
                if (r.pkType == 1 || r.pkType == 2 || r.pkType == 3) return true;
            }
            return false;
        }
    }
}
