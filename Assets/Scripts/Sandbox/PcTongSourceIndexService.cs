// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tong/guild source/config index service.
// Evidence-only wrapper over Reference/PcTongSource. Does not touch Tong runtime.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class PcTongSourceIndexService
    {
        public const string DefaultStreamingDir = "Reference/PcTongSource";
        public PcTongSourceIndexCatalog Catalog { get; }
        public int FileCount => Catalog != null ? Catalog.Count : 0;
        public int SourceFileCount => Catalog != null ? Catalog.SourceFileCount : 0;
        public int ConfigFileCount => Catalog != null ? Catalog.ConfigFileCount : 0;
        public int LuaFileCount => Catalog != null ? Catalog.LuaFileCount : 0;
        public int SourceRootCount => Catalog != null ? Catalog.SourceRootCount : 0;
        public int RootDirectoryCount => Catalog != null ? Catalog.RootDirectoryCount : 0;

        public PcTongSourceIndexService(PcTongSourceIndexCatalog catalog)
        {
            Catalog = catalog ?? new PcTongSourceIndexCatalog();
        }

        public static PcTongSourceIndexService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            var dir = Path.Combine(Application.streamingAssetsPath, subdir);
            return new PcTongSourceIndexService(PcTongSourceIndexParser.BuildCatalog(dir));
        }

        public PcTongSourceIndexEntry Get(string sourceRoot, string relativePath)
            => Catalog != null ? Catalog.Get(sourceRoot, relativePath) : null;
    }
}
