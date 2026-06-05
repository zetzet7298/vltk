// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tống Kim battlefield (chiến trường quốc chiến) parser
// Source: settings/maps/battlefield/* (80 battlefields trên PC) +
//         settings/maps/tongkin*.txt (nếu tồn tại).
// File format (GB2312, tab-separated):
//   MapId  Name  MinLevel  MaxLevel  MaxPlayers  TeamCount  DurationSeconds
// Trả về registry runtime tra cứu theo mapId. Vietnamese comments.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattlefieldParser
    {
        public const int NameCol = 0;
        public const int MinLevelCol = 1;
        public const int MaxLevelCol = 2;
        public const int MaxPlayersCol = 3;
        public const int TeamCountCol = 4;
        public const int DurationCol = 5;

        /// <summary>Parse 1 file .txt battlefield. Trả về danh sách entries (rỗng nếu lỗi).</summary>
        public static List<PcBattlefieldEntry> ParseFile(string path)
        {
            var rows = new List<PcBattlefieldEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                autoId++;
                int mapId = autoId;
                // Nếu cột 0 là số, dùng nó làm mapId; ngược lại autoId
                if (int.TryParse(cols[0].Trim(), out int parsed) && parsed > 0) mapId = parsed;
                rows.Add(new PcBattlefieldEntry
                {
                    mapId = mapId,
                    nameVi = PcItemCommon.Str(cols, NameCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol, 1),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol, 200),
                    maxPlayers = PcItemCommon.Int(cols, MaxPlayersCol, 100),
                    teamCount = PcItemCommon.Int(cols, TeamCountCol, 2),
                    duration = PcItemCommon.Int(cols, DurationCol, 1800),
                });
            }
            return rows;
        }

        /// <summary>
        /// Duyệt toàn bộ file .txt trong thư mục (kể cả subdir battlefield/) và xây registry.
        /// Trả về registry rỗng nếu thư mục không tồn tại.
        /// </summary>
        public static PcBattlefieldRegistry BuildRegistry(string rootDir)
        {
            var reg = new PcBattlefieldRegistry();
            if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return reg;

            // Quét thư mục battlefield/ nếu có
            string battlefieldDir = Path.Combine(rootDir, "battlefield");
            if (Directory.Exists(battlefieldDir))
            {
                foreach (var f in Directory.GetFiles(battlefieldDir, "*.txt", SearchOption.AllDirectories))
                    foreach (var e in ParseFile(f)) reg.Register(e);
            }

            // Quét các file tongkin*.txt ở root
            foreach (var f in Directory.GetFiles(rootDir, "tongkin*.txt", SearchOption.TopDirectoryOnly))
                foreach (var e in ParseFile(f)) reg.Register(e);

            // Quét tất cả *.txt ở root có chứa "battle" hoặc "tongkin" trong tên
            foreach (var f in Directory.GetFiles(rootDir, "*.txt", SearchOption.TopDirectoryOnly))
            {
                var fname = Path.GetFileName(f).ToLowerInvariant();
                if (fname.Contains("battle") || fname.Contains("tongkin") || fname.StartsWith("tk_"))
                    foreach (var e in ParseFile(f)) reg.Register(e);
            }

            return reg;
        }
    }

    /// <summary>Một bản đồ chiến trường (Tống Kim, Quốc Chiến, ...).</summary>
    [System.Serializable]
    public class PcBattlefieldEntry
    {
        public int mapId;
        public string nameVi;
        public int minLevel;      // Cấp tối thiểu được vào
        public int maxLevel;      // Cấp tối đa được vào
        public int maxPlayers;    // Tổng số người chơi tối đa (2 phe)
        public int teamCount;     // Số phe (thường 2 = Tống/Kim)
        public int duration;      // Thời lượng (giây)
    }

    public sealed class PcBattlefieldRegistry
    {
        private readonly Dictionary<int, PcBattlefieldEntry> _byId = new();
        private readonly List<PcBattlefieldEntry> _all = new();
        public int Count => _byId.Count;

        public void Register(PcBattlefieldEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            if (_byId.ContainsKey(e.mapId)) return; // dedup
            _byId[e.mapId] = e;
            _all.Add(e);
        }

        public PcBattlefieldEntry Get(int mapId)
            => _byId.TryGetValue(mapId, out var v) ? v : null;

        public IReadOnlyList<PcBattlefieldEntry> GetAll() => _all;
    }
}
