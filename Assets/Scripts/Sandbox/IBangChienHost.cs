// -----------------------------------------------------------------------------
// VLTK Mobile — IBangChienHost: giao diện host cho BangChienService.
// Cho phép runtime dispatch các side-effect khi Bang Chiến (công thành chiến)
// bắt đầu, kill, kết thúc, phần thưởng, log hệ thống.
// PC source: settings/battle/bangchien.txt, lua tongwar_event.
// PC surfaces: Msg2Tong, Msg2Player, broadcast, GrantMoney, AddItemEx.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho BangChienService. Implement bởi UI/Chat/Notify/Mail.
    /// </summary>
    public interface IBangChienHost
    {
        /// <summary>Thông báo Bang Chiến sắp bắt đầu (PC broadcast).</summary>
        void OnBangChienStarting(int challengerBangId, int defenderBangId);

        /// <summary>Thông báo khi ghi kill trong Bang Chiến (PC Msg2TongKill).</summary>
        void OnBangChienKill(bool isChallengerKill, int challengerScore, int defenderScore);

        /// <summary>Phần thưởng cho bang thắng / thua (PC AddMoney / AddItemEx).</summary>
        void GrantBangChienReward(int bangId, bool isWinner, int score, int cityId);

        /// <summary>Thông báo khi Bang Chiến kết thúc (PC broadcast).</summary>
        void OnBangChienEnded(int winnerBangId, int challengerScore, int defenderScore);

        /// <summary>Log thông báo Bang Chiến lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogBangChienEvent(string message);

        /// <summary>Tính thu nhập cho 1 thành sau khi sở hữu (PC AddMoney hourly).</summary>
        void GrantCityIncome(int tongId, int cityId, long amount);
    }
}
