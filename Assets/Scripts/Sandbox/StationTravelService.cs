// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.4 Station Travel Service (Xa Phu)
// Xa Phu travel system: station positions, fee calculation, map teleport.
// PC source: XaPhu NPC positions, travel fees, map level requirements.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public class TravelStation
    {
        public int stationId;
        public string nameVi;           // Tên hiển thị tiếng Việt
        public int mapId;               // Map đích
        public float spawnX;            // Tọa độ spawn X (MPS)
        public float spawnY;            // Tọa độ spawn Y (MPS)
        public int requiredLevel;       // Cấp độ yêu cầu tối thiểu
        public int silverCost;          // Phí Bạc
        public string categoryVi;       // Phân loại: "Tân Thủ Thôn", "Đại Đô Thị", "Bản Đồ Luyện Công"
    }

    /// <summary>
    /// Service quản lý hệ thống di chuyển Xa Phu.
    /// PC source: XaPhu NPC (templateId 501), travel menu, silver cost.
    /// </summary>
    public class StationTravelService
    {
        public const int XaPhuTemplateId = 501;
        public const int BaLangMapId = 79;

        private readonly List<TravelStation> _stations = new();
        private readonly PlayerLevelService _levelService;

        public IReadOnlyList<TravelStation> Stations => _stations;

        public event Action<TravelStation> OnTravelCompleted;

        public StationTravelService(PlayerLevelService levelService = null)
        {
            _levelService = levelService;
            InitializeDefaultStations();
        }

        /// <summary>Lấy danh sách trạm có thể đi (đủ level).</summary>
        public List<TravelStation> GetAvailableStations(int playerLevel, int silver)
        {
            var available = new List<TravelStation>();
            foreach (var station in _stations)
            {
                if (playerLevel >= station.requiredLevel && silver >= station.silverCost)
                    available.Add(station);
            }
            return available;
        }

        /// <summary>Thực hiện di chuyển đến trạm.</summary>
        public bool Travel(int stationId, ref int playerSilver, ref Vector2 playerPosition, ref int currentMapId)
        {
            var station = _stations.Find(s => s.stationId == stationId);
            if (station == null)
            {
                SubsystemLog.Warn("StationTravel", $"Station {stationId} not found.");
                return false;
            }

            int playerLevel = _levelService?.Level ?? 1;

            // Kiểm tra cấp độ
            if (playerLevel < station.requiredLevel)
            {
                SubsystemLog.Warn("StationTravel", $"Cần cấp độ {station.requiredLevel} để đi {station.nameVi}.");
                return false;
            }

            // Kiểm tra Bạc
            if (playerSilver < station.silverCost)
            {
                SubsystemLog.Warn("StationTravel", $"Không đủ Bạc ({station.silverCost} cần).");
                return false;
            }

            // Trừ tiền, dịch chuyển
            playerSilver -= station.silverCost;
            playerPosition = new Vector2(station.spawnX, station.spawnY);
            currentMapId = station.mapId;

            OnTravelCompleted?.Invoke(station);
            SubsystemLog.Info("StationTravel", $"Đã di chuyển đến {station.nameVi} (Map {station.mapId}), phí {station.silverCost} Bạc.");
            return true;
        }

        /// <summary>Tìm trạm gần nhất với vị trí hiện tại.</summary>
        public TravelStation FindNearestStation(Vector2 playerPos)
        {
            TravelStation nearest = null;
            float minDist = float.MaxValue;

            foreach (var station in _stations)
            {
                float dist = Vector2.Distance(playerPos, new Vector2(station.spawnX, station.spawnY));
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = station;
                }
            }
            return nearest;
        }

        /// <summary>Thêm trạm tùy chỉnh.</summary>
        public void AddStation(TravelStation station)
        {
            if (station == null) return;
            _stations.Add(station);
        }

        // ── Default Station Data ───────────────────────────────────────────

        private void InitializeDefaultStations()
        {
            // === Tân Thủ Thôn (Miễn phí hoặc phí rất thấp) ===
            _stations.Add(new TravelStation
            {
                stationId = 1,
                nameVi = "Ba Lăng Huyện",
                mapId = 79,
                spawnX = 1600, spawnY = 3200,
                requiredLevel = 1,
                silverCost = 0,
                categoryVi = "Tân Thủ Thôn"
            });
            _stations.Add(new TravelStation
            {
                stationId = 2,
                nameVi = "Giang Tân Thôn",
                mapId = 80,
                spawnX = 1400, spawnY = 2800,
                requiredLevel = 1,
                silverCost = 0,
                categoryVi = "Tân Thủ Thôn"
            });
            _stations.Add(new TravelStation
            {
                stationId = 3,
                nameVi = "Long Môn Trấn",
                mapId = 81,
                spawnX = 1200, spawnY = 2400,
                requiredLevel = 1,
                silverCost = 10,
                categoryVi = "Tân Thủ Thôn"
            });

            // === Đại Đô Thị (Tốn Bạc) ===
            _stations.Add(new TravelStation
            {
                stationId = 10,
                nameVi = "Phượng Tường Phủ",
                mapId = 100,
                spawnX = 2000, spawnY = 4000,
                requiredLevel = 10,
                silverCost = 50,
                categoryVi = "Đại Đô Thị"
            });
            _stations.Add(new TravelStation
            {
                stationId = 11,
                nameVi = "Biện Kinh Phủ",
                mapId = 101,
                spawnX = 2200, spawnY = 4200,
                requiredLevel = 10,
                silverCost = 50,
                categoryVi = "Đại Đô Thị"
            });
            _stations.Add(new TravelStation
            {
                stationId = 12,
                nameVi = "Lâm An Phủ",
                mapId = 102,
                spawnX = 1800, spawnY = 3800,
                requiredLevel = 10,
                silverCost = 50,
                categoryVi = "Đại Đô Thị"
            });
            _stations.Add(new TravelStation
            {
                stationId = 13,
                nameVi = "Thành Đô Phủ",
                mapId = 103,
                spawnX = 1600, spawnY = 3600,
                requiredLevel = 10,
                silverCost = 50,
                categoryVi = "Đại Đô Thị"
            });

            // === Bản Đồ Luyện Công ===
            _stations.Add(new TravelStation
            {
                stationId = 20,
                nameVi = "Đào Hoa Nguyên",
                mapId = 200,
                spawnX = 1000, spawnY = 2000,
                requiredLevel = 5,
                silverCost = 30,
                categoryVi = "Bản Đồ Luyện Công"
            });
            _stations.Add(new TravelStation
            {
                stationId = 21,
                nameVi = "Thục Cương Sơn",
                mapId = 201,
                spawnX = 800, spawnY = 1600,
                requiredLevel = 15,
                silverCost = 60,
                categoryVi = "Bản Đồ Luyện Công"
            });
            _stations.Add(new TravelStation
            {
                stationId = 22,
                nameVi = "Thần Nông Giá",
                mapId = 202,
                spawnX = 900, spawnY = 1800,
                requiredLevel = 25,
                silverCost = 100,
                categoryVi = "Bản Đồ Luyện Công"
            });
            _stations.Add(new TravelStation
            {
                stationId = 23,
                nameVi = "Tần Lăng",
                mapId = 203,
                spawnX = 700, spawnY = 1400,
                requiredLevel = 40,
                silverCost = 200,
                categoryVi = "Bản Đồ Luyện Công"
            });
            _stations.Add(new TravelStation
            {
                stationId = 24,
                nameVi = "Thái Hồ",
                mapId = 204,
                spawnX = 1100, spawnY = 2200,
                requiredLevel = 30,
                silverCost = 150,
                categoryVi = "Bản Đồ Luyện Công"
            });
        }
    }
}
