// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.2 Huo Yeu Du Service (Điểm hoạt động runtime)
// Wraps PcHuoYueDuRegistry. PC source: settings/huoyuedu/huoyuedu.txt (41 entries).
// Vietnamese: "Điểm Hoạt Động", "BOSS", "Thủy Phong Lăng Độ", "Tống Kim", "Công Thành".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class HuoYueDuService
    {
        public const string LogTag = "HuoYueDu";

        private PcHuoYueDuRegistry _registry;

        public event Action OnActivitiesLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public HuoYueDuService() { }
        public HuoYueDuService(PcHuoYueDuRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcHuoYueDuRegistry registry)
        {
            _registry = registry ?? new PcHuoYueDuRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} hoạt động điểm");
            OnActivitiesLoaded?.Invoke();
        }

        public PcHuoYueDuEntry GetActivity(int activityId)
            => _registry != null ? _registry.Get(activityId) : null;

        public IReadOnlyList<PcHuoYueDuEntry> GetByType(int type)
            => _registry != null
                ? _registry.GetByType(type)
                : (IReadOnlyList<PcHuoYueDuEntry>)Array.Empty<PcHuoYueDuEntry>();

        public IEnumerable<PcHuoYueDuEntry> GetAllActivities()
            => _registry != null ? _registry.All : (IEnumerable<PcHuoYueDuEntry>)Array.Empty<PcHuoYueDuEntry>();

        public static HuoYueDuService LoadFromStreamingAssets(string subdir = "Reference/PcEvent")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new HuoYueDuService();
            if (Directory.Exists(dir))
            {
                var reg = PcHuoYueDuParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"HuoYueDu: directory không tồn tại {dir}");
                svc.OnActivitiesLoaded?.Invoke();
            }
            return svc;
        }
    }
}
