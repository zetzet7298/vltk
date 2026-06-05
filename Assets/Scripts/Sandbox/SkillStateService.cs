// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3 Skill State Service
// Source: PC settings/skillstate.txt. Trạng thái kỹ năng (DOT/HOT/CC/stacks).
// Vietnamese: "Trạng Thái", "Tăng Cường", "Giảm Sút", "Choáng", "Làm Chậm",
//             "Chảy Máu", "Cháy", "Đóng Băng", "Độc", "Cộng Dồn".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SkillStateService
    {
        public const string LogTag = "SkillState";
        public const string DefaultStreamingDir = "Reference/PcSkill";

        private PcSkillStateRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SkillStateService() { }
        public SkillStateService(PcSkillStateRegistry reg) { AttachRegistry(reg); }

        public void AttachRegistry(PcSkillStateRegistry reg)
        {
            _registry = reg ?? new PcSkillStateRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} trạng thái kỹ năng");
        }

        public PcSkillStateEntry GetState(int stateId)
            => _registry != null ? _registry.Get(stateId) : null;

        public IReadOnlyList<PcSkillStateEntry> GetByType(int type)
            => _registry != null ? _registry.GetByType(type) : System.Array.Empty<PcSkillStateEntry>();

        public static string GetStateTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Tăng cường";
                case 1: return "Giảm sút";
                case 2: return "Choáng";
                case 3: return "Làm chậm";
                case 4: return "Chảy máu";
                case 5: return "Cháy";
                case 6: return "Đóng băng";
                case 7: return "Độc";
                default: return "Không rõ";
            }
        }

        /// <summary>
        /// Tính sát thương mỗi tick dựa trên số cộng dồn hiện tại.
        /// </summary>
        public int ComputeTickDamage(int stateId, int stack)
        {
            var e = GetState(stateId);
            if (e == null) return 0;
            if (stack <= 0) stack = 1;
            if (stack > e.stackMax) stack = e.stackMax;
            return e.effectValue * stack;
        }

        public bool CanStack(int stateId, int currentStacks)
        {
            var e = GetState(stateId);
            if (e == null) return false;
            if (e.stackMax <= 0) return false; // no stack
            return currentStacks < e.stackMax;
        }

        public static SkillStateService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new SkillStateService();
            if (Directory.Exists(dir))
            {
                var reg = PcSkillStateParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại: {dir}");
            }
            return svc;
        }
    }
}
