// -----------------------------------------------------------------------------
// VLTK Mobile — IPlayerEquipmentHost: giao diện host cho PlayerEquipmentService.
// Cho phép runtime dispatch các side-effect khi equipment thay đổi
// (refresh SPR visual, audio, log, broadcast khi đổi vũ khí lớn, save).
// PC source: NpcRes/npcres/man + npcres/woman, 男主角贴图顺序表.txt.
// PC surfaces: SetNpcRes, PlayEquipSFX, SaveEquip, Msg2Player.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PlayerEquipmentService. Implement bởi Visual/Audio/Chat.
    /// </summary>
    public interface IPlayerEquipmentHost
    {
        /// <summary>Refresh SPR visual layer sau khi equip thay đổi (PC SetNpcRes / ChangeBody).</summary>
        void RefreshVisual(PlayerEquipSlot slot, int oldVariant, int newVariant, int itemId);

        /// <summary>Phát âm thanh khi equip / unequip (PC PlayEquipSFX).</summary>
        void PlayEquipSFX(PlayerEquipSlot slot, int itemId);

        /// <summary>Thông báo khi vũ khí chính thay đổi (PC broadcast).</summary>
        void OnWeaponChanged(int oldItemId, int newItemId, int newVariant);

        /// <summary>Thông báo khi armor body thay đổi (PC broadcast).</summary>
        void OnArmorChanged(int oldVariant, int newVariant, int itemId);

        /// <summary>Thông báo khi helmet thay đổi (PC broadcast).</summary>
        void OnHelmetChanged(int oldVariant, int newVariant, int itemId);

        /// <summary>Thông báo khi mount (ngựa) thay đổi (PC RideHorse).</summary>
        void OnMountChanged(int oldVariant, int newVariant, int itemId);

        /// <summary>Log thông báo equipment lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogEquipEvent(PlayerEquipSlot slot, int oldVariant, int newVariant, int itemId);

        /// <summary>Lưu equipment vào DB player (PC SaveEquip).</summary>
        void SaveEquipmentState(int itemId, PlayerEquipSlot slot, int variant);
    }
}
