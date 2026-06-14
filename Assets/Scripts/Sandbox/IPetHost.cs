// -----------------------------------------------------------------------------
// VLTK Mobile — IPetHost: giao diện host cho PetService.
// Cho phép runtime dispatch các side-effect khi pet được tạo, cho ăn,
// huấn luyện, tăng thân mật (UI pet, animation, SFX, log, save).
// PC source: Pet NPC, PetDetailEntry, lua pet_event.
// PC surfaces: UpdatePetUI, PlayPetSFX, Msg2Player, SavePetState.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PetService. Implement bởi UI/Audio/Chat/DB.
    /// </summary>
    public interface IPetHost
    {
        /// <summary>Tạo pet cho player (PC CreatePet / AddPet).</summary>
        void OnPetCreated(int playerId, int petId, string name, int hunger, int intimacy);

        /// <summary>Cho pet ăn (PC PetFeed).</summary>
        void OnPetFed(int playerId, int petId, int foodId, int newHunger);

        /// <summary>Huấn luyện skill cho pet (PC PetTrain).</summary>
        void OnPetTrained(int playerId, int petId, int skillId, int newLevel, int newExp);

        /// <summary>Pet đói không học được skill (PC PetHungryMessage).</summary>
        void OnPetHungry(int playerId, int petId, int currentHunger, int threshold);

        /// <summary>Tăng thân mật (PC PetAddIntimacy).</summary>
        void OnPetIntimacyChanged(int playerId, int petId, int newIntimacy);

        /// <summary>Phát âm thanh pet (PC PlayPetSFX).</summary>
        void PlayPetSFX(int playerId, int petId, string action);

        /// <summary>Log thông báo pet lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogPetEvent(int playerId, int petId, string message);

        /// <summary>Lưu pet state vào DB (PC SavePetState).</summary>
        void SavePetState(int playerId, int petId, int level, int exp, int hunger, int intimacy);
    }
}
