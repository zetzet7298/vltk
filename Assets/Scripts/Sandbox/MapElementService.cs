// -----------------------------------------------------------------------------
// VLTK Mobile — MapElement runtime service
// Wraps PcMapElementRegistry. PC source: settings/mapelem.txt.
// Ngũ Hành tương khắc: Kim khắc Mộc, Mộc khắc Thổ, Thổ khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Kim.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapElementService
    {
        private readonly PcMapElementRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapElementService() { _reg = new PcMapElementRegistry(); }
        public MapElementService(PcMapElementRegistry reg) { _reg = reg ?? new PcMapElementRegistry(); }

        public static MapElementService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapElementService(PcMapElementParser.BuildRegistry(path));
        }

        public PcMapElementEntry GetElement(int mapId) => _reg.Get(mapId);
        public IReadOnlyList<PcMapElementEntry> GetByElement(int elementType) => _reg.GetByElement(elementType);
        public IReadOnlyList<PcMapElementEntry> GetAllElements() => _reg.All;

        /// <summary>Trả về element chính (0-4) của map, hoặc -1 nếu chưa có.</summary>
        public int GetDominantElement(int mapId)
        {
            var e = _reg.Get(mapId);
            return e != null ? e.elementType : -1;
        }

        /// <summary>Tương khắc ngũ hành. Trả về 0=trung lập, 1=khắc (lợi), 2=bị khắc (bất lợi).</summary>
        public int GetElementalAdvantage(int attackerElement, int defenderElement)
        {
            if (attackerElement < 0 || attackerElement > 4 || defenderElement < 0 || defenderElement > 4) return 0;
            if (attackerElement == defenderElement) return 0;
            // Kim(0) khắc Mộc(1); Mộc(1) khắc Thổ(4); Thổ(4) khắc Thủy(2); Thủy(2) khắc Hỏa(3); Hỏa(3) khắc Kim(0)
            bool attackerWins = (attackerElement == 0 && defenderElement == 1)
                || (attackerElement == 1 && defenderElement == 4)
                || (attackerElement == 4 && defenderElement == 2)
                || (attackerElement == 2 && defenderElement == 3)
                || (attackerElement == 3 && defenderElement == 0);
            return attackerWins ? 1 : 2;
        }
    }
}
