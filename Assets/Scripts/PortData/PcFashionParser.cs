// -----------------------------------------------------------------------------
// VLTK Mobile — PC fashion.txt parser
// Source: settings/fashion/fashion.txt (Thời Trang).
// Columns: FashionId Name Slot SpritePath RequiredLevel RequiredSex RequiredVipLevel
// Slot: 0=hair, 1=face, 2=body, 3=arm, 4=leg, 5=cape, 6=weapon
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFashionParser
    {
        public const int FashionIdCol = 0;
        public const int NameCol = 1;
        public const int SlotCol = 2;
        public const int SpritePathCol = 3;
        public const int RequiredLevelCol = 4;
        public const int RequiredSexCol = 5;
        public const int RequiredVipLevelCol = 6;

        public const int SlotHair = 0;
        public const int SlotFace = 1;
        public const int SlotBody = 2;
        public const int SlotArm = 3;
        public const int SlotLeg = 4;
        public const int SlotCape = 5;
        public const int SlotWeapon = 6;

        public static List<PcFashionEntry> ParseFile(string path)
        {
            var rows = new List<PcFashionEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, FashionIdCol);
                if (id <= 0) continue;
                rows.Add(new PcFashionEntry
                {
                    fashionId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    slot = PcItemCommon.Int(cols, SlotCol),
                    spritePath = PcItemCommon.Str(cols, SpritePathCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredSex = PcItemCommon.Int(cols, RequiredSexCol),
                    requiredVipLevel = PcItemCommon.Int(cols, RequiredVipLevelCol),
                });
            }
            return rows;
        }

        public static PcFashionRegistry BuildRegistry(string dir)
        {
            var reg = new PcFashionRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("fashion"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFashionEntry
    {
        public int fashionId;
        public string nameRaw;
        public int slot;
        public string spritePath;
        public int requiredLevel;
        public int requiredSex;
        public int requiredVipLevel;
    }

    public sealed class PcFashionRegistry
    {
        private readonly Dictionary<int, PcFashionEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcFashionEntry e) { if (e == null || e.fashionId <= 0) return; _byId[e.fashionId] = e; }
        public PcFashionEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcFashionEntry> GetBySlot(int slot)
        {
            var list = new List<PcFashionEntry>();
            foreach (var e in _byId.Values)
                if (e.slot == slot) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcFashionEntry> GetForSex(int sex)
        {
            var list = new List<PcFashionEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredSex == sex || e.requiredSex == -1) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcFashionEntry> All => new List<PcFashionEntry>(_byId.Values);
    }
}
