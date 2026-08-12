using System.Collections.Generic;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class HongbaoRuntimeBehaviorService
    {
        private readonly InventoryService _inventory;
        private readonly IHongbaoRuntimeHost _host;
        private readonly HongbaoOpenResultService _hongbaoModel;
        private readonly CityHongbaoOpenResultService _cityHongbaoModel;

        public HongbaoRuntimeBehaviorService(
            InventoryService inventory,
            HongbaoService hongbaoData,
            CityHongbaoService cityHongbaoData,
            IHongbaoRuntimeHost host = null)
        {
            _inventory = inventory;
            _host = host ?? new CapturingHongbaoRuntimeHost();
            if (hongbaoData != null)
                _hongbaoModel = new HongbaoOpenResultService(hongbaoData.GetAllHongbaos());

            if (cityHongbaoData != null)
                _cityHongbaoModel = new CityHongbaoOpenResultService(cityHongbaoData.All);
        }

        public List<HongbaoRuntimeOperation> CapturedOperations => _host.CapturedOperations;

        public HongbaoOpenResult OpenHongbao(int roll, string playerName = null)
            => OpenHongbao(roll, playerName, null);

        public HongbaoOpenResult OpenHongbao(int roll, string playerName, HongbaoOpenedItemRef openedItem)
        {
            CapturedOperations.Clear();
            if (_hongbaoModel == null || _inventory == null) return null;

            int freeSpace = _inventory.GetFreeSpace();
            var result = _hongbaoModel.TryOpen(freeSpace, roll, playerName);

            if (result.Status == HongbaoOpenStatus.InsufficientInventorySpace)
            {
                _host.Talk(result.FailureMessageVi);
                _host.Msg2Player(result.FailureMessageVi);
                return result;
            }

            if (result.Status == HongbaoOpenStatus.RewardSelected)
            {
                ConsumeOpenedItem(openedItem);
                ApplyReward(result.RewardCommand);
                CaptureBonusEvents(result.SelectedEntry?.nameRaw, result.ResolvedMessage, result.ShouldEmitGlobalNews, result.ShouldWriteLog);
            }

            return result;
        }

        public CityHongbaoOpenResult OpenCityHongbao(int roll, string playerName = null)
            => OpenCityHongbao(roll, playerName, null);

        public CityHongbaoOpenResult OpenCityHongbao(int roll, string playerName, HongbaoOpenedItemRef openedItem)
        {
            CapturedOperations.Clear();
            if (_cityHongbaoModel == null || _inventory == null) return null;

            int freeSpace = _inventory.GetFreeSpace();
            var result = _cityHongbaoModel.TryOpen(freeSpace, roll, playerName);

            if (result.Status == CityHongbaoOpenStatus.InsufficientInventorySpace)
            {
                _host.Talk(result.FailureMessageVi);
                _host.Msg2Player(result.FailureMessageVi);
                return result;
            }

            if (result.Status == CityHongbaoOpenStatus.RewardSelected)
            {
                ConsumeOpenedItem(openedItem);
                ApplyCityReward(result.RewardCommand);
                CaptureBonusEvents(result.SelectedEntry?.Name, result.ResolvedMessage, result.ShouldEmitGlobalNews, result.ShouldWriteLog);
            }

            return result;
        }

        private void ConsumeOpenedItem(HongbaoOpenedItemRef openedItem)
        {
            if (openedItem == null) return;
            if (_inventory.RemovePcItem(openedItem.Genre, openedItem.Detail, openedItem.Particular, openedItem.Count))
                _host.ConsumeOpenedItem(openedItem.Genre, openedItem.Detail, openedItem.Particular, openedItem.Count);
        }

        private void ApplyReward(HongbaoRewardCommand cmd)
        {
            if (cmd == null) return;
            if (cmd.CommandType == HongbaoRewardCommandType.AddItem)
            {
                _host.AddItem(cmd.Genre, cmd.Detail, cmd.Particular, cmd.Level, cmd.Serise, cmd.Luck, cmd.Params);
                _inventory.AddPcItem(cmd.Genre, cmd.Detail, cmd.Particular, 1);
            }
            else if (cmd.CommandType == HongbaoRewardCommandType.AddGoldItem)
            {
                _host.AddGoldItem(cmd.AddGoldItemFirstArg, cmd.Genre);
            }
        }

        private void ApplyCityReward(CityHongbaoRewardCommand cmd)
        {
            if (cmd == null) return;
            if (cmd.CommandType == CityHongbaoRewardCommandType.AddItem)
            {
                _host.AddItem(cmd.Genre, cmd.Detail, cmd.Particular, cmd.Level, cmd.Serise, cmd.Luck, cmd.Params);
                _inventory.AddPcItem(cmd.Genre, cmd.Detail, cmd.Particular, 1);
            }
            else if (cmd.CommandType == CityHongbaoRewardCommandType.AddGoldItem)
            {
                _host.AddGoldItem(cmd.AddGoldItemFirstArg, cmd.Genre);
            }
        }

        private void CaptureBonusEvents(string rewardName, string message, bool globalNews, bool writeLog)
        {
            if (!string.IsNullOrEmpty(rewardName))
                _host.Msg2Player("Bạn nhận được " + rewardName + " thưởng!");
            if (globalNews)
                _host.AddGlobalNews(message);
            if (writeLog)
                _host.WriteLog(message);
        }
    }
}
