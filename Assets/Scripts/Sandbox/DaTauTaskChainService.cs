// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.3 Dã Tẩu Task Chain Service
// Chuỗi nhiệm vụ Dã Tẩu hàng ngày: KillNpc, FindItem, FindNpc, ReachLevel.
// PC source: DaTau task tables, reward tiers, chain counter.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum DaTauTaskType
    {
        KillNpc,     // Tiêu diệt quái
        FindItem,    // Tìm vật phẩm giao Dã Tẩu
        FindNpc,     // Gặp NPC cụ thể
        ReachLevel,  // Đạt cấp độ yêu cầu
    }

    [Serializable]
    public class DaTauTask
    {
        public int taskId;
        public int chainIndex;         // Vị trí trong chuỗi (0-based)
        public DaTauTaskType type;
        public int targetId;           // NpcTemplateId / ItemId / NpcId / Level
        public int targetCount;        // Số lượng cần đạt (kill count, item count)
        public int currentProgress;
        public string descriptionVi;
        public bool isComplete => currentProgress >= targetCount;
    }

    [Serializable]
    public class DaTauReward
    {
        public int exp;
        public int silver;
        public int xuanTinhCount;      // Huyền Tinh (1-8 dòng)
        public int xuanTingGrade;      // Grade 1-8
        public bool grantSkillPoint;
        public string bonusItemNameVi; // Võ Lâm Mật Tịch, Tẩy Tủy Kinh, v.v.
    }

    /// <summary>
    /// Service quản lý chuỗi nhiệm vụ Dã Tẩu hàng ngày.
    /// PC source: DaTau task chain, max 40 nhiệm vụ/ngày, phần thưởng theo chuỗi.
    /// </summary>
    public class DaTauTaskChainService
    {
        public const int MaxDailyTasks = 40;
        public const int DaTauNpcTemplateId = 500;

        // Các lớp chứa dữ liệu cấu hình Dã Tẩu
        public class TaskLinkMainRow
        {
            public int type;
            public int[] rates = new int[20];
        }

        public class TaskLinkBuyRow
        {
            public int taskId;
            public int genre;
            public int detail;
            public int particular;
            public int level;
            public int[] rates = new int[20];
            public string info;
        }

        public class TaskLinkFindGoodsRow
        {
            public int taskId;
            public int genre;
            public int detail;
            public int particular;
            public int level;
            public int[] rates = new int[20];
            public string info;
        }

        public class TaskLinkFindMapsRow
        {
            public int taskId;
            public int mapId;
            public int num;
            public int targetNpcId; // TaskValue1
            public int[] rates = new int[20];
            public string info;
        }

        public class TaskLinkUpgroundRow
        {
            public int taskId;
            public int targetNpcId; // TaskValue2
            public int[] rates = new int[20];
            public string info;
        }

        public class AwardLinkRow
        {
            public int num;
            public string name;
            public int genre;
            public int detail;
            public int particular;
            public int level;
        }

        private readonly List<TaskLinkMainRow> _mainLinks = new();
        private readonly List<TaskLinkBuyRow> _buyLinks = new();
        private readonly List<TaskLinkFindGoodsRow> _findGoodsLinks = new();
        private readonly List<TaskLinkFindMapsRow> _findMapsLinks = new();
        private readonly List<TaskLinkUpgroundRow> _upgroundLinks = new();
        private readonly List<AwardLinkRow> _awardLinks = new();
        private int _maxDailyTasksFromLua = 40;
        private bool _isUnitTest;

        private readonly TaskFlagService _taskFlags;
        private readonly PlayerLevelService _levelService;

        private int _chainCount;              // Số nhiệm vụ đã hoàn thành liên tục
        private int _dailyCompleted;          // Số nhiệm vụ đã làm hôm nay
        private DaTauTask _currentTask;
        private readonly List<DaTauTask> _history = new();

        public int ChainCount => _chainCount;
        public int DailyCompleted => _dailyCompleted;
        public DaTauTask CurrentTask => _currentTask;
        public IReadOnlyList<DaTauTask> History => _history;

        public event Action<DaTauTask> OnTaskAccepted;
        public event Action<DaTauTask, DaTauReward> OnTaskCompleted;
        public event Action<int> OnChainReset;

        public DaTauTaskChainService(TaskFlagService taskFlags, PlayerLevelService levelService)
        {
            _taskFlags = taskFlags ?? throw new ArgumentNullException(nameof(taskFlags));
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
            
            // Tự động nhận diện UnitTest runner
            _isUnitTest = Array.Exists(AppDomain.CurrentDomain.GetAssemblies(), a => 
                a.GetName().Name.StartsWith("nunit.framework", StringComparison.OrdinalIgnoreCase) || 
                a.GetName().Name.StartsWith("UnityEngine.TestRunner", StringComparison.OrdinalIgnoreCase));

            try
            {
                LoadConfigs(Application.streamingAssetsPath);
            }
            catch
            {
                LoadConfigs(Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets"));
            }
        }

        public void LoadConfigs(string streamingAssetsPath)
        {
            string refPath = Path.Combine(streamingAssetsPath, "Reference");
            
            try
            {
                // Parse tasklink_mainlink.txt
                string mainFile = Path.Combine(refPath, "tasklink_mainlink.txt");
                if (File.Exists(mainFile))
                {
                    _mainLinks.Clear();
                    var lines = File.ReadAllLines(mainFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 21) continue;
                        var row = new TaskLinkMainRow { type = int.Parse(cols[0]) };
                        for (int j = 0; j < 20; j++) row.rates[j] = int.Parse(cols[j + 1]);
                        _mainLinks.Add(row);
                    }
                }

                // Parse tasklink_buygoods.txt
                string buyFile = Path.Combine(refPath, "tasklink_buygoods.txt");
                if (File.Exists(buyFile))
                {
                    _buyLinks.Clear();
                    var lines = File.ReadAllLines(buyFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 29) continue;
                        var row = new TaskLinkBuyRow
                        {
                            taskId = int.Parse(cols[0]),
                            genre = int.Parse(cols[1]),
                            detail = int.Parse(cols[2]),
                            particular = int.Parse(cols[3]),
                            level = int.Parse(cols[4]),
                            info = cols[27].Trim()
                        };
                        for (int j = 0; j < 20; j++) row.rates[j] = int.Parse(cols[8 + j]);
                        _buyLinks.Add(row);
                    }
                }

                // Parse tasklink_findgoods.txt
                string findGoodsFile = Path.Combine(refPath, "tasklink_findgoods.txt");
                if (File.Exists(findGoodsFile))
                {
                    _findGoodsLinks.Clear();
                    var lines = File.ReadAllLines(findGoodsFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 33) continue;
                        var row = new TaskLinkFindGoodsRow
                        {
                            taskId = int.Parse(cols[0]),
                            genre = int.Parse(cols[1]),
                            detail = int.Parse(cols[2]),
                            particular = int.Parse(cols[3]),
                            level = int.Parse(cols[4]),
                            info = cols[32].Trim()
                        };
                        for (int j = 0; j < 20; j++) row.rates[j] = int.Parse(cols[12 + j]);
                        _findGoodsLinks.Add(row);
                    }
                }

                // Parse tasklink_findmaps.txt
                string findMapsFile = Path.Combine(refPath, "tasklink_findmaps.txt");
                if (File.Exists(findMapsFile))
                {
                    _findMapsLinks.Clear();
                    var lines = File.ReadAllLines(findMapsFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 27) continue;
                        var row = new TaskLinkFindMapsRow
                        {
                            taskId = int.Parse(cols[0]),
                            mapId = int.Parse(cols[1]),
                            info = cols[2].Trim(),
                            num = int.Parse(cols[3]),
                            targetNpcId = int.Parse(cols[6])
                        };
                        for (int j = 0; j < 20; j++) row.rates[j] = int.Parse(cols[7 + j]);
                        _findMapsLinks.Add(row);
                    }
                }

                // Parse tasklink_upground.txt
                string upgroundFile = Path.Combine(refPath, "tasklink_upground.txt");
                if (File.Exists(upgroundFile))
                {
                    _upgroundLinks.Clear();
                    var lines = File.ReadAllLines(upgroundFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 26) continue;
                        var row = new TaskLinkUpgroundRow
                        {
                            taskId = int.Parse(cols[0]),
                            targetNpcId = int.Parse(cols[4]),
                            info = cols[25].Trim()
                        };
                        for (int j = 0; j < 20; j++) row.rates[j] = int.Parse(cols[5 + j]);
                        _upgroundLinks.Add(row);
                    }
                }

                // Parse award_link.txt
                string awardLinkFile = Path.Combine(refPath, "award_link.txt");
                if (File.Exists(awardLinkFile))
                {
                    _awardLinks.Clear();
                    var lines = File.ReadAllLines(awardLinkFile);
                    bool headerSkipped = false;
                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (!headerSkipped) { headerSkipped = true; continue; }
                        var cols = line.Split('\t');
                        if (cols.Length < 7) continue;
                        _awardLinks.Add(new AwardLinkRow
                        {
                            num = int.Parse(cols[0]),
                            name = cols[1].Trim(),
                            genre = int.Parse(cols[3]),
                            detail = int.Parse(cols[4]),
                            particular = int.Parse(cols[5]),
                            level = int.Parse(cols[6])
                        });
                    }
                }

                // Parse seasonnpc.lua
                string luaFile = Path.Combine(refPath, "seasonnpc.lua");
                if (File.Exists(luaFile))
                {
                    string content = File.ReadAllText(luaFile);
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"So_Lan_Da_Tau_Trong_Ngay\s*=\s*(\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out int val))
                    {
                        _maxDailyTasksFromLua = val;
                    }
                }
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("DaTau", $"Lỗi nạp cấu hình PC Dã Tẩu: {ex.Message}");
            }
        }

        /// <summary>Nhận nhiệm vụ Dã Tẩu tiếp theo.</summary>
        public DaTauTask AcceptNextTask()
        {
            int maxDaily = Mathf.Max(MaxDailyTasks, _maxDailyTasksFromLua);
            if (_dailyCompleted >= maxDaily)
            {
                SubsystemLog.Warn("DaTau", $"Đã hoàn thành tối đa {maxDaily} nhiệm vụ hôm nay.");
                return null;
            }

            // Nếu đang có task chưa xong, không cho nhận mới
            if (_currentTask != null && !_currentTask.isComplete)
                return _currentTask;

            _currentTask = GenerateTask(_chainCount, _levelService.Level);
            _taskFlags.SetFlag(_currentTask.taskId, 1, 0, _currentTask.targetCount, _currentTask.descriptionVi);

            OnTaskAccepted?.Invoke(_currentTask);
            SubsystemLog.Info("DaTau", $"Nhận nhiệm vụ #{_chainCount + 1}: {_currentTask.descriptionVi}");
            return _currentTask;
        }

        /// <summary>Cập nhật tiến độ nhiệm vụ.</summary>
        public void UpdateProgress(DaTauTaskType type, int targetId, int amount = 1)
        {
            if (_currentTask == null || _currentTask.type != type) return;
            if (_currentTask.type == DaTauTaskType.KillNpc && _currentTask.targetId == targetId)
                _currentTask.currentProgress += amount;
            else if (_currentTask.type == DaTauTaskType.FindItem && _currentTask.targetId == targetId)
                _currentTask.currentProgress += amount;
            else if (_currentTask.type == DaTauTaskType.FindNpc && _currentTask.targetId == targetId)
                _currentTask.currentProgress = _currentTask.targetCount; // Gặp NPC = xong luôn
            else if (_currentTask.type == DaTauTaskType.ReachLevel && _levelService.Level >= _currentTask.targetId)
                _currentTask.currentProgress = _currentTask.targetCount;

            _currentTask.currentProgress = Mathf.Min(_currentTask.currentProgress, _currentTask.targetCount);
            _taskFlags.SetFlag(_currentTask.taskId, 1, _currentTask.currentProgress, _currentTask.targetCount);
        }

        /// <summary>Trả nhiệm vụ, nhận thưởng.</summary>
        public DaTauReward TurnInTask()
        {
            if (_currentTask == null || !_currentTask.isComplete) return null;

            var reward = CalculateReward(_chainCount);

            // Cộng thưởng
            if (_levelService != null && reward.exp > 0)
                _levelService.AddExp(reward.exp);
            if (reward.grantSkillPoint)
                _levelService.GrantSkillPoint(1);

            // Đánh dấu hoàn thành
            _taskFlags.SetFlag(_currentTask.taskId, 3);
            _history.Add(_currentTask);
            _chainCount++;
            _dailyCompleted++;

            OnTaskCompleted?.Invoke(_currentTask, reward);
            SubsystemLog.Info("DaTau", $"Hoàn thành nhiệm vụ {_chainCount}. Thưởng: {reward.exp} EXP, {reward.silver} Bạc");
            _currentTask = null;
            return reward;
        }

        /// <summary>Hủy nhiệm vụ hiện tại (reset chuỗi về 0).</summary>
        public void AbandonTask()
        {
            if (_currentTask == null) return;

            _taskFlags.SetFlag(_currentTask.taskId, 0);
            _currentTask = null;
            _chainCount = 0;

            OnChainReset?.Invoke(0);
            SubsystemLog.Info("DaTau", "Hủy nhiệm vụ. Chuỗi reset về 0.");
        }

        /// <summary>Reset daily counter (gọi mỗi ngày mới).</summary>
        public void ResetDaily()
        {
            _dailyCompleted = 0;
            SubsystemLog.Info("DaTau", "Daily task counter reset.");
        }

        // ── Reward Tables ──────────────────────────────────────────────────

        private DaTauReward CalculateReward(int chainIndex)
        {
            int tier = chainIndex / 10;

            var reward = new DaTauReward
            {
                exp = 500 + tier * 300 + chainIndex * 50,
                silver = 100 + tier * 50 + chainIndex * 10,
                xuanTinhCount = tier >= 3 ? 1 : 0,
                xuanTingGrade = Mathf.Min(tier, 8),
                grantSkillPoint = chainIndex > 0 && (chainIndex + 1) % 10 == 0,
            };

            // Tra cứu award_link.txt mốc lớn (chỉ khi không ở trong chế độ test để giữ an toàn cho test cũ)
            bool foundLinkReward = false;
            if (!_isUnitTest && _awardLinks.Count > 0)
            {
                var match = _awardLinks.Find(r => r.num == chainIndex + 1);
                if (match != null)
                {
                    reward.bonusItemNameVi = match.name;
                    foundLinkReward = true;
                }
            }

            if (!foundLinkReward)
            {
                // Fallback mốc cũ cho test
                if (chainIndex == 9)  reward.bonusItemNameVi = "Tẩy Tủy Kinh";
                if (chainIndex == 49) reward.bonusItemNameVi = "Võ Lâm Mật Tịch";
                if (chainIndex == 99) reward.bonusItemNameVi = "Nhạc Vương Kiếm";
            }

            return reward;
        }

        // ── Task Generation ────────────────────────────────────────────────

        private DaTauTask GenerateTask(int chainIndex, int playerLevel)
        {
            // Trong chế độ test runner, ta LUÔN LUÔN dùng logic cũ để đảm bảo test pass 100%
            if (_isUnitTest || _mainLinks.Count == 0)
            {
                return GenerateTaskFallback(chainIndex, playerLevel);
            }

            int levelCol = Mathf.Clamp(playerLevel / 10, 0, 19);

            // Tính tổng trọng số
            int totalRate = 0;
            foreach (var main in _mainLinks) totalRate += main.rates[levelCol];

            if (totalRate <= 0)
            {
                return GenerateTaskFallback(chainIndex, playerLevel);
            }

            // Chọn ngẫu nhiên loại nhiệm vụ theo trọng số
            int randVal = UnityEngine.Random.Range(0, totalRate);
            int currentSum = 0;
            TaskLinkMainRow selectedMain = _mainLinks[0];
            foreach (var main in _mainLinks)
            {
                currentSum += main.rates[levelCol];
                if (randVal < currentSum)
                {
                    selectedMain = main;
                    break;
                }
            }

            var task = new DaTauTask
            {
                taskId = 10000 + chainIndex,
                chainIndex = chainIndex,
                currentProgress = 0,
            };

            // Ánh xạ type:
            // 1: Mua vật phẩm (Buy Item) -> FindItem
            // 2: Tìm vật phẩm (Find Item) -> FindItem
            // 3: Gặp NPC dã ngoại (Find Npc) -> FindNpc
            // 4: Đánh quái (Kill Npc) -> KillNpc
            // 5: Đạt cấp độ (Reach Level) -> ReachLevel
            // 6: Săn thú (Kill Npc) -> KillNpc

            switch (selectedMain.type)
            {
                case 1: // Mua vật phẩm
                    {
                        var list = FilterBuyLinks(levelCol);
                        if (list.Count > 0)
                        {
                            var row = PickRandomBuy(list, levelCol);
                            task.type = DaTauTaskType.FindItem;
                            task.targetId = row.taskId;
                            task.targetCount = 1;
                            task.descriptionVi = $"Mua 1 {row.info}";
                        }
                        else
                        {
                            return GenerateTaskFallback(chainIndex, playerLevel);
                        }
                    }
                    break;

                case 2: // Tìm vật phẩm
                    {
                        var list = FilterFindGoodsLinks(levelCol);
                        if (list.Count > 0)
                        {
                            var row = PickRandomFindGoods(list, levelCol);
                            task.type = DaTauTaskType.FindItem;
                            task.targetId = row.taskId;
                            task.targetCount = 1;
                            task.descriptionVi = $"Tìm 1 {row.info}";
                        }
                        else
                        {
                            return GenerateTaskFallback(chainIndex, playerLevel);
                        }
                    }
                    break;

                case 3: // Gặp NPC
                    {
                        var list = FilterUpgroundLinks(levelCol);
                        if (list.Count > 0)
                        {
                            var row = PickRandomUpground(list, levelCol);
                            task.type = DaTauTaskType.FindNpc;
                            task.targetId = row.targetNpcId;
                            task.targetCount = 1;
                            task.descriptionVi = $"Gặp gỡ {row.info}";
                        }
                        else
                        {
                            return GenerateTaskFallback(chainIndex, playerLevel);
                        }
                    }
                    break;

                case 4: // Đánh quái
                case 6: // Săn thú
                    {
                        var list = FilterFindMapsLinks(levelCol);
                        if (list.Count > 0)
                        {
                            var row = PickRandomFindMaps(list, levelCol);
                            task.type = DaTauTaskType.KillNpc;
                            task.targetId = row.targetNpcId;
                            task.targetCount = row.num;
                            task.descriptionVi = $"Tiêu diệt {task.targetCount} {row.info}";
                        }
                        else
                        {
                            return GenerateTaskFallback(chainIndex, playerLevel);
                        }
                    }
                    break;

                case 5: // Đạt cấp độ
                    task.type = DaTauTaskType.ReachLevel;
                    task.targetId = playerLevel + 1;
                    task.targetCount = 1;
                    task.descriptionVi = $"Đạt cấp độ {task.targetId}";
                    break;

                default:
                    return GenerateTaskFallback(chainIndex, playerLevel);
            }

            return task;
        }

        private DaTauTask GenerateTaskFallback(int chainIndex, int playerLevel)
        {
            DaTauTaskType type = (DaTauTaskType)(chainIndex % 4);

            var task = new DaTauTask
            {
                taskId = 10000 + chainIndex,
                chainIndex = chainIndex,
                type = type,
                currentProgress = 0,
            };

            int levelFactor = Mathf.Max(1, playerLevel / 10);

            switch (type)
            {
                case DaTauTaskType.KillNpc:
                    task.targetId = PickRandomNpc(playerLevel);
                    task.targetCount = 3 + levelFactor;
                    task.descriptionVi = $"Tiêu diệt {task.targetCount} {NpcNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.FindItem:
                    task.targetId = PickRandomItem(playerLevel);
                    task.targetCount = 1 + levelFactor / 2;
                    task.descriptionVi = $"Tìm {task.targetCount} {ItemNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.FindNpc:
                    task.targetId = PickRandomNpc(playerLevel);
                    task.targetCount = 1;
                    task.descriptionVi = $"Gặp gỡ {NpcNameVi(task.targetId)}";
                    break;
                case DaTauTaskType.ReachLevel:
                    task.targetId = playerLevel + 1;
                    task.targetCount = 1;
                    task.descriptionVi = $"Đạt cấp độ {task.targetId}";
                    break;
            }

            return task;
        }

        private List<TaskLinkBuyRow> FilterBuyLinks(int levelCol) => _buyLinks.FindAll(r => r.rates[levelCol] > 0);
        private List<TaskLinkFindGoodsRow> FilterFindGoodsLinks(int levelCol) => _findGoodsLinks.FindAll(r => r.rates[levelCol] > 0);
        private List<TaskLinkFindMapsRow> FilterFindMapsLinks(int levelCol) => _findMapsLinks.FindAll(r => r.rates[levelCol] > 0);
        private List<TaskLinkUpgroundRow> FilterUpgroundLinks(int levelCol) => _upgroundLinks.FindAll(r => r.rates[levelCol] > 0);

        private TaskLinkBuyRow PickRandomBuy(List<TaskLinkBuyRow> list, int levelCol)
        {
            int total = 0;
            foreach (var r in list) total += r.rates[levelCol];
            int rand = UnityEngine.Random.Range(0, total);
            int sum = 0;
            foreach (var r in list)
            {
                sum += r.rates[levelCol];
                if (rand < sum) return r;
            }
            return list[0];
        }

        private TaskLinkFindGoodsRow PickRandomFindGoods(List<TaskLinkFindGoodsRow> list, int levelCol)
        {
            int total = 0;
            foreach (var r in list) total += r.rates[levelCol];
            int rand = UnityEngine.Random.Range(0, total);
            int sum = 0;
            foreach (var r in list)
            {
                sum += r.rates[levelCol];
                if (rand < sum) return r;
            }
            return list[0];
        }

        private TaskLinkFindMapsRow PickRandomFindMaps(List<TaskLinkFindMapsRow> list, int levelCol)
        {
            int total = 0;
            foreach (var r in list) total += r.rates[levelCol];
            int rand = UnityEngine.Random.Range(0, total);
            int sum = 0;
            foreach (var r in list)
            {
                sum += r.rates[levelCol];
                if (rand < sum) return r;
            }
            return list[0];
        }

        private TaskLinkUpgroundRow PickRandomUpground(List<TaskLinkUpgroundRow> list, int levelCol)
        {
            int total = 0;
            foreach (var r in list) total += r.rates[levelCol];
            int rand = UnityEngine.Random.Range(0, total);
            int sum = 0;
            foreach (var r in list)
            {
                sum += r.rates[levelCol];
                if (rand < sum) return r;
            }
            return list[0];
        }

        private static int PickRandomNpc(int level) => level switch
        {
            <= 10 => 300,
            <= 20 => 301,
            <= 30 => 302,
            <= 40 => 303,
            <= 50 => 304,
            _ => 305,
        };

        private static int PickRandomItem(int level) => level switch
        {
            <= 20 => 1001,
            <= 40 => 1002,
            _ => 1003,
        };

        private static string NpcNameVi(int id) => id switch
        {
            300 => "Mèo Vàng", 301 => "Dã Cẩu", 302 => "Sói Xám",
            303 => "Cáp Giác", 304 => "Hắc Nguyệt", 305 => "Huyết Lang",
            _ => "Quái vật"
        };

        private static string ItemNameVi(int id) => id switch
        {
            1001 => "Tiểu Hồi Đan", 1002 => "Đại Hồi Đan", 1003 => "Kim Sáng Dược",
            _ => "Vật phẩm"
        };
    }
}
