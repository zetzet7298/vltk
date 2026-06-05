// -----------------------------------------------------------------------------
// VLTK Mobile — Server Event Service (Sự Kiện Máy Chủ runtime)
// Wraps PcServerEventRegistry. Lọc sự kiện đang mở theo ngày, theo bản đồ.
// Vietnamese: "Sự Kiện", "Đang Mở", "Bản Đồ", "Ngày Bắt Đầu", "Ngày Kết Thúc".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý sự kiện máy chủ (455 sự kiện). PC source: settings/events.txt
    /// hoặc script/event/* (đơn giản hóa bằng file index).
    /// </summary>
    public class ServerEventService
    {
        public const string LogTag = "ServerEvent";

        private PcServerEventRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public ServerEventService() { }
        public ServerEventService(PcServerEventRegistry registry)
        {
            _registry = registry ?? new PcServerEventRegistry();
        }

        public void AttachRegistry(PcServerEventRegistry registry)
        {
            _registry = registry ?? new PcServerEventRegistry();
        }

        public PcServerEventEntry GetEvent(int eventId)
            => _registry != null ? _registry.Get(eventId) : null;

        public IEnumerable<PcServerEventEntry> GetAllEvents()
            => _registry != null ? _registry.All : (IEnumerable<PcServerEventEntry>)System.Array.Empty<PcServerEventEntry>();

        /// <summary>Lọc sự kiện đang mở tại currentDate (yyyymmdd).</summary>
        public IEnumerable<PcServerEventEntry> GetActiveEvents(int currentDate)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetActive(currentDate)) yield return e;
        }

        public IEnumerable<PcServerEventEntry> GetEventsByMap(int mapId)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetByMap(mapId)) yield return e;
        }

        /// <summary>Kiểm tra sự kiện có đang mở tại currentDate không.</summary>
        public bool IsActive(int eventId, int currentDate)
        {
            var e = GetEvent(eventId);
            if (e == null) return false;
            if (e.type == 0) return true;
            if (e.startDate <= 0 || e.endDate <= 0) return true;
            return currentDate >= e.startDate && currentDate <= e.endDate;
        }

        public static ServerEventService LoadFromStreamingAssets()
        {
            var svc = new ServerEventService();
            string[] candidates = { "Reference/PcServerEvent", "Reference/PcEvent/Index" };
            foreach (var sub in candidates)
            {
                string dir = Path.Combine(Application.streamingAssetsPath, sub);
                if (Directory.Exists(dir))
                {
                    var reg = PcServerEventParser.BuildRegistry(dir);
                    svc.AttachRegistry(reg);
                    SubsystemLog.Info(LogTag, $"ServerEventService loaded {reg.Count} sự kiện từ {dir}");
                    return svc;
                }
            }
            SubsystemLog.Warn(LogTag, "ServerEventService: không tìm thấy thư mục index, khởi tạo registry rỗng");
            return svc;
        }
    }
}
