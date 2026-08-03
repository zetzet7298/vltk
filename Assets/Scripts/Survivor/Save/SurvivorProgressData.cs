// -----------------------------------------------------------------------------
// VLTK Survivor — Progress data model (meta, cross-run)
// Shape tham chiếu research 09 §1.1 (SurvivorProgressData) + dhcd BaseClientData
// (string key → JSON body). JsonUtility KHÔNG serialize Dictionary → meta
// upgrade dùng List key-value thay cho Dictionary<string,int>.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>
    /// Một meta-upgrade đã mua (id + level). Dạng pair vì JsonUtility
    /// không serialize Dictionary (research 09 §1.1 đề xuất Dictionary).
    /// </summary>
    [Serializable]
    public sealed class SurvivorMetaUpgrade
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// Tiến trình cross-run: unlock / best run / meta-upgrade.
    /// Mid-run state (loadout, wave index...) KHÔNG nằm đây — defer (research 09 §1.1).
    /// </summary>
    [Serializable]
    public sealed class SurvivorProgressData
    {
        /// <summary>Schema version — migrate khi thêm field (SurvivorSaveService).</summary>
        public int version;

        public int bestFloor;
        public long bestScore;
        public long totalKills;
        public int runCount;

        /// <summary>Stage đã unlock (nếu stage unlock là feature).</summary>
        public List<int> unlockedStageIds = new();

        /// <summary>Meta-upgrade currency/level (khi P2 chốt hệ meta-upgrade cụ thể).</summary>
        public List<SurvivorMetaUpgrade> metaUpgrades = new();

        public static SurvivorProgressData CreateDefault()
        {
            return new SurvivorProgressData
            {
                version = SurvivorSaveService.ProgressVersion,
                unlockedStageIds = new List<int> { 1 },
            };
        }
    }
}
