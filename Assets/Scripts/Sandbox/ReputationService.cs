// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.x Reputation runtime service
// Quản lý danh tiếng (reputation) theo môn phái + tier.
// Vietnamese: "Sơ Cấp", "Trung Cấp", "Cao Cấp", "Đại Sư", "Tông Sư".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ReputationService
    {
        public const string LogTag = "Reputation";

        private readonly PcReputationRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public ReputationService() { }
        public ReputationService(PcReputationRegistry registry) { _registry = registry ?? new PcReputationRegistry(); }

        public static ReputationService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcReputation");
            var reg = PcReputationParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} danh tiếng");
            return new ReputationService(reg);
        }

        public PcReputationEntry GetReputation(int repId) => _registry != null ? _registry.Get(repId) : null;
        public IReadOnlyList<PcReputationEntry> GetByFaction(int factionId)
            => _registry != null ? _registry.GetByFaction(factionId) : Array.Empty<PcReputationEntry>();

        public bool CanEarn(int repId, int playerLevel, int playerContribution)
        {
            var e = GetReputation(repId);
            if (e == null) return false;
            if (playerLevel < e.requiredLevel) return false;
            if (playerContribution < e.requiredContribution) return false;
            return true;
        }

        public string GetTierName(int repId, int contribution)
        {
            if (contribution <= 0) return "Sơ Cấp";
            if (contribution < 100) return "Sơ Cấp";
            if (contribution < 1000) return "Trung Cấp";
            if (contribution < 10000) return "Cao Cấp";
            if (contribution < 100000) return "Đại Sư";
            return "Tông Sư";
        }
    }
}
