// -----------------------------------------------------------------------------
// VLTK Survivor — Settings data model (riêng khỏi progress, research 09 §3)
// Audio default parity Sandbox AudioService (_categoryVolume BGM 0.6, SFX 0.8).
// Lang default "vi" (fallback vi, D14). Graphics = QualitySettings int index.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Survivor
{
    /// <summary>
    /// Settings persist giữa phiên. Persistence pattern research 09 §3.4:
    /// key `survivor.settings` → JSON, PlayerPrefs.Save() sau mỗi write,
    /// áp dụng runtime ngay khi thay đổi (caller tự apply).
    /// </summary>
    [Serializable]
    public sealed class SurvivorSettingsData
    {
        /// <summary>Schema version — migrate khi thêm field (SurvivorSaveService).</summary>
        public int version;

        public float audioBgm;
        public float audioSfx;
        public string lang;
        public int quality;

        public static SurvivorSettingsData CreateDefault()
        {
            return new SurvivorSettingsData
            {
                version = SurvivorSaveService.SettingsVersion,
                audioBgm = 0.6f,
                audioSfx = 0.8f,
                lang = "vi",
                quality = 1,
            };
        }
    }
}
