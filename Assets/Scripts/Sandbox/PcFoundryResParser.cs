// -----------------------------------------------------------------------------
// VLTK Mobile — PC foundryresdemand.ini parser (lò rèn tài nguyên theo sơ đồ)
// Source: settings/item/foundryresdemand.ini (GB2312). INI with [ResScheme_i] sections
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcFoundryResScheme
    {
        public string SchemeName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcFoundryResRegistry
    {
        private readonly Dictionary<string, PcFoundryResScheme> _byScheme = new Dictionary<string, PcFoundryResScheme>();
        public int Count => _byScheme.Count;
        public PcFoundryResScheme Get(string scheme) => _byScheme.TryGetValue(scheme, out var v) ? v : null;
        public IEnumerable<PcFoundryResScheme> All => _byScheme.Values;
        public void Add(PcFoundryResScheme e) { if (e != null) _byScheme[e.SchemeName] = e; }
    }

    public static class PcFoundryResParser
    {
        public static PcFoundryResRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcFoundryResRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "foundryresdemand.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            PcFoundryResScheme current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcFoundryResScheme { SchemeName = section };
                    reg.Add(current);
                    continue;
                }
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0 && current != null)
                {
                    var key = line.Substring(0, eqIdx).Trim();
                    var val = line.Substring(eqIdx + 1).Trim();
                    current.Values[key] = val;
                }
            }
            return reg;
        }
    }
}
