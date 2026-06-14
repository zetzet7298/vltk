// -----------------------------------------------------------------------------
// VLTK Mobile — IBattleAwardHost: giao diện host cho BattleAwardService.
// Cho phép runtime dispatch các side-effect khi player nhận phần thưởng
// chiến đấu theo battleType + rank (UI minimap, log, broadcast, SFX, save).
// PC source: settings/battleaward.txt + lua battle_award_event.
// PC surfaces: AddItemEx, AddMoney, broadcast, Msg2Player, PlayAwardSFX.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho BattleAwardService. Implement bởi UI/Chat/Audio.
    /// </summary>
    public interface IBattleAwardHost
    {
        /// <summary>Thông báo khi player nhận phần thưởng chiến đấu (PC OnAwardReceived).</summary>
        void OnAwardReceived(int playerId, int awardId, int battleType, int rank, int rewardSilver, int rewardExp, int rewardItem);

        /// <summary>Phát SFX khi nhận thưởng (PC PlayAwardSFX).</summary>
        void PlayAwardSFX(int playerId, int battleType, int rank);

        /// <summary>Hiển thị thông báo UI cho player (PC ShowAwardNotice).</summary>
        void ShowAwardNotice(int playerId, int battleType, int rank, int rewardSilver, int rewardExp);

        /// <summary>Broadcast khi player đạt top rank cao (PC broadcast).</summary>
        void BroadcastTopRank(int playerId, int battleType, int rank);

        /// <summary>Cộng bạc vào wallet (PC AddMoney).</summary>
        void GrantSilver(int playerId, int silver);

        /// <summary>Cộng exp vào player (PC AddExp).</summary>
        void GrantExp(int playerId, int exp);

        /// <summary>Thưởng vật phẩm (PC AddItemEx).</summary>
        void GrantItem(int playerId, int itemId, int count);

        /// <summary>Lưu thông tin award vào DB (PC SaveAwardHistory).</summary>
        void SaveAwardHistory(int playerId, int awardId, int battleType, int rank, long timestamp);
    }
}
