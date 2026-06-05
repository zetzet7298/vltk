// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/trip_config.ini Trip/Travel parser
// Hành trình du lịch: từ map A → map B trong DurationSec, thưởng exp/silver.
// Source: settings/trip_config.ini (INI sections) or trip.txt (7 tab cols).
//   [Trip1]   StartMap  EndMap  DurationSec  RewardExp  RewardSilver  RequiredItem
// Tab format: TripId  StartMapId  EndMapId  DurationSec  RewardExp  RewardSilver  RequiredItem
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcTripParser
    {
        public const int TripIdCol = 0;
        public const int StartMapIdCol = 1;
        public const int EndMapIdCol = 2;
        public const int DurationSecCol = 3;
        public const int RewardExpCol = 4;
        public const int RewardSilverCol = 5;
        public const int RequiredItemCol = 6;

        public static List<PcTripEntry> ParseFile(string path)
        {
            var rows = new List<PcTripEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]")) continue; // INI section
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcTripEntry
                {
                    tripId = PcItemCommon.Int(cols, TripIdCol),
                    startMapId = PcItemCommon.Int(cols, StartMapIdCol),
                    endMapId = PcItemCommon.Int(cols, EndMapIdCol),
                    durationSec = PcItemCommon.Int(cols, DurationSecCol),
                    rewardExp = cols.Length > RewardExpCol ? PcItemCommon.Int(cols, RewardExpCol) : 0,
                    rewardSilver = cols.Length > RewardSilverCol ? PcItemCommon.Int(cols, RewardSilverCol) : 0,
                    requiredItem = cols.Length > RequiredItemCol ? PcItemCommon.Int(cols, RequiredItemCol) : 0,
                });
            }
            return rows;
        }

        public static PcTripRegistry BuildRegistry(string dir)
        {
            var reg = new PcTripRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            foreach (var f in Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcTripEntry
    {
        public int tripId;
        public int startMapId;
        public int endMapId;
        public int durationSec;
        public int rewardExp;
        public int rewardSilver;
        public int requiredItem;
    }

    public sealed class PcTripRegistry
    {
        private readonly Dictionary<int, PcTripEntry> _byId = new();
        private readonly Dictionary<int, List<PcTripEntry>> _byStart = new();
        private readonly List<PcTripEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcTripEntry> All => _all;

        public void Register(PcTripEntry e)
        {
            if (e == null || e.tripId <= 0) return;
            _byId[e.tripId] = e;
            _all.Add(e);
            if (!_byStart.TryGetValue(e.startMapId, out var list))
            {
                list = new List<PcTripEntry>();
                _byStart[e.startMapId] = list;
            }
            list.Add(e);
        }

        public PcTripEntry Get(int tripId)
            => _byId.TryGetValue(tripId, out var v) ? v : null;

        public IReadOnlyList<PcTripEntry> GetFromMap(int startMapId)
            => _byStart.TryGetValue(startMapId, out var v) ? v : (IReadOnlyList<PcTripEntry>)System.Array.Empty<PcTripEntry>();
    }
}
