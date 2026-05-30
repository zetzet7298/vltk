using System;

namespace VLTK.Model
{
    /// <summary>
    /// Discovery report produced by the Python map catalog tool.
    /// Populated from the "conversionReport" section in MapCatalog.json (AC4).
    /// </summary>
    [Serializable]
    public class MapDiscoveryReport
    {
        /// <summary>Total INI/TXT setting files discovered in maps_pak.</summary>
        public int totalDiscovered;

        /// <summary>Maps with haveMap=true and valid rect — ready for conversion.</summary>
        public int available;

        /// <summary>Maps with haveMap=false — no usable source data.</summary>
        public int missing;

        /// <summary>Maps with haveMap=true but missing rect/region data.</summary>
        public int incomplete;

        /// <summary>Maps whose display name fell back to Map_{id} (no readable name).</summary>
        public int unnamed;

        /// <summary>ISO-8601 UTC timestamp when the catalog was generated.</summary>
        public string generatedAt;

        /// <summary>Version of the parse_map_settings.py tool that generated the catalog.</summary>
        public string toolVersion;
    }
}
