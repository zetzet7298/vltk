// -----------------------------------------------------------------------------
// VLTK Mobile — PC Task/Quest Panel Service (BtnTask / Player_Task)
// PC source: 工具控制条.ini [Task] ClassType=Player_Task, Tip=Nhiệm vụ.
// Uses QuestService (PC mission/dialog-derived quest runtime) before daily tasks.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public sealed class QuestTaskPanelSnapshot
    {
        public int activeCount;
        public int availableCount;
        public int completedCount;
        public IReadOnlyList<string> rows;
    }

    public static class QuestTaskPanelService
    {
        public static QuestTaskPanelSnapshot BuildSnapshot(QuestService questService, int playerLevel, int factionId, int mapId, DailyTaskService dailyService = null)
        {
            var rows = new List<string>();
            int activeCount = 0;
            int availableCount = 0;
            int completedCount = questService != null ? questService.CompletedQuests.Count : 0;

            rows.Add("PC [Task] Player_Task — Nhật ký nhiệm vụ");
            if (questService != null)
            {
                foreach (var pair in questService.ActiveQuests)
                {
                    activeCount++;
                    var def = questService.GetDefinition(pair.Key);
                    rows.Add($"Đang làm: {QuestName(def, pair.Key)} — {pair.Value.state}");
                    foreach (var obj in pair.Value.objectives)
                        rows.Add($"  • {obj.descriptionVi}: {obj.currentCount}/{obj.requiredCount}");
                }

                var available = questService.GetAvailableQuests(playerLevel, factionId, mapId);
                availableCount = available.Count;
                foreach (var def in available)
                    rows.Add($"Có thể nhận: {QuestName(def, def.questId)} — cấp {def.minLevel} — NPC {def.startNpcTemplateId}");
            }

            if (activeCount == 0 && availableCount == 0)
                rows.Add("Chưa có nhiệm vụ PC đang làm hoặc có thể nhận.");

            if (dailyService != null)
            {
                var daily = DailyTaskPanelService.BuildSnapshot(dailyService, 1);
                rows.Add($"Nhiệm vụ ngày: {daily.completedCount}/{daily.totalCount}");
            }

            return new QuestTaskPanelSnapshot
            {
                activeCount = activeCount,
                availableCount = availableCount,
                completedCount = completedCount,
                rows = rows,
            };
        }

        private static string QuestName(QuestDefinition def, int questId)
            => def != null && !string.IsNullOrWhiteSpace(def.nameVi) ? def.nameVi : $"Quest #{questId}";
    }
}
