// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2.x Faction Skill Tree Service
// Quản lý cây kỹ năng riêng từng môn phái. Reference: faction_skilltree.txt.
// Vietnamese: "Cây Kỹ Năng", "Học Kỹ Năng", "Đã Học", "Tầng Kế Tiếp".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Kết quả TryLearn: 0=OK, 1=fail.
    /// </summary>
    public static class FactionLearnResult
    {
        public const int Ok = 0;
        public const int Fail = 1;
    }

    /// <summary>
    /// Service quản lý cây kỹ năng môn phái.
    /// </summary>
    public class FactionSkillTreeService
    {
        public const string LogTag = "FactionSkillTree";
        public const string DefaultStreamingDir = "Reference/PcFaction";

        private PcFactionSkillTreeRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public FactionSkillTreeService() { }
        public FactionSkillTreeService(PcFactionSkillTreeRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcFactionSkillTreeRegistry reg)
        {
            _registry = reg ?? new PcFactionSkillTreeRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Cây kỹ năng rỗng");
        }

        public static FactionSkillTreeService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new FactionSkillTreeService();
            var reg = PcFactionSkillTreeParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} mục cây kỹ năng");
            return svc;
        }

        public PcFactionSkillTreeEntry GetSkillForFaction(int factionId, int skillId)
            => _registry != null ? _registry.Get(factionId, skillId) : null;

        public IReadOnlyList<PcFactionSkillTreeEntry> GetByFaction(int factionId)
            => _registry != null ? _registry.GetByFaction(factionId) : Array.Empty<PcFactionSkillTreeEntry>();

        public IReadOnlyList<PcFactionSkillTreeEntry> GetByTier(int factionId, int tier)
            => _registry != null ? _registry.GetByTier(factionId, tier) : Array.Empty<PcFactionSkillTreeEntry>();

        /// <summary>Có thể học kỹ năng này không?</summary>
        public bool CanLearn(int factionId, int skillId, int playerLevel, HashSet<int> knownSkills)
        {
            if (knownSkills == null) knownSkills = new HashSet<int>();
            var entry = GetSkillForFaction(factionId, skillId);
            if (entry == null) return false;
            if (entry.requiredLevel > 0 && playerLevel < entry.requiredLevel) return false;
            if (entry.requiredPrevSkill > 0 && !knownSkills.Contains(entry.requiredPrevSkill)) return false;
            return true;
        }

        /// <summary>Thử học kỹ năng — trả về 0=OK, 1=fail.</summary>
        public int TryLearn(int factionId, int skillId, int playerLevel, HashSet<int> knownSkills)
        {
            if (knownSkills == null) knownSkills = new HashSet<int>();
            if (knownSkills.Contains(skillId)) return FactionLearnResult.Fail; // đã học
            if (!CanLearn(factionId, skillId, playerLevel, knownSkills)) return FactionLearnResult.Fail;
            knownSkills.Add(skillId);
            SubsystemLog.Info(LogTag, $"Học skill {skillId} cho phái {factionId} (cấp NV {playerLevel})");
            return FactionLearnResult.Ok;
        }

        public string GetFactionName(int factionId)
            => FactionVietnameseCatalog.GetVietnameseName(factionId) ?? $"Phái {factionId}";

        public IReadOnlyList<int> GetTiersForFaction(int factionId)
        {
            var tiers = new SortedSet<int>();
            foreach (var e in GetByFaction(factionId)) tiers.Add(e.tier);
            return new List<int>(tiers);
        }
    }
}
