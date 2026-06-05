// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/missletemplate.txt Missile Effect parser
// Source: settings/missletemplate.txt (480+ entries, GB2312, tab-separated).
//   Cols: MissleId  Name  EffectType  AnimationPath  SoundPath  DurationMs
//         Scale  FollowCaster  IsLooping  ColorR  ColorG  ColorB
// EffectType: 0 = slash, 1 = aoe, 2 = projectile, 3 = buff_aura, 4 = debuff
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMissileEffectParser
    {
        public const int MissleIdCol = 0;
        public const int NameCol = 1;
        public const int EffectTypeCol = 2;
        public const int AnimationPathCol = 3;
        public const int SoundPathCol = 4;
        public const int DurationMsCol = 5;
        public const int ScaleCol = 6;
        public const int FollowCasterCol = 7;
        public const int IsLoopingCol = 8;
        public const int ColorRCol = 9;
        public const int ColorGCol = 10;
        public const int ColorBCol = 11;

        public const int EffectTypeSlash = 0;
        public const int EffectTypeAoe = 1;
        public const int EffectTypeProjectile = 2;
        public const int EffectTypeBuffAura = 3;
        public const int EffectTypeDebuff = 4;

        public static List<PcMissileEffectEntry> ParseFile(string path)
        {
            var rows = new List<PcMissileEffectEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path).ToArray(); }
            catch { try { lines = File.ReadAllLines(path); } catch { return rows; } }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MissleIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMissileEffectEntry
                {
                    missleId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    effectType = PcItemCommon.Int(cols, EffectTypeCol),
                    animationPath = PcItemCommon.Str(cols, AnimationPathCol),
                    soundPath = PcItemCommon.Str(cols, SoundPathCol),
                    durationMs = PcItemCommon.Int(cols, DurationMsCol),
                    scale = PcItemCommon.Int(cols, ScaleCol),
                    followCaster = PcItemCommon.Int(cols, FollowCasterCol) != 0,
                    isLooping = PcItemCommon.Int(cols, IsLoopingCol) != 0,
                    colorR = PcItemCommon.Int(cols, ColorRCol),
                    colorG = PcItemCommon.Int(cols, ColorGCol),
                    colorB = PcItemCommon.Int(cols, ColorBCol),
                });
            }
            return rows;
        }

        public static PcMissileEffectRegistry BuildRegistry(string dir)
        {
            var reg = new PcMissileEffectRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string[] candidates = { "missletemplate.txt", "missile_effect.txt", "missile.txt", "effect.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            // Fallback: quét tất cả *.txt trong thư mục missle
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMissileEffectEntry
    {
        public int missleId;
        public string name;
        public int effectType;        // 0=slash, 1=aoe, 2=projectile, 3=buff_aura, 4=debuff
        public string animationPath;
        public string soundPath;
        public int durationMs;
        public int scale;             // 100 = 1.0x
        public bool followCaster;
        public bool isLooping;
        public int colorR;
        public int colorG;
        public int colorB;
    }

    public sealed class PcMissileEffectRegistry
    {
        private readonly Dictionary<int, PcMissileEffectEntry> _byId = new();
        private readonly Dictionary<int, List<PcMissileEffectEntry>> _byType = new();
        public int Count => _byId.Count;
        public void Register(PcMissileEffectEntry e)
        {
            if (e == null || e.missleId <= 0) return;
            _byId[e.missleId] = e;
            if (!_byType.TryGetValue(e.effectType, out var list))
            {
                list = new List<PcMissileEffectEntry>();
                _byType[e.effectType] = list;
            }
            list.Add(e);
        }
        public PcMissileEffectEntry Get(int missleId)
            => _byId.TryGetValue(missleId, out var v) ? v : null;
        public IReadOnlyList<PcMissileEffectEntry> GetByType(int effectType)
            => _byType.TryGetValue(effectType, out var v)
                ? (IReadOnlyList<PcMissileEffectEntry>)v
                : (IReadOnlyList<PcMissileEffectEntry>)System.Array.Empty<PcMissileEffectEntry>();
        public IReadOnlyList<PcMissileEffectEntry> All
            => new List<PcMissileEffectEntry>(_byId.Values);
    }
}
