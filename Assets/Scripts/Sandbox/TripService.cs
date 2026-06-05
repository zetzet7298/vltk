// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.16 Trip/Travel Service
// Hành trình du lịch runtime: dịch chuyển theo thời gian + thưởng.
// PC source: settings/trip_config.ini + trip.txt
// Vietnamese: "Hành Trình", "Du Lịch", "Dịch Chuyển", "Phong Lăng Độ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TripService
    {
        public const string LogTag = "Trip";
        public const string DefaultStreamingDir = "Reference/PcTrip";

        private readonly PcTripRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public TripService(PcTripRegistry registry)
        {
            _registry = registry ?? new PcTripRegistry();
        }

        public static TripService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcTripParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} hành trình du lịch từ {dir}");
            return new TripService(reg);
        }

        public PcTripEntry GetTrip(int tripId)
            => _registry.Get(tripId);

        public IReadOnlyList<PcTripEntry> GetTripsFromMap(int startMapId)
            => _registry.GetFromMap(startMapId);

        public IEnumerable<PcTripEntry> GetAll() => _registry.All;
    }
}
