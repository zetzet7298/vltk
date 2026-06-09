// -----------------------------------------------------------------------------
// VLTK Mobile — PC skill-goc source index service.
// Catalog only: loads script/skill-goc source evidence from
// skill_goc_source_index.txt and exposes counts/lookups. Runtime Lua execution
// remains out of scope for this port slice.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class SkillGocScriptIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcSkillGocScript";
        public const string StreamingAssetsRelativePath = "Reference/PcSkillGocScript/skill_goc_source_index.txt";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser_bachkim_6.0/server1/script/skill-goc";

        private readonly PcSkillGocScriptIndexRegistry _registry;

        public SkillGocScriptIndexService()
            : this(new PcSkillGocScriptIndexRegistry())
        {
        }

        public SkillGocScriptIndexService(PcSkillGocScriptIndexRegistry registry)
        {
            _registry = registry ?? new PcSkillGocScriptIndexRegistry();
        }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int NonLuaFileCount => _registry.NonLuaFileCount;
        public int DirectoryCount => _registry.DirectoryCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcSkillGocScriptIndexEntry> All => _registry.All;

        public PcSkillGocScriptIndexEntry GetByRelativePath(string relativePath)
            => _registry.GetByRelativePath(relativePath);

        public IReadOnlyList<PcSkillGocScriptIndexEntry> GetByDirectory(string directory)
            => _registry.GetByDirectory(directory);

        public static SkillGocScriptIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static SkillGocScriptIndexService LoadFromDirectory(string dir)
            => new SkillGocScriptIndexService(PcSkillGocScriptIndexParser.BuildRegistry(dir));

        public static SkillGocScriptIndexService LoadFromFile(string path)
        {
            var reg = new PcSkillGocScriptIndexRegistry();
            foreach (var entry in PcSkillGocScriptIndexParser.ParseFile(path)) reg.Register(entry);
            return new SkillGocScriptIndexService(reg);
        }
    }
}
