// -----------------------------------------------------------------------------
// VLTK Mobile — ICityDefenceHost: giao diện host cho CityDefenceService.
// Cho phép runtime dispatch các side-effect khi wave thủ thành bắt đầu
// (spawn NPC, broadcast, log, SFX, UI notice, phần thưởng).
// PC source: settings/maps/newcitydefence/*.txt + lua wave_event.
// PC surfaces: CreateNpc, Msg2Tong, broadcast, PlayEffect, SetDefenceBuff.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho CityDefenceService. Implement bởi Combat/UI/Chat/Audio.
    /// </summary>
    public interface ICityDefenceHost
    {
        /// <summary>Spawn NPC defender cho wave (PC CreateNpc / Lua spawn).</summary>
        int SpawnDefenderNpc(int mapId, int waveIndex, int npcId, int count);

        /// <summary>Thông báo wave bắt đầu (PC Msg2Tong + broadcast).</summary>
        void OnWaveStarted(int mapId, int waveIndex, int npcCount, int waveIntervalSec);

        /// <summary>Phát âm thanh / effect cho wave bắt đầu (PC PlayEffect + SFX).</summary>
        void PlayWaveStartEffect(int mapId, int waveIndex);

        /// <summary>Thiết lập buff / thuộc tính đặc biệt cho defender (PC SetDefenceBuff).</summary>
        void SetDefenderBuff(int npcId, int mapId, int waveIndex);

        /// <summary>Hiển thị thông báo UI cho player trong bán kính (PC ShowDefenceNotice).</summary>
        void ShowDefenceNotice(int mapId, int waveIndex, int minLevel);

        /// <summary>Log thông báo wave lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogDefenceEvent(int mapId, int waveIndex, string message);

        /// <summary>Phần thưởng khi hoàn thành wave (PC AddItemEx).</summary>
        void GrantWaveReward(int playerId, int mapId, int waveIndex, int rewardId, int rewardCount);
    }
}
