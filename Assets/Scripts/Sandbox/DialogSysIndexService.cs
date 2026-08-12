// -----------------------------------------------------------------------------
// VLTK Mobile — PC dailogsys source index service.
// Loads Reference/PcDialogSys/dialogsys_source_index.txt as source evidence only.
// Runtime Lua execution / callback dispatch is intentionally out of scope here.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class DialogSysIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcDialogSys";
        public const string StreamingAssetsRelativePath = "Reference/PcDialogSys/dialogsys_source_index.txt";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser/server1/script/dailogsys";

        private readonly PcDialogSysSourceIndexRegistry _registry;

        public DialogSysIndexService()
            : this(new PcDialogSysSourceIndexRegistry())
        {
        }

        public DialogSysIndexService(PcDialogSysSourceIndexRegistry registry)
        {
            _registry = registry ?? new PcDialogSysSourceIndexRegistry();
        }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int TotalFunctionCount => _registry.TotalFunctionCount;
        public int TotalGlobalSymbolCount => _registry.TotalGlobalSymbolCount;
        public int TotalOptionSurfaceCount => _registry.TotalOptionSurfaceCount;
        public int TotalSaySurfaceCount => _registry.TotalSaySurfaceCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcDialogSysSourceIndexEntry> All => _registry.All;

        public PcDialogSysSourceIndexEntry GetByRelativePath(string relativePath) => _registry.GetByRelativePath(relativePath);
        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetByFunction(string functionName) => _registry.GetByFunction(functionName);
        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetBySurface(string surface) => _registry.GetBySurface(surface);

        public static DialogSysIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static DialogSysIndexService LoadFromDirectory(string dir)
            => new DialogSysIndexService(PcDialogSysSourceIndexParser.BuildRegistry(dir));

        public static DialogSysIndexService LoadFromFile(string path)
        {
            var reg = new PcDialogSysSourceIndexRegistry();
            foreach (var entry in PcDialogSysSourceIndexParser.ParseFile(path)) reg.Register(entry);
            return new DialogSysIndexService(reg);
        }
    }
}
