// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2.x Faction Bonus Service
// Quản lý bonus môn phái theo cấp. Reference: faction_bonus.txt.
// Vietnamese: "Bonus Môn Phái", "Cấp Thưởng", "Tăng Máu", "Tăng Công".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý bonus môn phái theo cấp.
    /// </summary>
    public class FactionBonusService
    {
        public const string LogTag = "FactionBonus";
        public const string DefaultStreamingDir = "Reference/PcFaction";

        private PcFactionBonusRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public FactionBonusService() { }
        public FactionBonusService(PcFactionBonusRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcFactionBonusRegistry reg)
        {
            _registry = reg ?? new PcFactionBonusRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Bonus môn phái rỗng");
        }

        public static FactionBonusService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new FactionBonusService();
            var reg = PcFactionBonusParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} mục bonus phái");
            return svc;
        }

        public PcFactionBonusEntry GetBonusForFaction(int factionId, int level)
            => _registry != null ? _registry.Get(factionId, level) : null;

        public IReadOnlyList<PcFactionBonusEntry> GetByFaction(int factionId)
            => _registry != null ? _registry.GetByFaction(factionId) : Array.Empty<PcFactionBonusEntry>();

        /// <summary>Tính tổng HP bonus cho tới cấp hiện tại.</summary>
        public int ComputeHpBonus(int factionId, int level)
        {
            if (_registry == null || level <= 0) return 0;
            int total = 0;
            foreach (var e in _registry.GetByFaction(factionId))
            {
                if (e.level <= level) total += e.hpBonus;
            }
            return total;
        }

        /// <summary>Tính tổng ATK bonus cho tới cấp hiện tại.</summary>
        public int ComputeAtkBonus(int factionId, int level)
        {
            if (_registry == null || level <= 0) return 0;
            int total = 0;
            foreach (var e in _registry.GetByFaction(factionId))
            {
                if (e.level <= level) total += e.atkBonus;
            }
            return total;
        }

        public int ComputeMpBonus(int factionId, int level)
        {
            if (_registry == null || level <= 0) return 0;
            int total = 0;
            foreach (var e in _registry.GetByFaction(factionId))
            {
                if (e.level <= level) total += e.mpBonus;
            }
            return total;
        }

        public int ComputeDefBonus(int factionId, int level)
        {
            if (_registry == null || level <= 0) return 0;
            int total = 0;
            foreach (var e in _registry.GetByFaction(factionId))
            {
                if (e.level <= level) total += e.defBonus;
            }
            return total;
        }
    }
}
