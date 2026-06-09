// -----------------------------------------------------------------------------
// VLTK Mobile — service wrapper for PC itemexchange_setting source inventory.
// Phase 1 only: exposes imported PC source facts, not exchange execution.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class ItemExchangeSourceCatalogService
    {
        public const string LogTag = "ItemExchangeSource";
        public PcItemExchangeSourceCatalog Catalog { get; private set; }
        public int NormalRowCount => Catalog != null && Catalog.normal != null ? Catalog.normal.dataRowCount : 0;
        public int RareRowCount => Catalog != null && Catalog.rare != null ? Catalog.rare.dataRowCount : 0;
        public int LevelExpRowCount => Catalog != null && Catalog.levelExp != null ? Catalog.levelExp.dataRowCount : 0;
        public int LevelLeadExpRowCount => Catalog != null && Catalog.levelLeadExp != null ? Catalog.levelLeadExp.dataRowCount : 0;
        public int RoleValueKeyCount => Catalog != null && Catalog.roleValue != null ? Catalog.roleValue.keys.Count : 0;

        public ItemExchangeSourceCatalogService(PcItemExchangeSourceCatalog catalog)
        {
            Catalog = catalog ?? new PcItemExchangeSourceCatalog();
        }

        public static ItemExchangeSourceCatalogService LoadFromStreamingAssets(string subdir = "Reference/PcItemExchange")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcItemExchangeSourceParser.ParseDirectory(dir);
            if (catalog.normal.exists || catalog.rare.exists || catalog.levelExp.exists || catalog.levelLeadExp.exists)
                SubsystemLog.Info(LogTag, $"Loaded PC itemexchange_setting source catalog from {dir}");
            else
                SubsystemLog.Warn(LogTag, $"PC itemexchange_setting source catalog missing at {dir}");
            return new ItemExchangeSourceCatalogService(catalog);
        }
    }
}
