// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.1 Activity Service (Hệ thống hoạt động runtime)
// Wraps PcActivityRegistry. PC source: settings/activitysys/activity.txt (21 entries).
// Vietnamese: "Hoạt Động", "Hằng Ngày", "Hằng Tuần", "Hằng Tháng", "Đang Mở".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ActivityService
    {
        public const string LogTag = "Activity";
        public const string DefaultStreamingDir = "Reference/PcEvent";

        private PcActivityRegistry _registry;

        public event Action<int> OnActivityStarted; // (activityId)
        public event Action OnActivityCatalogLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public ActivityService() { }
        public ActivityService(PcActivityRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcActivityRegistry registry)
        {
            _registry = registry ?? new PcActivityRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} hoạt động");
            OnActivityCatalogLoaded?.Invoke();
        }

        public PcActivityEntry GetActivity(int activityId)
            => _registry != null ? _registry.Get(activityId) : null;

        public IReadOnlyList<PcActivityEntry> GetByType(int type)
            => _registry != null
                ? _registry.GetByType(type)
                : (IReadOnlyList<PcActivityEntry>)Array.Empty<PcActivityEntry>();

        public IReadOnlyList<PcActivityEntry> GetActiveAtHour(int hour)
            => _registry != null
                ? _registry.GetActiveByHour(hour)
                : (IReadOnlyList<PcActivityEntry>)Array.Empty<PcActivityEntry>();

        public IEnumerable<PcActivityEntry> GetAllActivities()
            => _registry != null ? _registry.All : (IEnumerable<PcActivityEntry>)Array.Empty<PcActivityEntry>();

        public void StartActivity(int activityId)
        {
            if (_registry == null) return;
            var e = _registry.Get(activityId);
            if (e == null)
            {
                SubsystemLog.Warn(LogTag, $"Hoạt động {activityId} không tồn tại");
                return;
            }
            SubsystemLog.Info(LogTag, $"Bắt đầu hoạt động #{activityId} ({e.nameRaw})");
            OnActivityStarted?.Invoke(activityId);
        }

        public static ActivityService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new ActivityService();
            if (Directory.Exists(dir))
            {
                var reg = PcActivityParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Activity: directory không tồn tại {dir}");
                svc.OnActivityCatalogLoaded?.Invoke();
            }
            return svc;
        }
    }
}
