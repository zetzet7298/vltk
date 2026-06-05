// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.x World Boss runtime service
// Wraps PcWorldBossRegistry. PC source: settings/boss/worldboss.txt.
// Quản lý danh sách Boss Thế Giới (spawn, DPS score, active).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Boss Thế Giới: lookup theo ID, filter theo map, tính DPS, xác định boss đang hoạt động.
    /// </summary>
    public class WorldBossService
    {
        public const string LogTag = "WorldBoss";
        public const string DefaultStreamingDir = "Reference/PcBoss";

        private PcWorldBossRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public WorldBossService() { }
        public WorldBossService(PcWorldBossRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcWorldBossRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "WorldBoss registry rỗng");
        }

        public PcWorldBossEntry GetWorldBoss(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcWorldBossEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : Array.Empty<PcWorldBossEntry>();

        public IReadOnlyList<PcWorldBossEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcWorldBossEntry>();

        /// <summary>
        /// Tính điểm DPS cho một người chơi dựa trên sát thương + thời gian.
        /// Công thức: (damage * 1000) / timeMs (an toàn khi timeMs = 0).
        /// </summary>
        public int ComputeDpsScore(int bossId, int damage, int timeMs)
        {
            if (damage < 0) damage = 0;
            if (timeMs <= 0) return damage > 0 ? damage * 1000 : 0;
            long score = (long)damage * 1000L / timeMs;
            return score > int.MaxValue ? int.MaxValue : (int)score;
        }

        /// <summary>
        /// Trả về danh sách boss đang trong trạng thái hoạt động (theo chu kỳ respawn).
        /// </summary>
        public IReadOnlyList<PcWorldBossEntry> GetActiveBosses(DateTime now)
        {
            var list = new List<PcWorldBossEntry>();
            if (_reg == null) return list;
            // Bosses with respawn <= 0 are always active; otherwise assume they are
            // active once per respawn window anchored to the day.
            foreach (var e in _reg.All)
            {
                if (e.respawnSec <= 0)
                {
                    list.Add(e);
                }
                else
                {
                    long sec = (long)(now - new DateTime(2000, 1, 1)).TotalSeconds;
                    if (sec % e.respawnSec < (e.respawnSec / 2)) list.Add(e);
                }
            }
            return list;
        }

        public static WorldBossService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcWorldBossParser.BuildRegistry(dir);
            return new WorldBossService(reg);
        }
    }
}
