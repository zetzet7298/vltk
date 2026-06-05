// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.8 Tong Battle Script Service
// Quản lý script công thành chiến bang hội: attack, defend, gate, respawn, score.
// ScriptType: 0=attack, 1=defend, 2=gate, 3=respawn, 4=score.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý script công thành chiến bang hội.</summary>
    public class TongBattleScriptService
    {
        public const string LogTag = "TongBattleScript";
        public const string DefaultStreamingDir = "Reference/PcTong";

        private PcTongBattleScriptRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public TongBattleScriptService() { }
        public TongBattleScriptService(PcTongBattleScriptRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcTongBattleScriptRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Tong battle script registry rỗng");
        }

        public static TongBattleScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TongBattleScriptService();
            if (Directory.Exists(dir))
            {
                var reg = PcTongBattleScriptParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Tong battle script directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcTongBattleScriptEntry GetScript(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcTongBattleScriptEntry> GetByType(int scriptType)
            => _reg != null ? _reg.GetByType(scriptType) : System.Array.Empty<PcTongBattleScriptEntry>();
        public IReadOnlyList<PcTongBattleScriptEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcTongBattleScriptEntry>();

        public string GetScriptTypeName(int scriptType)
        {
            return scriptType switch
            {
                0 => "Tấn Công",
                1 => "Phòng Thủ",
                2 => "Cổng Thành",
                3 => "Hồi Sinh",
                4 => "Điểm Số",
                _ => $"Khác ({scriptType})",
            };
        }

        public string GetFunctionName(int scriptId)
        {
            var e = GetScript(scriptId);
            return e != null ? e.functionName : null;
        }
    }
}
