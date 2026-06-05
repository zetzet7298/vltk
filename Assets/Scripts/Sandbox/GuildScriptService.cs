// -----------------------------------------------------------------------------
// VLTK Mobile — ST-9.1 Guild Script Service
// Quản lý kịch bản bang hội. Reference: guildscript.txt.
// Vietnamese: "Kịch Bản Bang Hội", "Tạo Bang", "Gia Nhập", "Đóng Góp", "Công Thành".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Loại kịch bản bang.
    /// </summary>
    public static class GuildScriptType
    {
        public const int Create = 0;
        public const int Join = 1;
        public const int Leave = 2;
        public const int Donate = 3;
        public const int Build = 4;
        public const int War = 5;
        public const int Disband = 6;

        public static string GetName(int type)
        {
            switch (type)
            {
                case Create: return "Tạo Bang";
                case Join: return "Gia Nhập";
                case Leave: return "Rời Bang";
                case Donate: return "Đóng Góp";
                case Build: return "Xây Công Trình";
                case War: return "Công Thành";
                case Disband: return "Giải Tán";
                default: return "Khác";
            }
        }
    }

    /// <summary>
    /// Context truyền vào khi execute guild script.
    /// </summary>
    public class GuildContext
    {
        public int playerId;
        public int guildId;
        public int playerLevel;
        public int guildLevel;
        public int guildFunds;
        public long timestamp;
    }

    /// <summary>
    /// Service quản lý kịch bản bang hội.
    /// </summary>
    public class GuildScriptService
    {
        public const string LogTag = "GuildScript";
        public const string DefaultStreamingDir = "Reference/PcTong";

        private PcGuildScriptRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public GuildScriptService() { }
        public GuildScriptService(PcGuildScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcGuildScriptRegistry reg)
        {
            _registry = reg ?? new PcGuildScriptRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Kịch bản bang rỗng");
        }

        public static GuildScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new GuildScriptService();
            var reg = PcGuildScriptParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} kịch bản bang");
            return svc;
        }

        public PcGuildScriptEntry GetScript(int scriptId)
            => _registry != null ? _registry.Get(scriptId) : null;

        public IReadOnlyList<PcGuildScriptEntry> GetByType(int type)
            => _registry != null ? _registry.GetByType(type) : Array.Empty<PcGuildScriptEntry>();

        /// <summary>Có thể thực thi kịch bản này không (theo cấp NV).</summary>
        public bool CanExecute(int scriptId, int playerLevel)
        {
            var entry = GetScript(scriptId);
            if (entry == null) return false;
            if (entry.requiredLevel > 0 && playerLevel < entry.requiredLevel) return false;
            return true;
        }

        /// <summary>Thực thi kịch bản — trả về 0=OK, 1=fail.</summary>
        public int ExecuteScript(int scriptId, GuildContext ctx)
        {
            if (ctx == null) return 1;
            var entry = GetScript(scriptId);
            if (entry == null) return 1;
            if (!CanExecute(scriptId, ctx.playerLevel)) return 1;
            SubsystemLog.Info(LogTag, $"Thực thi kịch bản bang #{scriptId} ({entry.name}) cho NV {ctx.playerId}");
            return 0;
        }

        public string GetScriptTypeName(int type) => GuildScriptType.GetName(type);
    }
}
