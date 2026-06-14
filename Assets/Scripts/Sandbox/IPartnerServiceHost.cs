// -----------------------------------------------------------------------------
// VLTK Mobile — IPartnerServiceHost: giao diện host cho PartnerService.
// Cho phép runtime dispatch các side-effect khi thú cưng được spawn/despawn,
// lên cấp, giảm đói, đói cùng cực (UI pet, animation, SFX, log, save).
// PC source: settings/partner/* + lua partner_event.
// PC surfaces: CreateNpcPet, RemoveNpcPet, LevelUpNotice, Msg2Player, SavePet.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PartnerService. Implement bởi UI/Audio/Chat/DB.
    /// </summary>
    public interface IPartnerServiceHost
    {
        /// <summary>Spawn thú cưng mới (PC CreateNpcPet).</summary>
        void OnPetSpawned(int petId, int templateId, int level, string nameVi, int maxHp, int currentHp);

        /// <summary>Despawn thú cưng (PC RemoveNpcPet).</summary>
        void OnPetDespawned(int petId, int templateId, string reason);

        /// <summary>Pet lên cấp (PC LevelUpNotice).</summary>
        void OnPetLevelledUp(int petId, int newLevel, int maxHp, int currentHp, int overflowExp);

        /// <summary>Pet giảm đói mỗi tick (PC HungerDecay).</summary>
        void OnPetHungerDecayed(int petId, int currentHunger);

        /// <summary>Pet đói cùng cực (PC PetStarvingMessage).</summary>
        void OnPetStarving(int petId, int templateId, int currentHunger);

        /// <summary>Cho pet ăn (PC PetFeed).</summary>
        void OnPetFed(int petId, int newHunger, int previousHunger);

        /// <summary>Phát SFX khi pet tương tác (PC PlayPetSFX).</summary>
        void PlayPetSFX(int petId, string action);

        /// <summary>Lưu pet state vào DB (PC SavePet).</summary>
        void SavePetState(int petId, int templateId, int level, int exp, int hunger, int currentHp, int maxHp);
    }
}
