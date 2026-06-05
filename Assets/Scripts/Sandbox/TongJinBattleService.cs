// -----------------------------------------------------------------------------
// VLTK Mobile — ST-08.9 Tống Kim Battle Service (chiến trường quốc chiến runtime)
// Wraps PcTongJinBattleRegistry. PC source: settings/battle/tongjinbattle.txt.
// Vietnamese: "Tống Kim", "Binh đoàn Tống", "Binh đoàn Kim", "Điểm", "Thắng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TongJinBattleService
    {
        public const string LogTag = "TongJinBattle";
        public const string DefaultStreamingDir = "Reference/PcBattlefield";

        private PcTongJinBattleRegistry _registry;

        public event Action OnBattleLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public TongJinBattleService() { }
        public TongJinBattleService(PcTongJinBattleRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcTongJinBattleRegistry registry)
        {
            _registry = registry ?? new PcTongJinBattleRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} trận Tống Kim");
            OnBattleLoaded?.Invoke();
        }

        public PcTongJinBattleEntry GetBattle(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IReadOnlyList<PcTongJinBattleEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcTongJinBattleEntry>();

        public IReadOnlyList<PcTongJinBattleEntry> GetForLevel(int level)
            => _registry != null ? _registry.GetByLevel(level) : Array.Empty<PcTongJinBattleEntry>();

        /// <summary>Tính điểm chênh lệch (dương = Tống thắng).</summary>
        public int ComputeScore(int battleId, int songKills, int jinKills)
            => songKills - jinKills;

        /// <summary>Xác định bên thắng. 1=宋, 2=金, 0=hòa.</summary>
        public int GetWinner(int battleId, int songScore, int jinScore)
        {
            if (songScore > jinScore) return 1;
            if (jinScore > songScore) return 2;
            return 0;
        }

        public static TongJinBattleService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TongJinBattleService();
            if (Directory.Exists(dir))
            {
                var reg = PcTongJinBattleParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Tống Kim: directory không tồn tại {dir}");
                svc.OnBattleLoaded?.Invoke();
            }
            return svc;
        }
    }
}
