using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class HongbaoRuntimeBehaviorService
    {
        private readonly InventoryService _inventory;
        private readonly HongbaoOpenResultService _hongbaoModel;
        private readonly CityHongbaoOpenResultService _cityHongbaoModel;

        public HongbaoRuntimeBehaviorService(
            InventoryService inventory, 
            HongbaoService hongbaoData,
            CityHongbaoService cityHongbaoData)
        {
            _inventory = inventory;
            if (hongbaoData != null)
                _hongbaoModel = new HongbaoOpenResultService(hongbaoData.GetAllHongbaos());
            
            if (cityHongbaoData != null)
                _cityHongbaoModel = new CityHongbaoOpenResultService(cityHongbaoData.All);
        }

        public HongbaoOpenResult OpenHongbao(int roll, string playerName = null)
        {
            if (_hongbaoModel == null || _inventory == null) return null;

            int freeSpace = _inventory.GetFreeSpace();
            var result = _hongbaoModel.TryOpen(freeSpace, roll, playerName);

            if (result.Status == HongbaoOpenStatus.RewardSelected)
            {
                ApplyReward(result.RewardCommand);
            }

            return result;
        }

        public CityHongbaoOpenResult OpenCityHongbao(int roll, string playerName = null)
        {
            if (_cityHongbaoModel == null || _inventory == null) return null;

            int freeSpace = _inventory.GetFreeSpace();
            var result = _cityHongbaoModel.TryOpen(freeSpace, roll, playerName);

            if (result.Status == CityHongbaoOpenStatus.RewardSelected)
            {
                ApplyCityReward(result.RewardCommand);
            }

            return result;
        }

        private void ApplyReward(HongbaoRewardCommand cmd)
        {
            if (cmd.CommandType == HongbaoRewardCommandType.AddItem)
            {
                _inventory.AddPcItem(cmd.Genre, cmd.Detail, cmd.Particular, 1);
            }
            else if (cmd.CommandType == HongbaoRewardCommandType.AddGoldItem)
            {
                // In Sandbox, map AddGoldItem to AddPcItem for testing. Genre maps to AddGoldItemFirstArg
                _inventory.AddPcItem(cmd.Genre, 0, 0, 1);
            }
        }

        private void ApplyCityReward(CityHongbaoRewardCommand cmd)
        {
            if (cmd.CommandType == CityHongbaoRewardCommandType.AddItem)
            {
                _inventory.AddPcItem(cmd.Genre, cmd.Detail, cmd.Particular, 1);
            }
            else if (cmd.CommandType == CityHongbaoRewardCommandType.AddGoldItem)
            {
                _inventory.AddPcItem(cmd.Genre, 0, 0, 1);
            }
        }
    }
}
