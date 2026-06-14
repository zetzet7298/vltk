// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.2 Task Flag Service
// Quest and task flag storage system matching JX PC Task/Quest tables.
// Source: PC task flags (0=inactive, 1=active, 2=complete, 3=rewarded).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    [Serializable]
    public class TaskData
    {
        public int taskId;
        public int status; // 0=chưa nhận, 1=đang làm, 2=đã xong (chưa nhận thưởng), 3=hoàn thành
        public int progress;
        public int targetCount;
        public string descriptionVi;
    }

    /// <summary>
    /// Service quản lý Quest/Task flags (Nhiệm vụ Dã Tẩu, môn phái).
    /// PC source: Task flags, m_QuestState.
    /// </summary>
    public class TaskFlagService
    {
        private readonly Dictionary<int, TaskData> _flags = new();
        private ITaskFlagHost _host;

        /// <summary>Event kích hoạt khi trạng thái nhiệm vụ thay đổi.</summary>
        public event Action<int, int> OnTaskStatusChanged; // (taskId, status)

        public TaskFlagService() : this(null) { }
        public TaskFlagService(ITaskFlagHost host) { _host = host; }
        public void AttachHost(ITaskFlagHost host) { _host = host; }

        /// <summary>
        /// Đặt giá trị flag cho một nhiệm vụ.
        /// </summary>
        public void SetFlag(int taskId, int status, int progress = 0, int targetCount = 0, string desc = "")
        {
            if (!_flags.TryGetValue(taskId, out var task))
            {
                task = new TaskData { taskId = taskId };
                _flags[taskId] = task;
            }

            int oldStatus = task.status;
            task.status = status;
            task.progress = progress;
            if (targetCount > 0) task.targetCount = targetCount;
            if (!string.IsNullOrEmpty(desc)) task.descriptionVi = desc;

            if (_host != null)
            {
                _host.OnTaskFlagSet(taskId, oldStatus, status, progress, task.targetCount);
                if (status == 2)
                    _host.OnTaskComplete(taskId, progress, task.targetCount);
                else if (status == 3)
                    _host.OnTaskRewarded(taskId);
                _host.ShowTaskUI(taskId, status, progress, task.targetCount);
                _host.LogTaskFlagEvent(taskId, status, $"Task {taskId} status: {oldStatus} → {status}");
                _host.PlayTaskSFX(taskId, status, status == 3 ? "reward" : (status == 2 ? "complete" : "update"));
                _host.SaveTaskFlagState(taskId, status, progress, task.targetCount);
            }

            if (oldStatus != status)
            {
                SubsystemLog.Info("TaskFlag", $"Task {taskId} status changed: {oldStatus} → {status}");
                OnTaskStatusChanged?.Invoke(taskId, status);
            }
        }

        /// <summary>Lấy trạng thái hiện tại của nhiệm vụ.</summary>
        public int GetFlag(int taskId)
        {
            return _flags.TryGetValue(taskId, out var task) ? task.status : 0;
        }

        /// <summary>Nhiệm vụ có tồn tại trong hệ thống không.</summary>
        public bool HasFlag(int taskId)
        {
            return _flags.ContainsKey(taskId);
        }

        /// <summary>Nhiệm vụ đã hoàn thành nhưng chưa trả.</summary>
        public bool IsTaskComplete(int taskId)
        {
            return GetFlag(taskId) == 2;
        }

        /// <summary>Nhiệm vụ đã kết thúc hoàn toàn (đã nhận thưởng).</summary>
        public bool IsTaskFinished(int taskId)
        {
            return GetFlag(taskId) == 3;
        }

        /// <summary>
        /// Kiểm tra xem người chơi có đủ điều kiện nhận nhiệm vụ không.
        /// </summary>
        public bool CanAcceptTask(int taskId, int playerLevel, int reqLevel, int prerequisiteTaskId = 0)
        {
            if (playerLevel < reqLevel)
            {
                _host?.OnTaskAcceptDenied(taskId, playerLevel, reqLevel);
                return false;
            }
            if (GetFlag(taskId) > 0) return false; // Đã nhận hoặc đã làm xong

            // Kiểm tra nhiệm vụ tiên quyết
            if (prerequisiteTaskId > 0)
            {
                return IsTaskFinished(prerequisiteTaskId);
            }

            return true;
        }

        /// <summary>Lấy thông tin tiến độ của nhiệm vụ.</summary>
        public TaskData GetTaskData(int taskId)
        {
            return _flags.TryGetValue(taskId, out var task) ? task : null;
        }

        /// <summary>
        /// Serialize quest state sang JSON để save/load game.
        /// </summary>
        public string SerializeToSave()
        {
            var list = new List<TaskData>(_flags.Values);
            string json = JsonUtility.ToJson(new TaskSaveWrapper { tasks = list });
            _host?.OnSerialized(json, list.Count);
            return json;
        }

        /// <summary>
        /// Load quest state từ dữ liệu save.
        /// </summary>
        public void DeserializeFromSave(string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var wrapper = JsonUtility.FromJson<TaskSaveWrapper>(json);
                _flags.Clear();
                if (wrapper?.tasks != null)
                {
                    foreach (var task in wrapper.tasks)
                    {
                        _flags[task.taskId] = task;
                    }
                    _host?.OnDeserialized(wrapper.tasks.Count);
                }
                else
                {
                    _host?.OnDeserialized(0);
                }
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("TaskFlag", $"Failed to load quest state: {ex.Message}");
            }
        }

        [Serializable]
        private class TaskSaveWrapper
        {
            public List<TaskData> tasks;
        }

        // ─── Task flag catalog loader (PC source: settings/task/taskflag.txt) ───
        public const string LogTag = "TaskFlag";
        public const string DefaultStreamingDir = "Reference/PcTask";

        private PcTaskFlagRegistry _catalog;

        public int CatalogCount => _catalog != null ? _catalog.Count : 0;

        public void AttachCatalog(PcTaskFlagRegistry reg)
        {
            _catalog = reg ?? new PcTaskFlagRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_catalog.Count} cờ nhiệm vụ");
            _host?.OnCatalogAttached(_catalog.Count);
        }

        public PcTaskFlagEntry GetFlagMeta(int flagId)
            => _catalog != null ? _catalog.Get(flagId) : null;

        public IReadOnlyList<PcTaskFlagEntry> GetFlagsByType(int taskType)
            => _catalog != null ? _catalog.GetByType(taskType) : Array.Empty<PcTaskFlagEntry>();

        public IReadOnlyList<PcTaskFlagEntry> GetFlagsByCategory(int categoryId)
            => _catalog != null ? _catalog.GetByCategory(categoryId) : Array.Empty<PcTaskFlagEntry>();

        public static TaskFlagService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TaskFlagService();
            if (Directory.Exists(dir))
            {
                var reg = PcTaskFlagParser.BuildRegistry(dir);
                svc.AttachCatalog(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Task flag catalog: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
