// -----------------------------------------------------------------------------
// VLTK Mobile — IAchievementHost: giao diện host cho AchievementService.
// Cho phép runtime dispatch các side-effect khi thành tựu hoàn thành
// (UI icon, log chat, phần thưởng, broadcast, lưu tiến độ player).
// PC source: settings/achievement/achievement.txt, lua achievement_notify.
// PC surfaces: Msg2Player, AddItemEx, AddExp, AddMoney, broadcast.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho AchievementService. Implement bởi UI/Chat/Notify/Mail.
    /// </summary>
    public interface IAchievementHost
    {
        /// <summary>Hiển thị icon thành tựu trên UI (PC SetAchievementIcon).</summary>
        void ShowAchievementIcon(int playerId, int achievementId, bool isCompleted);

        /// <summary>Thông báo khi thành tựu vừa hoàn thành (PC broadcast + Msg2Player).</summary>
        void OnAchievementCompleted(int playerId, int achievementId, string achievementName);

        /// <summary>Phát âm thanh chúc mừng (PC PlayAchievementSFX).</summary>
        void PlayAchievementSFX(int playerId, int achievementId);

        /// <summary>Thưởng vật phẩm cho player khi hoàn thành (PC AddItemEx).</summary>
        void GrantAchievementItem(int playerId, int itemId, int count);

        /// <summary>Thưởng kinh nghiệm cho player (PC AddExp).</summary>
        void GrantAchievementExp(int playerId, int exp);

        /// <summary>Thưởng tiền cho player (PC AddMoney).</summary>
        void GrantAchievementMoney(int playerId, int money);

        /// <summary>Cộng điểm thành tựu vào bảng xếp hạng (PC AddAchievementPoints).</summary>
        void AddAchievementPoints(int playerId, int points);

        /// <summary>Lưu tiến độ thành tựu vào DB (PC SaveAchievementProgress).</summary>
        void SaveProgress(int playerId, int achievementId, long progress, bool completed);
    }
}
