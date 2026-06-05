// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/partner/* Partner parser
// Source: partner/character.txt (AI behavior + vision/active radius per char id)
//   Characteristic  VisionRadius  ActiveRadius  ForceSync  AIMaxTime  AIMode
//   AIParam1..8  + more
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcPartnerParser
    {
        public const int CharacteristicCol = 0;
        public const int VisionRadiusCol = 1;
        public const int ActiveRadiusCol = 2;
        public const int ForceSyncCol = 3;
        public const int AIMaxTimeCol = 4;
        public const int AIModeCol = 5;
        public const int AIParam1Col = 6;
        public const int AIParam2Col = 7;

        public static List<PcPartnerEntry> ParseFile(string path)
        {
            var rows = new List<PcPartnerEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                rows.Add(new PcPartnerEntry
                {
                    characteristic = PcItemCommon.Int(cols, CharacteristicCol),
                    visionRadius = PcItemCommon.Int(cols, VisionRadiusCol),
                    activeRadius = PcItemCommon.Int(cols, ActiveRadiusCol),
                    forceSync = PcItemCommon.Int(cols, ForceSyncCol),
                    aiMaxTime = PcItemCommon.Int(cols, AIMaxTimeCol),
                    aiMode = PcItemCommon.Int(cols, AIModeCol),
                    aiParam1 = PcItemCommon.Int(cols, AIParam1Col),
                    aiParam2 = PcItemCommon.Int(cols, AIParam2Col),
                });
            }
            return rows;
        }

        public static PcPartnerRegistry BuildRegistry(string dir)
        {
            var reg = new PcPartnerRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "character.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcPartnerEntry
    {
        public int characteristic;
        public int visionRadius;
        public int activeRadius;
        public int forceSync;
        public int aiMaxTime;
        public int aiMode;
        public int aiParam1;
        public int aiParam2;
    }

    public sealed class PcPartnerRegistry
    {
        private readonly Dictionary<int, PcPartnerEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcPartnerEntry e) { if (e == null || e.characteristic <= 0) return; _byId[e.characteristic] = e; }
        public PcPartnerEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
    }
}
