// -----------------------------------------------------------------------------
// VLTK Mobile — Quest System
// PC quest database, quest states, dialogue trees, and quest tracking.
// Source: PC mission/*.txt, dialog/*.lua
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Quest type from PC mission system.</summary>
    public enum QuestType
    {
        MainStory = 0,       // Main storyline quests
        SideQuest = 1,       // Side quests from NPCs
        DailyQuest = 2,      // Repeatable daily quests
        SectQuest = 3,       // Sect/faction-specific quests
        TrainingQuest = 4,   // Training/tutorial quests
    }

    /// <summary>Quest objective type.</summary>
    public enum QuestObjectiveType
    {
        KillMonster = 0,     // Kill N monsters of type
        CollectItem = 1,     // Collect N items
        TalkToNpc = 2,       // Talk to NPC
        ReachLevel = 3,      // Reach player level
        ReachLocation = 4,   // Reach map location
        EquipItem = 5,       // Equip specific item
        LearnSkill = 6,      // Learn a skill
        DefeatBoss = 7,      // Defeat a boss monster
    }

    /// <summary>Quest objective.</summary>
    [Serializable]
    public class QuestObjective
    {
        public QuestObjectiveType type;
        public int targetId;         // Monster template ID / Item ID / NPC template ID
        public int requiredCount;
        public int currentCount;
        public string descriptionVi; // Vietnamese description

        public bool IsComplete => currentCount >= requiredCount;
        public float Progress => requiredCount > 0 ? Mathf.Clamp01((float)currentCount / requiredCount) : 0f;
    }

    /// <summary>Quest reward.</summary>
    [Serializable]
    public class QuestReward
    {
        public int exp;
        public int silver;
        public int itemId;           // 0 = no item reward
        public int itemCount;
        public int skillPoints;
    }

    /// <summary>Quest definition from PC data.</summary>
    [Serializable]
    public class QuestDefinition
    {
        public int questId;
        public string nameRaw;        // PC original name (Chinese)
        public string nameVi;         // Vietnamese name
        public string descriptionVi;  // Vietnamese description
        public QuestType type;
        public int minLevel;          // Required minimum level
        public int requiredQuestId;   // Prerequisite quest (0 = none)
        public int requiredSectId;    // Required sect/faction (0 = any)
        public int startNpcTemplateId;// NPC that gives the quest
        public int endNpcTemplateId;  // NPC that completes the quest
        public int mapId;             // Map where quest takes place
        public List<QuestObjective> objectives = new();
        public QuestReward reward = new();
        public List<string> dialogueStart = new();    // Dialogue lines when accepting
        public List<string> dialogueComplete = new(); // Dialogue lines when completing
        public List<string> dialogueProgress = new(); // Dialogue lines during progress
        public QuestSourceKind sourceKind;
        public bool isSampleQuest;
        public int pcTaskIdFirst;
        public int pcTaskIdLast;
        public int pcSyncFlag;
        public int pcClientFlag;
    }

    public enum QuestSourceKind
    {
        Unknown = 0,
        PcPlayerTaskMetadata = 1,
        Sample = 2,
    }

    /// <summary>Runtime quest state for a player.</summary>
    public enum QuestState
    {
        NotStarted = 0,
        Available = 1,      // Prerequisites met, can be accepted
        Active = 2,         // Currently in progress
        Complete = 3,       // Objectives met, can turn in
        TurnedIn = 4,       // Rewards claimed
        Failed = 5,
    }

    /// <summary>Runtime quest instance.</summary>
    public class QuestInstance
    {
        public int questId;
        public QuestState state;
        public List<QuestObjective> objectives = new();
    }

    /// <summary>
    /// Quest database and runtime state manager.
    /// Tracks active quests, objective progress, and completion.
    /// </summary>
    public class QuestService
    {
        private readonly Dictionary<int, QuestDefinition> _questDefs = new();
        private readonly Dictionary<int, QuestInstance> _activeQuests = new();
        private readonly HashSet<int> _completedQuests = new();
        private readonly HashSet<int> _failedQuests = new();

        public event Action<QuestInstance> OnQuestAccepted;
        public event Action<QuestInstance> OnQuestObjectiveUpdated;
        public event Action<QuestInstance> OnQuestCompleted;
        public event Action<int, QuestReward> OnQuestRewardClaimed;

        public IReadOnlyDictionary<int, QuestDefinition> AllQuests => _questDefs;
        public IReadOnlyDictionary<int, QuestInstance> ActiveQuests => _activeQuests;
        public IReadOnlyCollection<int> CompletedQuests => _completedQuests;

        public QuestService(bool includeSampleQuests = false)
        {
            LoadPcPlayerTaskMetadata();
            if (includeSampleQuests)
                LoadBuiltInQuests();
        }

        // ── Quest Database ──────────────────────────────────────────────

        public QuestDefinition GetDefinition(int questId)
        {
            _questDefs.TryGetValue(questId, out var def);
            return def;
        }

        public List<QuestDefinition> GetAvailableQuests(int playerLevel, int sectId, int mapId)
        {
            var results = new List<QuestDefinition>();
            foreach (var def in _questDefs.Values)
            {
                if (_completedQuests.Contains(def.questId)) continue;
                if (_activeQuests.ContainsKey(def.questId)) continue;
                if (playerLevel < def.minLevel) continue;
                if (def.requiredSectId > 0 && def.requiredSectId != sectId) continue;
                if (def.requiredQuestId > 0 && !_completedQuests.Contains(def.requiredQuestId)) continue;
                results.Add(def);
            }
            results.Sort((a, b) => a.questId.CompareTo(b.questId));
            return results;
        }

        public List<QuestDefinition> GetQuestsForNpc(int npcTemplateId)
        {
            var results = new List<QuestDefinition>();
            foreach (var def in _questDefs.Values)
            {
                if (def.startNpcTemplateId == npcTemplateId || def.endNpcTemplateId == npcTemplateId)
                    results.Add(def);
            }
            return results;
        }

        // ── Quest State Management ──────────────────────────────────────

        public QuestInstance AcceptQuest(int questId)
        {
            var def = GetDefinition(questId);
            if (def == null)
            {
                SubsystemLog.Warn("Quest", $"Quest {questId} not found");
                return null;
            }
            if (_activeQuests.ContainsKey(questId) || _completedQuests.Contains(questId))
            {
                SubsystemLog.Warn("Quest", $"Quest {questId} already active/completed");
                return null;
            }

            var instance = new QuestInstance
            {
                questId = questId,
                state = QuestState.Active,
            };

            // Deep copy objectives
            foreach (var obj in def.objectives)
            {
                instance.objectives.Add(new QuestObjective
                {
                    type = obj.type,
                    targetId = obj.targetId,
                    requiredCount = obj.requiredCount,
                    currentCount = 0,
                    descriptionVi = obj.descriptionVi,
                });
            }

            _activeQuests[questId] = instance;
            OnQuestAccepted?.Invoke(instance);
            SubsystemLog.Info("Quest", $"Accepted: {def.nameVi} (id={questId})");
            return instance;
        }

        public void UpdateKillObjective(int monsterTemplateId, int count = 1)
        {
            foreach (var qi in _activeQuests.Values)
            {
                if (qi.state != QuestState.Active) continue;
                foreach (var obj in qi.objectives)
                {
                    if (obj.type == QuestObjectiveType.KillMonster &&
                        obj.targetId == monsterTemplateId &&
                        !obj.IsComplete)
                    {
                        obj.currentCount = Math.Min(obj.currentCount + count, obj.requiredCount);
                        OnQuestObjectiveUpdated?.Invoke(qi);
                        CheckCompletion(qi);
                    }
                }
            }
        }

        public void UpdateCollectObjective(int itemId, int count = 1)
        {
            foreach (var qi in _activeQuests.Values)
            {
                if (qi.state != QuestState.Active) continue;
                foreach (var obj in qi.objectives)
                {
                    if (obj.type == QuestObjectiveType.CollectItem &&
                        obj.targetId == itemId &&
                        !obj.IsComplete)
                    {
                        obj.currentCount = Math.Min(obj.currentCount + count, obj.requiredCount);
                        OnQuestObjectiveUpdated?.Invoke(qi);
                        CheckCompletion(qi);
                    }
                }
            }
        }

        public void UpdateTalkObjective(int npcTemplateId)
        {
            foreach (var qi in _activeQuests.Values)
            {
                if (qi.state != QuestState.Active) continue;
                foreach (var obj in qi.objectives)
                {
                    if (obj.type == QuestObjectiveType.TalkToNpc &&
                        obj.targetId == npcTemplateId &&
                        !obj.IsComplete)
                    {
                        obj.currentCount = obj.requiredCount;
                        OnQuestObjectiveUpdated?.Invoke(qi);
                        CheckCompletion(qi);
                    }
                }
            }
        }

        public QuestReward CompleteQuest(int questId)
        {
            if (!_activeQuests.TryGetValue(questId, out var instance)) return null;
            if (instance.state != QuestState.Complete) return null;

            var def = GetDefinition(questId);
            if (def == null) return null;

            instance.state = QuestState.TurnedIn;
            _activeQuests.Remove(questId);
            _completedQuests.Add(questId);

            OnQuestRewardClaimed?.Invoke(questId, def.reward);
            SubsystemLog.Info("Quest",
                $"Completed: {def.nameVi} → +{def.reward.exp}EXP +{def.reward.silver}Bạc");
            return def.reward;
        }

        public QuestState GetQuestState(int questId)
        {
            if (_completedQuests.Contains(questId)) return QuestState.TurnedIn;
            if (_failedQuests.Contains(questId)) return QuestState.Failed;
            if (_activeQuests.TryGetValue(questId, out var qi)) return qi.state;
            return QuestState.NotStarted;
        }

        private void CheckCompletion(QuestInstance qi)
        {
            bool allDone = true;
            foreach (var obj in qi.objectives)
            {
                if (!obj.IsComplete) { allDone = false; break; }
            }
            if (allDone)
            {
                qi.state = QuestState.Complete;
                OnQuestCompleted?.Invoke(qi);
                SubsystemLog.Info("Quest", $"Quest {qi.questId} objectives complete — ready to turn in");
            }
        }

        // ── Built-in Quest Catalog ──────────────────────────────────────

        public const string PcMissionRelativeDir = "Reference/PcMission";

        public static string ResolvePcMissionDirectory(string streamingAssetsPath)
            => Path.Combine(streamingAssetsPath ?? string.Empty, PcMissionRelativeDir);

        public void LoadPcPlayerTaskMetadata(string pcMissionDir = null)
        {
            string dir = string.IsNullOrEmpty(pcMissionDir)
                ? ResolvePcMissionDirectory(Application.streamingAssetsPath)
                : pcMissionDir;
            LoadPcPlayerTaskMetadata(PcMissionParser.BuildRegistry(dir));
        }

        public void LoadPcPlayerTaskMetadata(PcMissionRegistry registry)
        {
            if (registry == null) return;
            foreach (var entry in registry.All)
            {
                if (entry == null || entry.taskIdFirst <= 0) continue;
                var def = CreatePcTaskMetadataQuest(entry);
                _questDefs[def.questId] = def;
            }
        }

        private static QuestDefinition CreatePcTaskMetadataQuest(PcMissionEntry entry)
        {
            string name = string.IsNullOrWhiteSpace(entry.nameRaw)
                ? $"PC Task {entry.taskIdFirst}"
                : entry.nameRaw;
            return new QuestDefinition
            {
                questId = entry.taskIdFirst,
                nameRaw = entry.nameRaw,
                nameVi = name,
                descriptionVi = entry.describe ?? string.Empty,
                type = QuestType.SideQuest,
                minLevel = 1,
                pcTaskIdFirst = entry.taskIdFirst,
                pcTaskIdLast = entry.taskIdLast,
                pcSyncFlag = entry.syncFlag,
                pcClientFlag = entry.clientFlag,
                sourceKind = QuestSourceKind.PcPlayerTaskMetadata,
                isSampleQuest = false,
            };
        }

        private void LoadBuiltInQuests()
        {
            // Training Quests (Ba Lăng)
            AddQuest(1001, "Tập Luyện Cơ Bản", "Tiêu diệt 5 Mèo vàng ở Ba Lăng Huyện",
                QuestType.TrainingQuest, 1, 0, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.KillMonster, 31, 5, "Tiêu diệt Mèo vàng (0/5)") },
                Reward(50, 100, 0, 0, 1),
                new[] { "Chào tiểu hữu! Ta là Võ Sư. Hãy chứng minh bản lĩnh bằng cách tiêu diệt 5 con Mèo vàng." },
                new[] { "Tốt lắm! Ngươi đã hoàn thành bài kiểm tra đầu tiên." });

            AddQuest(1002, "Săn Hươu Đốm", "Tiêu diệt 8 Hươu đốm ở Ba Lăng",
                QuestType.TrainingQuest, 3, 1001, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.KillMonster, 42, 8, "Tiêu diệt Hươu đốm (0/8)") },
                Reward(80, 200, 7001, 3, 1),
                new[] { "Hươu đốm đang phá hoại nông trại. Hãy giúp dân làng tiêu diệt 8 con." },
                new[] { "Dân làng rất biết ơn! Đây là phần thưởng cho ngươi." });

            AddQuest(1003, "Dọn Sạch Heo Rừng", "Tiêu diệt 10 Heo trắng",
                QuestType.TrainingQuest, 5, 1002, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.KillMonster, 43, 10, "Tiêu diệt Heo trắng (0/10)") },
                Reward(120, 350, 2001, 1, 2),
                new[] { "Heo trắng ngày càng đông. Tiêu diệt 10 con để bảo vệ làng." },
                new[] { "Làng đã an toàn hơn. Nhận lấy áo giáp phần thưởng!" });

            // Main Story Quests
            AddQuest(2001, "Hành Trình Võ Lâm", "Đến Giang Tân Thôn gặp Cố lão",
                QuestType.MainStory, 10, 1003, 0, 311, 0, MapPortManifest.GiangTanThonId,
                new[] { Obj(QuestObjectiveType.ReachLocation, MapPortManifest.GiangTanThonId, 1, "Đến Giang Tân Thôn") },
                Reward(200, 500, 0, 0, 3),
                new[] { "Thế giới bên ngoài rất rộng lớn. Hãy đến Giang Tân Thôn để bắt đầu hành trình." },
                new[] { "Ngươi đã đến rồi! Giang Tân Thôn cần người như ngươi." });

            AddQuest(2002, "Sói Xám Quấy Rối", "Tiêu diệt 12 Sói xám ở Giang Tân",
                QuestType.MainStory, 10, 2001, 0, 0, 0, MapPortManifest.GiangTanThonId,
                new[] { Obj(QuestObjectiveType.KillMonster, 37, 12, "Tiêu diệt Sói xám (0/12)") },
                Reward(250, 600, 1001, 1, 2),
                new[] { "Bầy sói xám đang quấy rối dân làng. Tiêu diệt 12 con giúp chúng ta." },
                new[] { "Bầy sói đã bị đẩy lùi. Nhận thanh kiếm này!" });

            AddQuest(2003, "Rắn Độc Khu Vực Đông", "Tiêu diệt 15 Rắn độc",
                QuestType.SideQuest, 12, 2002, 0, 0, 0, MapPortManifest.GiangTanThonId,
                new[] { Obj(QuestObjectiveType.KillMonster, 39, 15, "Tiêu diệt Rắn độc (0/15)") },
                Reward(300, 800, 7002, 5, 2),
                new[] { "Rắn độc xuất hiện nhiều ở khu vực đông. Hãy dọn sạch giúp chúng ta." },
                new[] { "Khu vực đã an toàn rồi!" });

            // Tương Dương quests
            AddQuest(3001, "Hổ Dữ Xuất Hiện", "Tiêu diệt 5 Hổ dữ ở Tương Dương",
                QuestType.MainStory, 15, 2003, 0, 0, 0, MapPortManifest.TuongDuongId,
                new[] { Obj(QuestObjectiveType.KillMonster, 50, 5, "Tiêu diệt Hổ (0/5)") },
                Reward(500, 1200, 2002, 1, 3),
                new[] { "Hổ dữ hoành hành ở Tương Dương. Hãy giúp dân chúng!" },
                new[] { "Ngươi thật dũng cảm! Nhận lấy giáp da này." });

            // Thành Đô quests
            AddQuest(4001, "Cướp Núi", "Tiêu diệt 10 Cướp núi quanh Thành Đô",
                QuestType.MainStory, 20, 3001, 0, 0, 0, MapPortManifest.ThanhDoId,
                new[] { Obj(QuestObjectiveType.KillMonster, 55, 10, "Tiêu diệt Cướp núi (0/10)") },
                Reward(800, 2000, 1003, 1, 4),
                new[] { "Cướp núi cướp bóc quanh Thành Đô. Tiêu diệt 10 tên!" },
                new[] { "Thành Đô an toàn hơn rồi. Nhận thanh kiếm thanh phong!" });

            // Đại Lý quests
            AddQuest(5001, "Voi Giận Dữ", "Tiêu diệt 3 Voi ở Đại Lý",
                QuestType.SideQuest, 25, 4001, 0, 0, 0, MapPortManifest.DaiLyId,
                new[] { Obj(QuestObjectiveType.KillMonster, 60, 3, "Tiêu diệt Voi (0/3)") },
                Reward(1000, 3000, 3002, 1, 5),
                new[] { "Voi rừng đang phá hoại đường phố Đại Lý." },
                new[] { "Đường phố đã thông. Nhận mũ sắt bảo vệ!" });

            // Sect-specific quests
            AddQuest(6001, "Thử Thách Cái Bang", "Tiêu diệt 20 quái vật bất kỳ để gia nhập Cái Bang",
                QuestType.SectQuest, 10, 1003, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.KillMonster, 0, 20, "Tiêu diệt quái vật (0/20)") },
                Reward(500, 1000, 0, 0, 5),
                new[] { "Muốn gia nhập Cái Bang? Chứng minh thực lực bằng cách tiêu diệt 20 quái vật!" },
                new[] { "Ngươi đã được chấp nhận vào Cái Bang!" });

            // Daily quests
            AddQuest(7001, "[Hàng Ngày] Dọn Quái Ba Lăng", "Tiêu diệt 30 quái ở Ba Lăng",
                QuestType.DailyQuest, 1, 0, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.KillMonster, 0, 30, "Tiêu diệt quái vật (0/30)") },
                Reward(200, 500, 7001, 5, 1),
                new[] { "Hàng ngày cần dọn dẹp quái vật. Hãy giúp đỡ!" },
                new[] { "Cảm ơn ngươi! Mai lại tiếp tục nhé." });

            AddQuest(7002, "[Hàng Ngày] Thu Thập Dược Liệu", "Thu thập 10 Thuốc Hồi Máu",
                QuestType.DailyQuest, 1, 0, 0, 311, 311, MapPortManifest.BaLangHuyenId,
                new[] { Obj(QuestObjectiveType.CollectItem, 7001, 10, "Thu thập Thuốc Hồi Máu (0/10)") },
                Reward(150, 400, 0, 0, 1),
                new[] { "Thu thập 10 Thuốc Hồi Máu từ quái vật để chữa bệnh cho dân." },
                new[] { "Cảm ơn ngươi!" });
        }

        private void AddQuest(int id, string name, string desc, QuestType type,
            int minLevel, int requiredQuest, int requiredSect,
            int startNpc, int endNpc, int mapId,
            QuestObjective[] objectives, QuestReward reward,
            string[] startDialogue, string[] completeDialogue)
        {
            var def = new QuestDefinition
            {
                questId = id,
                nameRaw = name,
                nameVi = name,
                descriptionVi = desc,
                type = type,
                minLevel = minLevel,
                requiredQuestId = requiredQuest,
                requiredSectId = requiredSect,
                startNpcTemplateId = startNpc,
                endNpcTemplateId = endNpc,
                mapId = mapId,
                reward = reward,
                sourceKind = QuestSourceKind.Sample,
                isSampleQuest = true,
            };

            if (objectives != null)
                def.objectives.AddRange(objectives);
            if (startDialogue != null)
                def.dialogueStart.AddRange(startDialogue);
            if (completeDialogue != null)
                def.dialogueComplete.AddRange(completeDialogue);

            _questDefs[id] = def;
        }

        private static QuestObjective Obj(QuestObjectiveType type, int target, int count, string desc)
            => new() { type = type, targetId = target, requiredCount = count, currentCount = 0, descriptionVi = desc };

        private static QuestReward Reward(int exp, int silver, int itemId, int itemCount, int skillPoints)
            => new() { exp = exp, silver = silver, itemId = itemId, itemCount = itemCount, skillPoints = skillPoints };
    }
}
