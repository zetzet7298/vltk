// -----------------------------------------------------------------------------
// VLTK Mobile — PC partner/pet source index service.
// Catalog only: loads partner/pet source file evidence from
// partner_pet_source_index.txt and exposes counts/lookups. Runtime partner/pet
// behavior remains a separate port.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class PartnerPetSourceIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcPartnerPet";
        public const string StreamingAssetsRelativePath = "Reference/PcPartnerPet/partner_pet_source_index.txt";

        private readonly PcPartnerPetSourceIndexRegistry _registry;

        public PartnerPetSourceIndexService()
            : this(new PcPartnerPetSourceIndexRegistry())
        {
        }

        public PartnerPetSourceIndexService(PcPartnerPetSourceIndexRegistry registry)
        {
            _registry = registry ?? new PcPartnerPetSourceIndexRegistry();
        }

        public int Count => _registry.Count;
        public int ConfigFileCount => _registry.ConfigFileCount;
        public int LuaFileCount => _registry.LuaFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcPartnerPetSourceIndexEntry> All => _registry.All;

        public PcPartnerPetSourceIndexEntry GetBySourceRootPath(string sourceRoot, string relativePath)
            => _registry.GetBySourceRootPath(sourceRoot, relativePath);

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetByCategory(string category)
            => _registry.GetByCategory(category);

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetBySourceRoot(string sourceRoot)
            => _registry.GetBySourceRoot(sourceRoot);

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetByFileName(string fileName)
            => _registry.GetByFileName(fileName);

        public static PartnerPetSourceIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static PartnerPetSourceIndexService LoadFromDirectory(string dir)
            => new PartnerPetSourceIndexService(PcPartnerPetSourceIndexParser.BuildRegistry(dir));

        public static PartnerPetSourceIndexService LoadFromFile(string path)
        {
            var reg = new PcPartnerPetSourceIndexRegistry();
            foreach (var entry in PcPartnerPetSourceIndexParser.ParseFile(path)) reg.Register(entry);
            return new PartnerPetSourceIndexService(reg);
        }
    }
}
