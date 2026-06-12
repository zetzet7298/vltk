// -----------------------------------------------------------------------------
// VLTK Mobile — pure PC Đại Hồng Bao thành thị weighted-open model.
// PC source of truth (vl_update_27 only):
// - Server 6.0/server/home_jxser/server1/script/item/chengshidahongbao.lua
// - Server 6.0/server/home_jxser/server1/script/class/kbonus.lua
// - Server 6.0/server/home_jxser/server1/settings/item/chengshidahongbao.txt
// No inventory mutation; exposes only the command/events PC Lua would issue.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class CityHongbaoOpenResultService
    {
        public const int RequiredFreeItemCells = 6;
        public const int PcItemType = 1;
        public const int PcGoldenType = 2;

        private readonly List<PcCityHongbaoEntry> _entries = new List<PcCityHongbaoEntry>();

public CityHongbaoOpenResultService() : this(null) { }
                public CityHongbaoOpenResultService(IEnumerable<PcCityHongbaoEntry> entries)
        {
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                _entries.Add(entry);
                TotalProba += entry.Proba;
                if (entry.Type == PcItemType) Type1Count++;
                if (entry.Type == PcGoldenType) Type2Count++;
                if (entry.Costly != 0) CostlyCount++;
                if (entry.Log != 0) LogCount++;
            }
        }

        public int Count => _entries.Count;
        public int TotalProba { get; private set; }
        public int Type1Count { get; private set; }
        public int Type2Count { get; private set; }
        public int CostlyCount { get; private set; }
        public int LogCount { get; private set; }

        public PcCityHongbaoEntry SelectByPcRoll(int roll)
        {
            if (TotalProba <= 0) return null;
            if (roll < 1 || roll > TotalProba)
                throw new ArgumentOutOfRangeException(nameof(roll), "PC random(total) returns 1..total inclusive.");

            int cumulative = 0;
            foreach (var entry in _entries)
            {
                cumulative += entry.Proba;
                if (roll <= cumulative) return entry;
            }
            return null;
        }

        public CityHongbaoOpenResult TryOpen(int freeCells, int roll, string playerName = null)
        {
            var result = new CityHongbaoOpenResult
            {
                Status = CityHongbaoOpenStatus.NotOpened,
                FreeCells = freeCells,
                RequiredFreeCells = RequiredFreeItemCells,
                Roll = roll,
                TotalProba = TotalProba,
                RewardCommand = CityHongbaoRewardCommand.None,
            };

            if (freeCells < RequiredFreeItemCells)
            {
                result.Status = CityHongbaoOpenStatus.InsufficientInventorySpace;
                result.FailureMessageVi = "Hành trang không đủ 6 ô trống để mở Đại Hồng Bao thành thị.";
                return result;
            }

            var selected = SelectByPcRoll(roll);
            if (selected == null)
            {
                result.Status = CityHongbaoOpenStatus.NoRewardSelected;
                return result;
            }

            result.Status = CityHongbaoOpenStatus.RewardSelected;
            result.SelectedEntry = selected;
            result.RewardCommand = CityHongbaoRewardCommand.FromEntry(selected);
            result.MessageTemplate = string.IsNullOrEmpty(selected.Msg) ? null : selected.Msg;
            result.ResolvedMessage = ConvertBonusMessage(selected, playerName);
            result.ShouldEmitGlobalNews = selected.Costly != 0 && !string.IsNullOrEmpty(selected.Msg);
            result.ShouldWriteLog = selected.Log != 0;
            return result;
        }

        public static string ConvertBonusMessage(PcCityHongbaoEntry entry, string playerName)
        {
            if (entry == null || string.IsNullOrEmpty(entry.Msg)) return string.Empty;
            string msg = entry.Msg.Replace("<name>", entry.Name ?? string.Empty);
            return string.IsNullOrEmpty(playerName) ? msg : msg.Replace("<player>", playerName);
        }
    }

    public enum CityHongbaoOpenStatus
    {
        NotOpened = 0,
        InsufficientInventorySpace = 1,
        NoRewardSelected = 2,
        RewardSelected = 3,
    }

    public enum CityHongbaoRewardCommandType
    {
        None = 0,
        AddItem = 1,
        AddGoldItem = 2,
    }

    public sealed class CityHongbaoOpenResult
    {
        public CityHongbaoOpenStatus Status;
        public int FreeCells;
        public int RequiredFreeCells;
        public int Roll;
        public int TotalProba;
        public PcCityHongbaoEntry SelectedEntry;
        public CityHongbaoRewardCommand RewardCommand;
        public string MessageTemplate;
        public string ResolvedMessage;
        public bool ShouldEmitGlobalNews;
        public bool ShouldWriteLog;
        public string FailureMessageVi;
    }

    public sealed class CityHongbaoRewardCommand
    {
        public static readonly CityHongbaoRewardCommand None = new CityHongbaoRewardCommand { CommandType = CityHongbaoRewardCommandType.None };

        public CityHongbaoRewardCommandType CommandType;
        public string ApiName;
        public int Genre;
        public int Detail;
        public int Particular;
        public int Level;
        public int Serise;
        public int Luck;
        public readonly int[] Params = new int[PcCityHongbaoParser.ParamCount];
        public int AddGoldItemFirstArg;

        public static CityHongbaoRewardCommand FromEntry(PcCityHongbaoEntry entry)
        {
            if (entry == null) return None;
            if (entry.Type == CityHongbaoOpenResultService.PcItemType)
            {
                var command = new CityHongbaoRewardCommand
                {
                    CommandType = CityHongbaoRewardCommandType.AddItem,
                    ApiName = "AddItem",
                    Genre = entry.Genre,
                    Detail = entry.Detail,
                    Particular = entry.Particular,
                    Level = entry.Level,
                    Serise = entry.Serise,
                    Luck = 0,
                };
                for (int i = 0; i < PcCityHongbaoParser.ParamCount; i++) command.Params[i] = entry.Param[i];
                return command;
            }

            if (entry.Type == CityHongbaoOpenResultService.PcGoldenType)
            {
                return new CityHongbaoRewardCommand
                {
                    CommandType = CityHongbaoRewardCommandType.AddGoldItem,
                    ApiName = "AddGoldItem",
                    AddGoldItemFirstArg = 0,
                    Genre = entry.Genre,
                };
            }

            return None;
        }
    }
}
