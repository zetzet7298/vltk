// -----------------------------------------------------------------------------
// VLTK Mobile — PC reputation.txt reputation system parser
// Source: server settings/reputation.txt (Reference/PcReputation).
// Cols: ReputationId, Name, FactionId, RequiredLevel, RequiredContribution,
//       RewardsJson, Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcReputationParser
    {
        public const int ReputationIdCol = 0;
        public const int NameCol = 1;
        public const int FactionIdCol = 2;
        public const int RequiredLevelCol = 3;
        public const int RequiredContributionCol = 4;
        public const int RewardsJsonCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcReputationEntry> ParseFile(string path)
        {
            var rows = new List<PcReputationEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, ReputationIdCol);
                if (id <= 0) continue;
                rows.Add(new PcReputationEntry
                {
                    reputationId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredContribution = PcItemCommon.Int(cols, RequiredContributionCol),
                    rewardsJson = PcItemCommon.Str(cols, RewardsJsonCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcReputationRegistry BuildRegistry(string dir)
        {
            var reg = new PcReputationRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcReputationEntry
    {
        public int reputationId;
        public string name;
        public int factionId;
        public int requiredLevel;
        public int requiredContribution;
        public string rewardsJson;
        public string description;
    }

    public sealed class PcReputationRegistry
    {
        private readonly Dictionary<int, PcReputationEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcReputationEntry e) { if (e == null || e.reputationId <= 0) return; _byId[e.reputationId] = e; }
        public PcReputationEntry Get(int reputationId) => _byId.TryGetValue(reputationId, out var v) ? v : null;
        public IReadOnlyList<PcReputationEntry> GetByFaction(int factionId)
        {
            var list = new List<PcReputationEntry>();
            foreach (var e in _byId.Values)
                if (e.factionId == factionId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcReputationEntry> All => new List<PcReputationEntry>(_byId.Values);
    }
}
