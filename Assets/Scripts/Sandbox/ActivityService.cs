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
        private IActivityServiceHost _host;

        public event Action<int> OnActivityStarted; // (activityId)
        public event Action OnActivityCatalogLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public ActivityService() { }
        public ActivityService(IActivityServiceHost host) { _host = host; }
        public ActivityService(PcActivityRegistry registry) { AttachRegistry(registry); }

        public void AttachHost(IActivityServiceHost host) { _host = host; }

        public void AttachRegistry(PcActivityRegistry registry)
        {
            _registry = registry ?? new PcActivityRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} hoạt động");
            OnActivityCatalogLoaded?.Invoke();
            if (_host != null)
            {
                _host.OnActivityRegistryAttached(_registry.Count);
                _host.LogActivityEvent("load", 0, $"Loaded {_registry.Count} activities");
                _host.PlayActivitySFX("load", 0);
            }
        }

        public PcActivityEntry GetActivity(int activityId)
        {
            var e = _registry != null ? _registry.Get(activityId) : null;
            if (_host != null)
            {
                if (e != null)
                    _host.OnActivityResolved(e.activityId, e.nameRaw, e.type, e.openHour, e.closeHour);
                else
                    _host.LogActivityEvent("query_missing", activityId, "Activity not found in registry");
            }
            return e;
        }

        public IReadOnlyList<PcActivityEntry> GetByType(int type)
        {
            var list = _registry != null
                ? _registry.GetByType(type)
                : (IReadOnlyList<PcActivityEntry>)Array.Empty<PcActivityEntry>();
            if (_host != null)
                _host.OnActivitiesByTypeQueried(type, list.Count, TypeNameVi(type));
            return list;
        }

        public IReadOnlyList<PcActivityEntry> GetActiveAtHour(int hour)
        {
            var list = _registry != null
                ? _registry.GetActiveByHour(hour)
                : (IReadOnlyList<PcActivityEntry>)Array.Empty<PcActivityEntry>();
            if (_host != null)
                _host.OnActivitiesAtHourQueried(hour, list.Count);
            return list;
        }

        public IEnumerable<PcActivityEntry> GetAllActivities()
        {
            var list = _registry != null ? _registry.All : (IEnumerable<PcActivityEntry>)Array.Empty<PcActivityEntry>();
            int n = 0;
            foreach (var _ in list) n++;
            if (_host != null) _host.OnAllActivitiesQueried(n);
            return list;
        }

        public void StartActivity(int activityId)
        {
            if (_registry == null) return;
            var e = _registry.Get(activityId);
            if (e == null)
            {
                SubsystemLog.Warn(LogTag, $"Hoạt động {activityId} không tồn tại");
                if (_host != null)
                {
                    _host.OnActivityStartDispatched(activityId, false, "Activity not found in registry");
                    _host.LogActivityEvent("start_missing", activityId, "Activity not found");
                }
                return;
            }
            SubsystemLog.Info(LogTag, $"Bắt đầu hoạt động #{activityId} ({e.nameRaw})");
            OnActivityStarted?.Invoke(activityId);
            if (_host != null)
            {
                _host.OnActivityStartDispatched(activityId, true, $"Started {e.nameRaw}");
                _host.ShowActivityUI(e.activityId, e.nameRaw, e.type);
                _host.LogActivityEvent("start", e.activityId, $"Started {e.nameRaw}");
                _host.PlayActivitySFX("start", e.activityId);
                _host.SaveActivityState(e.activityId, e.type, e.openHour);
            }
        }

        public static string TypeNameVi(int type) => type switch
        {
            0 => "Hằng Ngày",
            1 => "Hằng Tuần",
            2 => "Hằng Tháng",
            _ => $"Khác ({type})",
        };

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
