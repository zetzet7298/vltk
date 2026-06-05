// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Hongbao Service (Hồng Bao runtime)
// Wraps PcHongbaoRegistry. PC source: settings/hongbaosetting.ini (69 entries).
// Hỗ trợ kiểm tra cấp nhân vật có đủ để nhận hồng bao không.
// Vietnamese: "Hồng Bao", "Lì Xì", "Quà Tặng", "Sự Kiện".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Hồng Bao (lì xì / quà tặng sự kiện).
    /// PC source: settings/hongbaosetting.ini.
    /// </summary>
    public class HongbaoService
    {
        public const string LogTag = "Hongbao";

        private PcHongbaoRegistry _registry;

        public event Action<int> OnHongbaoClaimed; // (hongbaoId)

        public int Count => _registry != null ? _registry.Count : 0;

        public HongbaoService() : this(null) { }

        public HongbaoService(PcHongbaoRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcHongbaoRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Hồng Bao loaded: {Count} món quà");
        }

        public PcHongbaoEntry GetHongbao(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IEnumerable<PcHongbaoEntry> GetAllHongbaos()
            => _registry != null ? _registry.GetAll() : (IEnumerable<PcHongbaoEntry>)Array.Empty<PcHongbaoEntry>();

        /// <summary>
        /// Kiểm tra nhân vật có thể nhận hồng bao hay không (theo minLevel/maxLevel).
        /// Nếu minLevel = maxLevel = 0 → luôn nhận được.
        /// </summary>
        public bool CanClaim(int id, int playerLevel)
        {
            var hb = GetHongbao(id);
            if (hb == null) return false;
            if (hb.minLevel <= 0 && hb.maxLevel <= 0) return true;
            if (playerLevel < hb.minLevel) return false;
            if (hb.maxLevel > 0 && playerLevel > hb.maxLevel) return false;
            return true;
        }

        /// <summary>Đánh dấu đã nhận — emit event.</summary>
        public bool Claim(int id, int playerLevel)
        {
            if (!CanClaim(id, playerLevel)) return false;
            SubsystemLog.Info(LogTag, $"Nhận hồng bao #{id}");
            OnHongbaoClaimed?.Invoke(id);
            return true;
        }

        public static HongbaoService LoadFromStreamingAssets(string subdir = "Reference/PcHongbao")
        {
            var svc = new HongbaoService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcHongbaoParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"HongbaoService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
