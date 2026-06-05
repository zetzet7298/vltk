// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.1 Core Libraries runtime service
// Quản lý 44 thư viện lõi và function signatures.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class LibraryScriptService
    {
        public const string LogTag = "LibraryScript";
        public const string DefaultStreamingDir = "Reference/PcGlobal";

        private PcLibraryScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public LibraryScriptService() { }
        public LibraryScriptService(PcLibraryScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcLibraryScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Library script registry rỗng");
        }

        public PcLibraryScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcLibraryScriptEntry> GetByLibrary(string libraryName)
            => _registry != null ? _registry.GetByLibrary(libraryName) : System.Array.Empty<PcLibraryScriptEntry>();
        public IReadOnlyList<PcLibraryScriptEntry> GetByReturnType(string returnType)
            => _registry != null ? _registry.GetByReturnType(returnType) : System.Array.Empty<PcLibraryScriptEntry>();

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public static LibraryScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new LibraryScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcLibraryScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
