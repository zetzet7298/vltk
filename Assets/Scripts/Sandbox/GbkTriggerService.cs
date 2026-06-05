// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.x GBK Trigger Service
// Quản lý GBK trigger. EventType: 0=player_enter, 2=npc_kill, 3=item_use, 4=time, 5=death.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý GBK trigger (sự kiện kích hoạt khi vào map, giết NPC, ...).</summary>
    public class GbkTriggerService
    {
        public const string LogTag = "GbkTrigger";
        public const string DefaultStreamingDir = "Reference/PcGbk";

        private PcGbkTriggerRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public GbkTriggerService() { }
        public GbkTriggerService(PcGbkTriggerRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcGbkTriggerRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "GBK trigger registry rỗng");
        }

        public static GbkTriggerService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new GbkTriggerService();
            if (Directory.Exists(dir))
            {
                var reg = PcGbkTriggerParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"GBK trigger directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcGbkTriggerEntry GetTrigger(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcGbkTriggerEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcGbkTriggerEntry>();
        public IReadOnlyList<PcGbkTriggerEntry> GetByEvent(int eventType)
            => _reg != null ? _reg.GetByEvent(eventType) : System.Array.Empty<PcGbkTriggerEntry>();
        public IReadOnlyList<PcGbkTriggerEntry> GetTriggersForMap(int mapId) => GetByMap(mapId);

        public string GetEventTypeName(int eventType)
        {
            return eventType switch
            {
                0 => "Player Vào Map",
                2 => "Giết NPC",
                3 => "Dùng Vật Phẩm",
                4 => "Thời Gian",
                5 => "Chết",
                _ => $"Khác ({eventType})",
            };
        }

        /// <summary>Kiểm tra trigger có thể kích hoạt không (basic level/time check).</summary>
        public bool CanFire(int triggerId, int playerLevel, int time)
        {
            var t = GetTrigger(triggerId);
            if (t == null) return false;
            if (playerLevel <= 0) return false;
            if (time < 0) return false;
            // Time-based trigger chỉ fire trong khoảng 0-23
            if (t.eventType == 4 && (time < 0 || time > 23)) return false;
            return true;
        }
    }
}
