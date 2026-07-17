// -----------------------------------------------------------------------------
// VLTK Mobile — HUD user-facing art catalog
// Keeps Chinese PC HUD sprites out of visible mobile UI by resolving baked text
// sprites to Vietnamese variants or localization keys.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.UI
{
    public readonly struct HudUserFacingArtReplacement
    {
        public readonly string chineseArtName;
        public readonly string vietnameseArtName;
        public readonly string localizationKey;
        public readonly string vietnameseText;
        public readonly bool usesTextOverlay;

        public HudUserFacingArtReplacement(string chineseArtName, string vietnameseArtName, string localizationKey, string vietnameseText, bool usesTextOverlay = false)
        {
            this.chineseArtName = chineseArtName;
            this.vietnameseArtName = vietnameseArtName;
            this.localizationKey = localizationKey;
            this.vietnameseText = vietnameseText;
            this.usesTextOverlay = usesTextOverlay;
        }

        public bool HasVietnameseAsset => !string.IsNullOrEmpty(vietnameseArtName);
        public bool HasLocalizationKey => !string.IsNullOrEmpty(localizationKey);
    }

    public static class HudUserFacingArtCatalog
    {
        public const string VietnameseFolderName = "vi";

        private static readonly Dictionary<string, HudUserFacingArtReplacement> Replacements = new()
        {
            { "技能", Entry("技能", "skill_panel_vi", "hud.skill.panel.title", "Kỹ năng võ công") },
            { "技能－战斗分页", Entry("技能－战斗分页", "skill_panel_combat_tab_vi", "hud.skill.tab.combat", "Chiến đấu") },
            { "技能－关闭_00", Entry("技能－关闭_00", "btn_close_skill_00_vi", "hud.action.close", "Đóng") },
            { "技能－关闭_01", Entry("技能－关闭_01", "btn_close_skill_01_vi", "hud.action.close", "Đóng") },
            { "技能－关闭_02", Entry("技能－关闭_02", "btn_close_skill_02_vi", "hud.action.close", "Đóng") },
            { "关闭_00", Entry("关闭_00", "btn_close_00_vi", "hud.action.close", "Đóng") },
            { "关闭_01", Entry("关闭_01", "btn_close_01_vi", "hud.action.close", "Đóng") },
            { "关闭_02", Entry("关闭_02", "btn_close_02_vi", "hud.action.close", "Đóng") },
            { "刷新列表_00", Entry("刷新列表_00", "btn_refresh_00_vi", "hud.action.refresh", "Làm mới") },
            { "刷新列表_01", Entry("刷新列表_01", "btn_refresh_01_vi", "hud.action.refresh", "Làm mới") },
            { "刷新列表_02", Entry("刷新列表_02", "btn_refresh_02_vi", "hud.action.refresh", "Làm mới") },
            { "刷新列表_03", Entry("刷新列表_03", "btn_refresh_03_vi", "hud.action.refresh", "Làm mới") },
            { "好友－关闭_00", Entry("好友－关闭_00", "btn_friend_close_00_vi", "hud.friend.close", "Đóng") },
            { "好友－关闭_01", Entry("好友－关闭_01", "btn_friend_close_01_vi", "hud.friend.close", "Đóng") },
            { "好友－关闭_02", Entry("好友－关闭_02", "btn_friend_close_02_vi", "hud.friend.close", "Đóng") },
            { "好友－查找_00", Entry("好友－查找_00", "btn_friend_find_00_vi", "hud.friend.find", "Tìm") },
            { "好友－查找_01", Entry("好友－查找_01", "btn_friend_find_01_vi", "hud.friend.find", "Tìm") },
            { "好友－查找_02", Entry("好友－查找_02", "btn_friend_find_02_vi", "hud.friend.find", "Tìm") },
            { "帮派－关闭_00", Entry("帮派－关闭_00", "btn_guild_close_00_vi", "hud.guild.close", "Đóng") },
            { "帮派－关闭_01", Entry("帮派－关闭_01", "btn_guild_close_01_vi", "hud.guild.close", "Đóng") },
            { "帮派－关闭_02", Entry("帮派－关闭_02", "btn_guild_close_02_vi", "hud.guild.close", "Đóng") },
            { "离开队伍_00", Entry("离开队伍_00", "btn_leave_team_00_vi", "hud.team.leave", "Rời đội") },
            { "离开队伍_01", Entry("离开队伍_01", "btn_leave_team_01_vi", "hud.team.leave", "Rời đội") },
            { "离开队伍_02", Entry("离开队伍_02", "btn_leave_team_02_vi", "hud.team.leave", "Rời đội") },
            { "离开队伍_03", Entry("离开队伍_03", "btn_leave_team_03_vi", "hud.team.leave", "Rời đội") },
            { "踢出队伍_00", Entry("踢出队伍_00", "btn_kick_team_00_vi", "hud.team.kick", "Mời ra") },
            { "踢出队伍_01", Entry("踢出队伍_01", "btn_kick_team_01_vi", "hud.team.kick", "Mời ra") },
            { "踢出队伍_02", Entry("踢出队伍_02", "btn_kick_team_02_vi", "hud.team.kick", "Mời ra") },
            { "踢出队伍_03", Entry("踢出队伍_03", "btn_kick_team_03_vi", "hud.team.kick", "Mời ra") },
            { "邀请加入_00", Entry("邀请加入_00", "btn_invite_team_00_vi", "hud.team.invite", "Mời") },
            { "邀请加入_01", Entry("邀请加入_01", "btn_invite_team_01_vi", "hud.team.invite", "Mời") },
            { "邀请加入_02", Entry("邀请加入_02", "btn_invite_team_02_vi", "hud.team.invite", "Mời") },
            { "邀请加入_03", Entry("邀请加入_03", "btn_invite_team_03_vi", "hud.team.invite", "Mời") },
            { "队长移交_00", Entry("队长移交_00", "btn_transfer_leader_00_vi", "hud.team.transfer_leader", "Đội trưởng") },
            { "队长移交_01", Entry("队长移交_01", "btn_transfer_leader_01_vi", "hud.team.transfer_leader", "Đội trưởng") },
            { "队长移交_02", Entry("队长移交_02", "btn_transfer_leader_02_vi", "hud.team.transfer_leader", "Đội trưởng") },
            { "队长移交_03", Entry("队长移交_03", "btn_transfer_leader_03_vi", "hud.team.transfer_leader", "Đội trưởng") },
            { "技能－战斗分页－一页_00", Entry("技能－战斗分页－一页_00", "skill_page_one_00_vi", "hud.skill.page.one", "Trang 1") },
            { "技能－战斗分页－一页_01", Entry("技能－战斗分页－一页_01", "skill_page_one_01_vi", "hud.skill.page.one", "Trang 1") },
            { "技能－战斗分页－二页_00", Entry("技能－战斗分页－二页_00", "skill_page_two_00_vi", "hud.skill.page.two", "Trang 2") },
            { "技能－战斗分页－二页_01", Entry("技能－战斗分页－二页_01", "skill_page_two_01_vi", "hud.skill.page.two", "Trang 2") },
            { "技能－战斗技能按钮_00", Entry("技能－战斗技能按钮_00", "skill_fight_tab_00_vi", "hud.skill.tab.combat", "Chiến đấu") },
            { "技能－战斗技能按钮_01", Entry("技能－战斗技能按钮_01", "skill_fight_tab_01_vi", "hud.skill.tab.combat", "Chiến đấu") },
            { "技能－生活技能按钮_00", Entry("技能－生活技能按钮_00", "skill_life_tab_00_vi", "hud.skill.tab.life", "Sinh hoạt") },
            { "技能－生活技能按钮_01", Entry("技能－生活技能按钮_01", "skill_life_tab_01_vi", "hud.skill.tab.life", "Sinh hoạt") },
            { "主界面按钮-GM频道选择", Entry("主界面按钮-GM频道选择", "chat_main_gm_vi", "hud.chat.channel.gm", "GM") },
            { "主界面按钮-世界频道选择", Entry("主界面按钮-世界频道选择", "chat_main_world_vi", "hud.chat.channel.world", "Thế giới") },
            { "主界面按钮-城市频道选择", Entry("主界面按钮-城市频道选择", "chat_main_city_vi", "hud.chat.channel.city", "Thành") },
            { "主界面按钮-好友频道选择", Entry("主界面按钮-好友频道选择", "chat_main_friend_vi", "hud.chat.channel.friend", "Bạn") },
            { "主界面按钮-密人频道选择", Entry("主界面按钮-密人频道选择", "chat_main_private_vi", "hud.chat.channel.private", "Mật") },
            { "主界面按钮-门派频道选择", Entry("主界面按钮-门派频道选择", "chat_main_faction_vi", "hud.chat.channel.faction", "Môn phái") },
            { "主界面按钮-队伍频道选择", Entry("主界面按钮-队伍频道选择", "chat_main_team_vi", "hud.chat.channel.team", "Đội") },
            { "主界面按钮-附近频道选择", Entry("主界面按钮-附近频道选择", "chat_main_nearby_vi", "hud.chat.channel.nearby", "Gần") },
            { "聊天频道图示－GM频道", Entry("聊天频道图示－GM频道", "chat_icon_gm_vi", "hud.chat.channel.gm", "GM") },
            { "聊天频道图示－世界频道", Entry("聊天频道图示－世界频道", "chat_icon_world_vi", "hud.chat.channel.world", "Thế giới") },
            { "聊天频道图示－城市频道", Entry("聊天频道图示－城市频道", "chat_icon_city_vi", "hud.chat.channel.city", "Thành") },
            { "聊天频道图示－好友频道", Entry("聊天频道图示－好友频道", "chat_icon_friend_vi", "hud.chat.channel.friend", "Bạn") },
            { "聊天频道图示－密人频道", Entry("聊天频道图示－密人频道", "chat_icon_private_vi", "hud.chat.channel.private", "Mật") },
            { "聊天频道图示－自己说", Entry("聊天频道图示－自己说", "chat_icon_self_vi", "hud.chat.channel.self", "Tôi") },
            { "聊天频道图示－门派频道", Entry("聊天频道图示－门派频道", "chat_icon_faction_vi", "hud.chat.channel.faction", "Môn phái") },
            { "聊天频道图示－队伍频道", Entry("聊天频道图示－队伍频道", "chat_icon_team_vi", "hud.chat.channel.team", "Đội") },
            { "聊天频道图示－附近频道", Entry("聊天频道图示－附近频道", "chat_icon_nearby_vi", "hud.chat.channel.nearby", "Gần") },
        };

        public static IReadOnlyDictionary<string, HudUserFacingArtReplacement> All => Replacements;

        public static bool TryGetReplacement(string artName, out HudUserFacingArtReplacement replacement)
        {
            var key = NormalizeArtName(artName);
            return Replacements.TryGetValue(key, out replacement);
        }

        public static string ResolveVietnameseArtName(string artName)
        {
            return TryGetReplacement(artName, out var replacement) && replacement.HasVietnameseAsset
                ? VietnameseFolderName + "/" + replacement.vietnameseArtName
                : artName;
        }

        public static bool ContainsCjk(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            foreach (var ch in value)
            {
                if (ch >= '\u4e00' && ch <= '\u9fff')
                    return true;
            }
            return false;
        }

        private static HudUserFacingArtReplacement Entry(string chineseArtName, string vietnameseArtName, string localizationKey, string vietnameseText, bool usesTextOverlay = false)
            => new(chineseArtName, vietnameseArtName, localizationKey, vietnameseText, usesTextOverlay);

        private static string NormalizeArtName(string artName)
        {
            if (string.IsNullOrEmpty(artName))
                return string.Empty;

            return artName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)
                ? artName.Substring(0, artName.Length - 4)
                : artName;
        }
    }
}
