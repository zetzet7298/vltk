// -----------------------------------------------------------------------------
// VLTK Mobile — CityHongbaoService: runtime service cho hồng bao thành thị
// Source: PC settings/item/chengshidahongbao.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class CityHongbaoService
    {
        private readonly PcCityHongbaoRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public CityHongbaoService(PcCityHongbaoRegistry reg) { _reg = reg ?? new PcCityHongbaoRegistry(); }

        public static CityHongbaoService LoadFromStreamingAssets(string subDir = "Reference/PcItemFull")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new CityHongbaoService(PcCityHongbaoParser.BuildRegistry(path));
        }

        public PcCityHongbaoEntry Get(int id) => _reg?.Get(id);
        public IEnumerable<PcCityHongbaoEntry> All => _reg?.All ?? System.Array.Empty<PcCityHongbaoEntry>();
    }
}
