// -----------------------------------------------------------------------------
// VLTK Mobile — DoubleExpService (Lịch Nhân Đôi Kinh Nghiệm runtime)
// Wraps PcDoubleExpRegistry. PC source: settings/doubleexp.txt.
// Tra cứu theo giờ/ngày trong tuần, multiplier quy ước 10000 = 1.0x.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý lịch nhân đôi EXP theo khung giờ / ngày trong tuần.
    /// CombatRuntimeService dùng để áp dụng multiplier khi cộng EXP cho nhân vật.
    /// </summary>
    public class DoubleExpService
    {
        public const string LogTag = "DoubleExp";

        public const float DefaultMultiplier = 1.0f;

        private PcDoubleExpRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public DoubleExpService() : this(null) { }

        public DoubleExpService(PcDoubleExpRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterRegistry(PcDoubleExpRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Lịch Nhân Đôi EXP loaded: {Count} schedule");
        }

        public PcDoubleExpEntry GetActiveByHour(int hour, int dayOfWeek)
            => _registry != null ? _registry.GetActiveByHour(hour, dayOfWeek) : null;

        public IEnumerable<PcDoubleExpEntry> GetAllSchedules()
            => _registry != null ? _registry.All : (IEnumerable<PcDoubleExpEntry>)System.Array.Empty<PcDoubleExpEntry>();

        /// <summary>True nếu tại hour/dayOfWeek có schedule đang active.</summary>
        public bool IsDoubleExpActive(int hour, int dayOfWeek)
            => GetActiveByHour(hour, dayOfWeek) != null;

        /// <summary>Multiplier hiện tại (1.0 nếu không có schedule).</summary>
        public float GetCurrentMultiplier(int hour, int dayOfWeek)
        {
            var active = GetActiveByHour(hour, dayOfWeek);
            return active != null ? active.Multiplier : DefaultMultiplier;
        }

        /// <summary>Load từ StreamingAssets/Reference/PcAttrib hoặc Reference.</summary>
        public static DoubleExpService LoadFromStreamingAssets()
        {
            string root = Application.streamingAssetsPath;
            PcDoubleExpRegistry reg = null;
            string[] candidates =
            {
                Path.Combine(root, "Reference/PcAttrib"),
                Path.Combine(root, "Reference"),
            };
            foreach (var dir in candidates)
            {
                if (Directory.Exists(dir))
                {
                    reg = PcDoubleExpParser.BuildRegistry(dir);
                    if (reg != null && reg.Count > 0) break;
                }
            }
            return new DoubleExpService(reg);
        }
    }
}
