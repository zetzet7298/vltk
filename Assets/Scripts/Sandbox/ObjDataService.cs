// -----------------------------------------------------------------------------
// VLTK Mobile — ObjDataService: runtime service cho PC obj/objdata.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime cho metadata object thế giới (rương, biển báo, vật rơi).
    /// </summary>
    public class ObjDataService
    {
        private readonly PcObjDataRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ObjDataService() { _reg = new PcObjDataRegistry(); }
        public ObjDataService(PcObjDataRegistry reg) { _reg = reg ?? new PcObjDataRegistry(); }

        public static ObjDataService LoadFromStreamingAssets(string subDir = "Reference/PcObj")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new ObjDataService(PcObjDataParser.BuildRegistry(path));
        }

        public PcObjDataEntry Get(int dataId) => _reg.Get(dataId);
        public IReadOnlyList<PcObjDataEntry> All => _reg.All;
        public IReadOnlyList<PcObjDataEntry> GetByKind(string kind) => _reg.GetByKind(kind);
    }
}
