// -----------------------------------------------------------------------------
// VLTK Mobile — FoundryResDemandService: runtime service cho lò rèn tài nguyên
// Source: PC settings/item/foundryresdemand.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class FoundryResDemandService
    {
        private readonly PcFoundryResRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public FoundryResDemandService(PcFoundryResRegistry reg) { _reg = reg ?? new PcFoundryResRegistry(); }

        public static FoundryResDemandService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new FoundryResDemandService(PcFoundryResParser.BuildRegistry(path));
        }

        public PcFoundryResScheme GetScheme(string name) => _reg?.Get(name);
        public IEnumerable<PcFoundryResScheme> AllSchemes => _reg?.All ?? System.Array.Empty<PcFoundryResScheme>();
        public string GetValue(string scheme, string key)
        {
            var s = _reg?.Get(scheme);
            return s != null && s.Values.TryGetValue(key, out var v) ? v : null;
        }
    }
}
