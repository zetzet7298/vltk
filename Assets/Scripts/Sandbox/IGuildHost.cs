// -----------------------------------------------------------------------------
// VLTK Mobile — IGuildHost: giao diện host cho GuildService.
// Cho phép runtime dispatch các side-effect khi tạo bang, disband, add/remove
// member, donate, upgrade (UI/chat log/notification/global news).
// PC source: script/tong/tong_mix.lua, tong.lua, tong_apply_member.lua.
// PC surfaces: Msg2Player, Msg2Tong, Msg2Faction, Msg2Global.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    public enum GuildMemberRole
    {
        Member,     // Thành viên thường
        Elder,      // Trưởng lão
        ViceLeader, // Phó bang chủ
        Leader,     // Bang chủ
    }

    /// <summary>
    /// Host-side callbacks cho GuildService. Implement bởi UI/Chat/Notify.
    /// </summary>
    public interface IGuildHost
    {
        /// <summary>Thông báo trên kênh chat thế giới khi bang được tạo (PC Msg2Global).</summary>
        void OnGuildCreated(string guildName, string founderName);

        /// <summary>Thông báo trên kênh chat hệ thống khi bang giải tán (PC Msg2Tong).</summary>
        void OnGuildDisbanded(string guildName, string leaderName);

        /// <summary>Thông báo khi có thành viên mới (PC Msg2Tong + Msg2Player).</summary>
        void OnMemberJoined(string guildName, string playerName, GuildMemberRole role);

        /// <summary>Thông báo khi thành viên rời bang (PC Msg2Tong).</summary>
        void OnMemberLeft(string guildName, string playerName, GuildMemberRole previousRole);

        /// <summary>Thông báo khi bang nâng cấp cấp (PC Msg2Tong).</summary>
        void OnGuildLevelUpgraded(string guildName, int oldLevel, int newLevel);

        /// <summary>Thông báo donate (PC Msg2Tong).</summary>
        void OnFundsDonated(string guildName, string playerName, int amount);

        /// <summary>Phát thông báo broadcast tới toàn bang (PC Msg2Tong).</summary>
        void BroadcastToTong(string guildName, string message);

        /// <summary>Trừ tiền player khi upgrade bang (PC Pay).</summary>
        bool TryDeductPlayerMoney(string playerName, int amount);
    }
}
