// -----------------------------------------------------------------------------
// VLTK Mobile — Save/Load Game Slots (Ô lưu trữ game)
// Quản lý slot lưu game cho nhân vật, bao gồm auto-save.
// Serializes to JSON via UnityEngine.JsonUtility.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Metadata của 1 slot lưu game.
    /// </summary>
    [Serializable]
    public class SaveSlotData
    {
        public int slotId;
        public string playerName;
        public int playerLevel;
        public int mapId;
        public int playTimeSec;
        public long saveTimeUnix;
        public int faction;
        public long gold;
        public bool isAutoSave;
    }

    /// <summary>
    /// Snapshot nhân vật để lưu/xuất.
    /// </summary>
    [Serializable]
    public class PlayerSnapshot
    {
        public int slotId;
        public string playerName;
        public int playerLevel;
        public int mapId;
        public int playTimeSec;
        public long saveTimeUnix;
        public int faction;
        public long gold;
        public List<int> learnedSkillIds = new();
        public List<int> inventoryItemIds = new();
        public string serializedState;
    }

    /// <summary>
    /// Wrapper để JsonUtility serialize list of slots.
    /// </summary>
    [Serializable]
    public class SaveSlotListWrapper
    {
        public List<SaveSlotData> slots = new();
    }

    /// <summary>
    /// Quản lý save slots (ô lưu game).
    /// </summary>
    public class PcSaveSlotService
    {
        public const string LogTag = "SaveSlot";
        public const int MaxSlots = 5;

        private readonly Dictionary<int, SaveSlotData> _slots = new();
        private readonly Dictionary<int, PlayerSnapshot> _fullSnapshots = new();
        private SaveSlotData _autoSaveMeta;
        private PlayerSnapshot _autoSaveSnapshot;

        public int Count => _slots.Count;
        public bool HasAutoSave => _autoSaveMeta != null;

        public PcSaveSlotService() { }

        /// <summary>Lưu nhân vật vào slot cụ thể.</summary>
        public bool SaveGame(int slotId, PlayerSnapshot snapshot)
        {
            if (snapshot == null) return false;
            if (slotId < 0 || slotId >= MaxSlots) return false;
            snapshot.slotId = slotId;
            snapshot.saveTimeUnix = NowUnix();
            _fullSnapshots[slotId] = snapshot;
            _slots[slotId] = new SaveSlotData
            {
                slotId = slotId,
                playerName = snapshot.playerName ?? string.Empty,
                playerLevel = snapshot.playerLevel,
                mapId = snapshot.mapId,
                playTimeSec = snapshot.playTimeSec,
                saveTimeUnix = snapshot.saveTimeUnix,
                faction = snapshot.faction,
                gold = snapshot.gold,
                isAutoSave = false,
            };
            return true;
        }

        /// <summary>Tải nhân vật từ slot.</summary>
        public PlayerSnapshot LoadGame(int slotId)
        {
            return _fullSnapshots.TryGetValue(slotId, out var snap) ? snap : null;
        }

        /// <summary>Xóa slot lưu.</summary>
        public bool DeleteSave(int slotId)
        {
            bool had = _slots.Remove(slotId) || _fullSnapshots.Remove(slotId);
            return had;
        }

        /// <summary>Danh sách slot metadata.</summary>
        public IReadOnlyList<SaveSlotData> GetAllSlots()
        {
            var list = new List<SaveSlotData>(_slots.Values);
            list.Sort((a, b) => a.slotId.CompareTo(b.slotId));
            return list;
        }

        /// <summary>Lưu auto (ghi đè).</summary>
        public void AutoSave(PlayerSnapshot snapshot)
        {
            if (snapshot == null) return;
            snapshot.saveTimeUnix = NowUnix();
            _autoSaveSnapshot = snapshot;
            _autoSaveMeta = new SaveSlotData
            {
                slotId = -1,
                playerName = snapshot.playerName ?? string.Empty,
                playerLevel = snapshot.playerLevel,
                mapId = snapshot.mapId,
                playTimeSec = snapshot.playTimeSec,
                saveTimeUnix = snapshot.saveTimeUnix,
                faction = snapshot.faction,
                gold = snapshot.gold,
                isAutoSave = true,
            };
        }

        /// <summary>Lấy snapshot auto-save.</summary>
        public PlayerSnapshot LoadAutoSave() => _autoSaveSnapshot;
        public SaveSlotData GetAutoSaveMeta() => _autoSaveMeta;

        /// <summary>Serialize toàn bộ state thành JSON.</summary>
        public string SerializeToJson()
        {
            var wrap = new SaveSlotListWrapper();
            wrap.slots.AddRange(_slots.Values);
            if (_autoSaveMeta != null) wrap.slots.Add(_autoSaveMeta);
            return JsonUtility.ToJson(wrap);
        }

        public static PcSaveSlotService LoadFromStreamingAssets()
        {
            return new PcSaveSlotService();
        }

        private static long NowUnix() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
