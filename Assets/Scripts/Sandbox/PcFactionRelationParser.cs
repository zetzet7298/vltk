// -----------------------------------------------------------------------------
// VLTK Mobile — PC faction relation parser (Faction Relations)
// Source: faction_relation.txt (Reference/PcFaction).
// Columns: FactionId  AlliedFactionId  EnemyFactionId  NeutralFactionId  Alignment
// Vietnamese: "Đồng Minh", "Thù Địch", "Trung Lập", "Chính - Tà - Trung".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionRelationParser
    {
        public const int FactionIdCol = 0;
        public const int AlliedFactionIdCol = 1;
        public const int EnemyFactionIdCol = 2;
        public const int NeutralFactionIdCol = 3;
        public const int AlignmentCol = 4;

        public static List<PcFactionRelationEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionRelationEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                int factionId = PcItemCommon.Int(cols, FactionIdCol);
                if (factionId < 0) continue;
                rows.Add(new PcFactionRelationEntry
                {
                    factionId = factionId,
                    alliedFactionId = PcItemCommon.Int(cols, AlliedFactionIdCol),
                    enemyFactionId = PcItemCommon.Int(cols, EnemyFactionIdCol),
                    neutralFactionId = PcItemCommon.Int(cols, NeutralFactionIdCol),
                    alignment = PcItemCommon.Int(cols, AlignmentCol),
                });
            }
            return rows;
        }

        public static PcFactionRelationRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionRelationRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFactionRelationEntry
    {
        public int factionId;
        public int alliedFactionId;
        public int enemyFactionId;
        public int neutralFactionId;
        public int alignment; // 0=Chính, 1=Tà, 2=Trung Lập
    }

    public sealed class PcFactionRelationRegistry
    {
        private readonly Dictionary<int, PcFactionRelationEntry> _byFactionId = new();
        public int Count => _byFactionId.Count;

        public void Register(PcFactionRelationEntry e)
        {
            if (e == null || e.factionId < 0) return;
            _byFactionId[e.factionId] = e;
        }

        public PcFactionRelationEntry Get(int factionId)
            => _byFactionId.TryGetValue(factionId, out var v) ? v : null;

        public IReadOnlyList<PcFactionRelationEntry> All => new List<PcFactionRelationEntry>(_byFactionId.Values);
    }
}
