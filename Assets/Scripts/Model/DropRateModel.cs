using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M7.x — PC drop rate table parsed from settings/droprate/npcdroprate*.ini.
    /// Mirrors the [Main] section and the per-item sections ([1]..[Count]).
    /// Each table covers one NPC level band or one special NPC group.
    /// </summary>
    [Serializable]
    public class DropRateTable
    {
        public string tableName;
        public int minNpcLevel;
        public int maxNpcLevel;

        public int count;
        public int randRange;
        public int magicRate;
        public int moneyRate;
        public int moneyScale;
        public int minItemLevel;
        public int minItemLevelScale;
        public int maxItemLevel;
        public int maxItemLevelScale;

        public List<DropRateEntry> entries = new List<DropRateEntry>();
    }

    /// <summary>
    /// PC drop rate entry. Each [N] section in the INI maps a particular item id
    /// (resolved through genre/detail/particular) to a probability weight.
    /// </summary>
    [Serializable]
    public class DropRateEntry
    {
        public int sectionIndex;
        public int genre;
        public int detail;
        public int particular;
        public int itemId;
        public int randRate;
        public int minItemLevel;
        public int maxItemLevel;
        public float probability;
    }
}
