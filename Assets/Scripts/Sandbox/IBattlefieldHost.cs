// -----------------------------------------------------------------------------
// VLTK Mobile — IBattlefieldHost: giao diện host cho BattlefieldService.
// Cho phép runtime dispatch các side-effect khi chiến trường Tống Kim mở/đóng,
// người chơi vào/ra, phe thắng/thua, phần thưởng, log kênh hệ thống.
// PC source: settings/battle/battlefield.txt, lua battlefield_event.
// PC surfaces: Msg2Tong, Msg2Player, SendMail, AddMoney, AddItemEx, broadcast.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho BattlefieldService. Implement bởi UI/Chat/Notify/Mail.
    /// </summary>
    public interface IBattlefieldHost
    {
        /// <summary>Thông báo chiến trường sắp mở (PC Msg2Tong + system broadcast).</summary>
        void OnBattlefieldOpening(int mapId, int minLevel, int maxLevel, long secondsUntilOpen);

        /// <summary>Phân phe cho người chơi khi vào chiến trường (1=Tống, 2=Kim).</summary>
        int AssignPlayerTeam(int mapId, int playerId, int playerFaction);

        /// <summary>Thông báo khi người chơi vào chiến trường (PC Msg2Tong).</summary>
        void OnPlayerJoinedBattlefield(int mapId, int playerId, int team, int totalPlayers);

        /// <summary>Thông báo khi người chơi rời chiến trường (chết/rời/hết giờ).</summary>
        void OnPlayerLeftBattlefield(int mapId, int playerId, int team, int remainingPlayers);

        /// <summary>Phát sự kiện kill trong chiến trường (PC Msg2TongKill).</summary>
        void OnBattlefieldKill(int mapId, int killerId, int killerTeam, int victimId, int victimTeam);

        /// <summary>Phần thưởng khi kết thúc chiến trường (PC AddMoney / AddItemEx).</summary>
        void GrantBattlefieldReward(int playerId, int team, int winningTeam, int score);

        /// <summary>Thông báo khi chiến trường kết thúc (PC broadcast).</summary>
        void OnBattlefieldEnded(int mapId, int winningTeam, int challengerScore, int defenderScore);

        /// <summary>Log thông báo chiến trường lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogBattlefieldEvent(int mapId, string message);
    }
}
