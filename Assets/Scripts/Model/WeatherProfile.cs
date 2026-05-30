using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M1.1 — Weather profile converted from map setting [weather] section.
    /// </summary>
    [Serializable]
    public class WeatherEntry
    {
        public int index;
        public int type;   // weather type id from PC source (0=clear, 1=rain, etc.)
        public int odds;   // probability weight
    }

    [Serializable]
    public class WeatherProfile
    {
        public string profileId;
        public List<WeatherEntry> entries = new();
        public float windSpeedX;
        public float windSpeedY;
        public float windSpeedZ;
    }

    /// <summary>
    /// M1.1 — Light profile from map setting [light] section.
    /// Stores raw key=value strings from the PC INI until full parsing is done.
    /// </summary>
    [Serializable]
    public class LightProfile
    {
        public string profileId;
        /// <summary>Raw key=value entries from the [light] INI section.</summary>
        public List<string> rawEntries = new();
    }
}
