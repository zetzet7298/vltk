// -----------------------------------------------------------------------------
// VLTK Mobile — pure PC Hồng Bao weighted-open model.
// PC source of truth (00.src-tinh-kiem only):
// - Server 6.0/server/home_jxser/server1/script/item/shenmi_hongbao.lua
// - Server 6.0/server/home_jxser/server1/script/class/kbonus.lua
// - Server 6.0/server/home_jxser/server1/settings/item/hongbao.txt
// No inventory mutation; this only exposes the command/events PC Lua would issue.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class HongbaoOpenResultService
    {
        public const int RequiredFreeItemCells = 6;
        public const int PcItemType = 1;
        public const int PcGoldenType = 2;

        private readonly List<PcHongbaoEntry> _entries = new List<PcHongbaoEntry>();

public HongbaoOpenResultService() : this(null) { }
                public HongbaoOpenResultService(IEnumerable<PcHongbaoEntry> entries)
        {
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                _entries.Add(entry);
                TotalProba += entry.proba;
                if (entry.type == PcItemType) Type1Count++;
                if (entry.type == PcGoldenType) Type2Count++;
                if (entry.costly != 0) CostlyCount++;
                if (entry.log != 0) LogCount++;
            }
        }

        public int Count => _entries.Count;
        public int TotalProba { get; private set; }
        public int Type1Count { get; private set; }
        public int Type2Count { get; private set; }
        public int CostlyCount { get; private set; }
        public int LogCount { get; private set; }

        public PcHongbaoEntry SelectByPcRoll(int roll)
        {
            if (TotalProba <= 0) return null;
            if (roll < 1 || roll > TotalProba)
                throw new ArgumentOutOfRangeException(nameof(roll), "PC random(total) returns 1..total inclusive.");

            int cumulative = 0;
            foreach (var entry in _entries)
            {
                cumulative += entry.proba;
                if (roll <= cumulative) return entry;
            }
            return null;
        }

        public HongbaoOpenResult TryOpen(int freeCells, int roll, string playerName = null)
        {
            var result = new HongbaoOpenResult
            {
                Status = HongbaoOpenStatus.NotOpened,
                FreeCells = freeCells,
                RequiredFreeCells = RequiredFreeItemCells,
                Roll = roll,
                TotalProba = TotalProba,
                RewardCommand = HongbaoRewardCommand.None,
            };

            if (freeCells < RequiredFreeItemCells)
            {
                result.Status = HongbaoOpenStatus.InsufficientInventorySpace;
                result.FailureMessageVi = "Hành trang không đủ 6 ô trống để mở Hồng Bao.";
                return result;
            }

            var selected = SelectByPcRoll(roll);
            if (selected == null)
            {
                result.Status = HongbaoOpenStatus.NoRewardSelected;
                return result;
            }

            result.Status = HongbaoOpenStatus.RewardSelected;
            result.SelectedEntry = selected;
            result.RewardCommand = HongbaoRewardCommand.FromEntry(selected);
            result.MessageTemplate = string.IsNullOrEmpty(selected.msg) ? null : selected.msg;
            result.ResolvedMessage = ConvertBonusMessage(selected, playerName);
            result.ShouldEmitGlobalNews = selected.costly != 0 && !string.IsNullOrEmpty(selected.msg);
            result.ShouldWriteLog = selected.log != 0;
            return result;
        }

        public static string ConvertBonusMessage(PcHongbaoEntry entry, string playerName)
        {
            if (entry == null || string.IsNullOrEmpty(entry.msg)) return string.Empty;
            string msg = entry.msg.Replace("<name>", entry.nameRaw ?? string.Empty);
            return string.IsNullOrEmpty(playerName) ? msg : msg.Replace("<player>", playerName);
        }
    }

    public enum HongbaoOpenStatus
    {
        NotOpened = 0,
        InsufficientInventorySpace = 1,
        NoRewardSelected = 2,
        RewardSelected = 3,
    }

    public enum HongbaoRewardCommandType
    {
        None = 0,
        AddItem = 1,
        AddGoldItem = 2,
    }

    public sealed class HongbaoOpenResult
    {
        public HongbaoOpenStatus Status;
        public int FreeCells;
        public int RequiredFreeCells;
        public int Roll;
        public int TotalProba;
        public PcHongbaoEntry SelectedEntry;
        public HongbaoRewardCommand RewardCommand;
        public string MessageTemplate;
        public string ResolvedMessage;
        public bool ShouldEmitGlobalNews;
        public bool ShouldWriteLog;
        public string FailureMessageVi;
    }

    public sealed class HongbaoRewardCommand
    {
        public static readonly HongbaoRewardCommand None = new HongbaoRewardCommand { CommandType = HongbaoRewardCommandType.None };

        public HongbaoRewardCommandType CommandType;
        public string ApiName;
        public int Genre;
        public int Detail;
        public int Particular;
        public int Level;
        public int Serise;
        public int Luck;
        public readonly int[] Params = new int[PcHongbaoParser.ParamCount];
        public int AddGoldItemFirstArg;

        public static HongbaoRewardCommand FromEntry(PcHongbaoEntry entry)
        {
            if (entry == null) return None;
            if (entry.type == HongbaoOpenResultService.PcItemType)
            {
                var command = new HongbaoRewardCommand
                {
                    CommandType = HongbaoRewardCommandType.AddItem,
                    ApiName = "AddItem",
                    Genre = entry.itemGenre,
                    Detail = entry.itemDetail,
                    Particular = entry.itemParticular,
                    Level = entry.level,
                    Serise = entry.serise,
                    Luck = 0,
                };
                for (int i = 0; i < PcHongbaoParser.ParamCount; i++) command.Params[i] = entry.param[i];
                return command;
            }

            if (entry.type == HongbaoOpenResultService.PcGoldenType)
            {
                return new HongbaoRewardCommand
                {
                    CommandType = HongbaoRewardCommandType.AddGoldItem,
                    ApiName = "AddGoldItem",
                    AddGoldItemFirstArg = 0,
                    Genre = entry.itemGenre,
                };
            }

            return None;
        }
    }
}
