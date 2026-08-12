// -----------------------------------------------------------------------------
// VLTK Mobile — TongNpcPosService: runtime service cho vị trí NPC bang
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TongNpcPosService
    {
        private readonly PcTongNpcPosRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TongNpcPosService() : this(null) { }

        public TongNpcPosService(PcTongNpcPosRegistry reg) { _reg = reg ?? new PcTongNpcPosRegistry(); }

        public static TongNpcPosService LoadFromStreamingAssets(string subDir = "Reference/PcTong")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TongNpcPosService(PcTongNpcPosParser.BuildRegistry(path));
        }

        public PcTongNpcPosEntry GetNpc(int id) => _reg.Get(id);
        public IEnumerable<PcTongNpcPosEntry> GetByMap(int mapId) => _reg.GetByMap(mapId);
        public IEnumerable<PcTongNpcPosEntry> GetByType(int type) => _reg.GetByType(type);
    }
}
