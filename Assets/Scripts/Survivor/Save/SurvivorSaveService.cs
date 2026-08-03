// -----------------------------------------------------------------------------
// VLTK Survivor — Save service (progress + settings, v1 PlayerPrefs + JsonUtility)
// Parity shape dhcd BaseClientData (string key → JSON body, Save() sau mỗi write)
// + Sandbox PcSaveSlotService (JsonUtility serialize). KHÔNG copy 5-slot shape —
// survivor dùng 1 progress + 1 settings, key ổn định, version nằm TRONG JSON
// (migration chain đọc 1 key, không cần biết key cũ theo version).
//
// Corrupt-recovery: mỗi lần save thành công ghi thêm backup key `.bak`
// (last-known-good). Load thấy main key hỏng → thử `.bak`; `.bak` ok → phục hồi,
// `.bak` cũng hỏng → reset defaults. Backup luôn được giữ lại (không tự xóa).
//
// Migration hook cho v2+: set ProgressMigrator/SettingsMigrator,
// service loop fromVersion → fromVersion+1 → ... → CurrentVersion.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Kết quả load — để caller biết dữ liệu có phải từ backup/reset không.</summary>
    public enum SurvivorLoadOutcome
    {
        Fresh,               // chưa có save → defaults
        Loaded,              // đọc main key OK
        RecoveredFromBackup, // main key hỏng → phục hồi từ `.bak`
        ResetDefaults,       // main + backup đều hỏng → reset defaults
    }

    /// <summary>
    /// Save/load progress + settings. Class thuần (không MonoBehaviour, không scene)
    /// — EditMode test seam theo spec (Testing Decisions: pure-logic).
    /// </summary>
    public sealed class SurvivorSaveService
    {
        public const int ProgressVersion = 1;
        public const int SettingsVersion = 1;

        public const string ProgressKey = "survivor.progress";
        public const string SettingsKey = "survivor.settings";
        private const string BackupSuffix = ".bak";

        private readonly ISaveStorage _storage;

        /// <summary>Migration hook v2+: (data, fromVersion) → data ở version fromVersion+1.</summary>
        public Func<SurvivorProgressData, int, SurvivorProgressData> ProgressMigrator { get; set; } = (d, v) => d;

        /// <summary>Migration hook v2+ cho settings.</summary>
        public Func<SurvivorSettingsData, int, SurvivorSettingsData> SettingsMigrator { get; set; } = (d, v) => d;

        public SurvivorSaveService(ISaveStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public SurvivorSaveService() : this(new PlayerPrefsSaveStorage()) { }

        // --- Progress ---

        public SurvivorProgressData LoadProgress()
        {
            return LoadProgress(out _);
        }

        public SurvivorProgressData LoadProgress(out SurvivorLoadOutcome outcome)
        {
            return Load(
                ProgressKey, ProgressVersion,
                SurvivorProgressData.CreateDefault,
                ProgressMigrator,
                d => d.version,
                (d, v) => d.version = v,
                out outcome);
        }

        public void SaveProgress(SurvivorProgressData data)
        {
            data.version = ProgressVersion;
            Save(ProgressKey, data);
        }

        // --- Settings ---

        public SurvivorSettingsData LoadSettings()
        {
            return LoadSettings(out _);
        }

        public SurvivorSettingsData LoadSettings(out SurvivorLoadOutcome outcome)
        {
            return Load(
                SettingsKey, SettingsVersion,
                SurvivorSettingsData.CreateDefault,
                SettingsMigrator,
                d => d.version,
                (d, v) => d.version = v,
                out outcome);
        }

        public void SaveSettings(SurvivorSettingsData data)
        {
            data.version = SettingsVersion;
            Save(SettingsKey, data);
        }

        // --- Core (generic) ---

        private void Save<T>(string key, T data) where T : class
        {
            string json = JsonUtility.ToJson(data);
            _storage.SetString(key, json);
            _storage.SetString(key + BackupSuffix, json); // last-known-good backup
        }

        private T Load<T>(string key, int currentVersion, Func<T> makeDefault,
            Func<T, int, T> migrator, Func<T, int> getVersion, Action<T, int> setVersion,
            out SurvivorLoadOutcome outcome) where T : class
        {
            string raw = _storage.GetString(key, null);
            if (string.IsNullOrEmpty(raw))
            {
                outcome = SurvivorLoadOutcome.Fresh;
                return makeDefault();
            }

            // JsonUtility FromJson THROW ArgumentException với JSON syntax hỏng (không trả null) → bắt coi như corrupt.
            T data = TryParse<T>(raw);
            if (data != null)
            {
                outcome = SurvivorLoadOutcome.Loaded;
                return Migrate(data, currentVersion, migrator, getVersion, setVersion);
            }

            // Main key hỏng (corrupt/crash giữa write) → thử backup last-known-good.
            string backupRaw = _storage.GetString(key + BackupSuffix, null);
            if (!string.IsNullOrEmpty(backupRaw))
            {
                T backup = TryParse<T>(backupRaw);
                if (backup != null)
                {
                    outcome = SurvivorLoadOutcome.RecoveredFromBackup;
                    return Migrate(backup, currentVersion, migrator, getVersion, setVersion);
                }
            }

            // Cả hai hỏng → reset defaults. Backup key (bytes hỏng) được GIỮ lại
            // để điều tra, không tự xóa. Main key xóa cho lần save sau sạch sẽ.
            outcome = SurvivorLoadOutcome.ResetDefaults;
            _storage.DeleteKey(key);
            return makeDefault();
        }

        private static T TryParse<T>(string raw) where T : class
        {
            try { return JsonUtility.FromJson<T>(raw); }
            catch (System.Exception) { return null; }
        }

        private static T Migrate<T>(T data, int currentVersion, Func<T, int, T> migrator,
            Func<T, int> getVersion, Action<T, int> setVersion) where T : class
        {
            int guard = 0;
            while (getVersion(data) < currentVersion)
            {
                int from = getVersion(data);
                T next = migrator(data, from) ?? data;
                if (getVersion(next) <= from) setVersion(next, from + 1); // chống stall → loop vô hạn
                data = next;
                if (++guard > 64) break; // ponytail: hard cap, migration chain không bao giờ > 64 bước
            }
            setVersion(data, currentVersion);
            return data;
        }
    }
}
