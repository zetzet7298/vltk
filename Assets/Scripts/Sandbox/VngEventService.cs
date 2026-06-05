// -----------------------------------------------------------------------------
// VLTK Mobile — VNG Event Service (Sự Kiện VNG runtime)
// Wraps PcVngEventRegistry. Lọc theo cấp VIP, cấp nhân vật, kiểm tra điều kiện.
// Vietnamese: "Sự Kiện VNG", "Cấp Yêu Cầu", "VIP", "Phần Thưởng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý sự kiện VNG (195 sự kiện VNG Corp). PC source: settings/vng_events.txt.
    /// Hỗ trợ lọc theo VIP, cấp nhân vật, và kiểm tra điều kiện tham gia.
    /// </summary>
    public class VngEventService
    {
        public const string LogTag = "VngEvent";

        private PcVngEventRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public VngEventService() { }
        public VngEventService(PcVngEventRegistry registry)
        {
            _registry = registry ?? new PcVngEventRegistry();
        }

        public void AttachRegistry(PcVngEventRegistry registry)
        {
            _registry = registry ?? new PcVngEventRegistry();
        }

        public PcVngEventEntry GetEvent(int eventId)
            => _registry != null ? _registry.Get(eventId) : null;

        public IEnumerable<PcVngEventEntry> GetAllEvents()
            => _registry != null ? _registry.All : (IEnumerable<PcVngEventEntry>)System.Array.Empty<PcVngEventEntry>();

        /// <summary>Lọc sự kiện VNG theo cấp VIP (≤ vipLevel).</summary>
        public IEnumerable<PcVngEventEntry> GetEventsForVip(int vipLevel)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetByVip(vipLevel)) yield return e;
        }

        /// <summary>Lọc sự kiện VNG theo cấp nhân vật (≤ playerLevel).</summary>
        public IEnumerable<PcVngEventEntry> GetEventsForLevel(int playerLevel)
        {
            if (_registry == null) yield break;
            foreach (var e in _registry.GetByLevel(playerLevel)) yield return e;
        }

        /// <summary>
        /// Kiểm tra nhân vật có đủ điều kiện tham gia sự kiện.
        /// PC dùng cả 2 điều kiện level + VIP (type=3), hoặc 1 trong 2 (type=1 hoặc 2), hoặc open (type=0).
        /// </summary>
        public bool CanParticipate(int eventId, int playerLevel, int vipLevel)
        {
            var e = GetEvent(eventId);
            if (e == null) return false;
            switch (e.type)
            {
                case 0: return true; // open
                case 1: return vipLevel >= e.requiredVip;
                case 2: return playerLevel >= e.requiredLevel;
                case 3: return playerLevel >= e.requiredLevel && vipLevel >= e.requiredVip;
                default: return true;
            }
        }

        public static VngEventService LoadFromStreamingAssets()
        {
            var svc = new VngEventService();
            string[] candidates = { "Reference/PcVngEvent", "Reference/PcEvent/Vng" };
            foreach (var sub in candidates)
            {
                string dir = Path.Combine(Application.streamingAssetsPath, sub);
                if (Directory.Exists(dir))
                {
                    var reg = PcVngEventParser.BuildRegistry(dir);
                    svc.AttachRegistry(reg);
                    SubsystemLog.Info(LogTag, $"VngEventService loaded {reg.Count} sự kiện VNG từ {dir}");
                    return svc;
                }
            }
            SubsystemLog.Warn(LogTag, "VngEventService: không tìm thấy thư mục, khởi tạo registry rỗng");
            return svc;
        }
    }
}
