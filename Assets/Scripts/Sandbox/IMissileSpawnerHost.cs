// -----------------------------------------------------------------------------
// VLTK Mobile — IMissileSpawnerHost: giao diện host cho MissileSpawner.
// Cho phép runtime dispatch các side-effect khi spawn đạn (UI skill, SFX, log).
// PC source: PcMissles.txt Speed, LifeTime + KMissle::Activate.
// PC surfaces: SpawnMissileSFX, Msg2Player, UpdateSkillUI, SaveMissileLog.
// -----------------------------------------------------------------------------

using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho MissileSpawner. Implement bởi UI/Audio/DB.
    /// </summary>
    public interface IMissileSpawnerHost
    {
        /// <summary>Spawn đạn bắt đầu (PC OnMissileSpawnStart).</summary>
        void OnSpawnStart(int skillId, int childCount, SkillMissileForm form);

        /// <summary>Spawn đạn hoàn thành (PC OnMissileSpawnComplete).</summary>
        void OnSpawnComplete(int skillId, int spawnedCount, float speed, float duration);

        /// <summary>Đạn bắn trúng đích (PC OnMissileHit).</summary>
        void OnMissileHit(int missileId, int targetActorId, int damage);

        /// <summary>Tất cả đạn trong fan/surround đã được tạo (PC OnMissileBatchSpawned).</summary>
        void OnMissileBatchSpawned(int skillId, int missileCount, SkillMissileForm form);

        /// <summary>Hiển thị UI skill effect (PC ShowSkillEffect).</summary>
        void ShowSkillEffect(int skillId, SkillMissileForm form);

        /// <summary>Log thông báo spawn đạn lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogMissileEvent(int skillId, int missileCount, SkillMissileForm form);

        /// <summary>Phát SFX khi bắn đạn (PC PlayMissileSFX).</summary>
        void PlayMissileSFX(int skillId, SkillMissileForm form);

        /// <summary>Lưu log spawn đạn vào DB (PC SaveMissileLog).</summary>
        void SaveMissileLog(int skillId, int missileCount, SkillMissileForm form);
    }
}
