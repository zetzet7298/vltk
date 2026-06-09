// -----------------------------------------------------------------------------
// VLTK Mobile — PC battle script source catalog service.
// Data-only wrapper for script/battles file paths; it does not execute Lua.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class BattleScriptSourceCatalogService
    {
        public const string LogTag = "BattleScriptSourceCatalog";
        public const string DefaultStreamingDir = "Reference/PcBattleScript";
        public const string PcSourceRelativeRoot = PcBattleScriptSourceParser.PcSourceRelativeRoot;

        private readonly PcBattleScriptSourceCatalog _catalog;

        public int Count => _catalog != null ? _catalog.Count : 0;
        public int ActiveLuaCount => _catalog != null ? _catalog.ActiveLuaCount : 0;
        public int BackupFileCount => _catalog != null ? _catalog.BackupFileCount : 0;
        public int DirectoryCount => _catalog != null ? _catalog.DirectoryCount : 0;
        public IReadOnlyList<PcBattleScriptSourceEntry> All
            => _catalog != null ? _catalog.All : (IReadOnlyList<PcBattleScriptSourceEntry>)System.Array.Empty<PcBattleScriptSourceEntry>();

        public BattleScriptSourceCatalogService(PcBattleScriptSourceCatalog catalog)
        {
            _catalog = catalog ?? new PcBattleScriptSourceCatalog();
        }

        public PcBattleScriptSourceEntry GetByRelativePath(string relativePath) => _catalog.Get(relativePath);
        public IReadOnlyList<PcBattleScriptSourceEntry> GetByDirectory(string directory) => _catalog.GetByDirectory(directory);
        public int GetDirectoryCount(string directory) => _catalog.GetDirectoryCount(directory);
        public int GetActiveLuaDirectoryCount(string directory) => _catalog.GetActiveLuaDirectoryCount(directory);

        public static BattleScriptSourceCatalogService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcBattleScriptSourceParser.BuildCatalog(dir);
            if (catalog.Count > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC battle script source catalog: {catalog.Count} files, {catalog.ActiveLuaCount} active Lua");
            else
                SubsystemLog.Warn(LogTag, $"PC battle script source catalog missing at {dir}");
            return new BattleScriptSourceCatalogService(catalog);
        }
    }

    public sealed class PcBattleScriptSourceCatalog
    {
        private readonly List<PcBattleScriptSourceEntry> _all = new List<PcBattleScriptSourceEntry>();
        private readonly Dictionary<string, PcBattleScriptSourceEntry> _byPath = new Dictionary<string, PcBattleScriptSourceEntry>();
        private readonly Dictionary<string, List<PcBattleScriptSourceEntry>> _byDirectory = new Dictionary<string, List<PcBattleScriptSourceEntry>>();

        public int Count => _all.Count;
        public int ActiveLuaCount { get; private set; }
        public int BackupFileCount { get; private set; }
        public int DirectoryCount => _byDirectory.Count;
        public IReadOnlyList<PcBattleScriptSourceEntry> All => _all;

        public void Register(PcBattleScriptSourceEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.relativePath)) return;
            if (_byPath.ContainsKey(entry.relativePath)) return;

            _all.Add(entry);
            _byPath[entry.relativePath] = entry;
            if (entry.isActiveLua) ActiveLuaCount++;
            if (string.Equals(entry.fileKind, "lua_backup", System.StringComparison.OrdinalIgnoreCase)) BackupFileCount++;

            var dir = string.IsNullOrEmpty(entry.directory) ? "." : entry.directory;
            if (!_byDirectory.TryGetValue(dir, out var list))
            {
                list = new List<PcBattleScriptSourceEntry>();
                _byDirectory[dir] = list;
            }
            list.Add(entry);
        }

        public PcBattleScriptSourceEntry Get(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcBattleScriptSourceEntry> GetByDirectory(string directory)
        {
            var key = string.IsNullOrEmpty(directory) ? "." : directory;
            return _byDirectory.TryGetValue(key, out var list)
                ? (IReadOnlyList<PcBattleScriptSourceEntry>)list
                : System.Array.Empty<PcBattleScriptSourceEntry>();
        }

        public int GetDirectoryCount(string directory)
        {
            var list = GetByDirectory(directory);
            return list.Count;
        }

        public int GetActiveLuaDirectoryCount(string directory)
        {
            int count = 0;
            foreach (var entry in GetByDirectory(directory))
                if (entry.isActiveLua) count++;
            return count;
        }
    }

}
