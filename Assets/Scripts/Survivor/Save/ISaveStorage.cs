// -----------------------------------------------------------------------------
// VLTK Survivor — Save storage backend (mỏng)
// Seam để test pure-logic: runtime dùng PlayerPrefs, test dùng in-memory.
// Shape tham chiếu Sandbox PcSaveSlotService (string key → JSON body),
// KHÔNG copy (survivor không cần 5-slot RPG — 1 progress + 1 settings).
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Backend lưu trữ key-value mỏng. Interface duy nhất để save service
    /// không phụ thuộc PlayerPrefs (EditMode test dùng impl in-memory).
    /// </summary>
    public interface ISaveStorage
    {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
        void DeleteKey(string key);
    }

    /// <summary>
    /// Impl PlayerPrefs (v1 parity dhcd BaseClientData: PlayerPrefs.GetString/SetString
    /// + Save() sau mỗi write). Corrupt nếu crash giữa set — chấp nhận cho
    /// offline roguelike, corrupt-recovery nằm ở SurvivorSaveService.
    /// </summary>
    public sealed class PlayerPrefsSaveStorage : ISaveStorage
    {
        public string GetString(string key, string defaultValue)
        {
            return PlayerPrefs.GetString(key, defaultValue);
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
            PlayerPrefs.Save();
        }

        public void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }
    }
}
