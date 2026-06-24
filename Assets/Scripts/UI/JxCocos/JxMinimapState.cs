// -----------------------------------------------------------------------------
// VLTK Mobile — JX Minimap state (port of KuiMinMapVN.cpp)
//
// Nguồn: client/Classes/vn/gameui/KuiMinMapVN.cpp (1422 L).
//  - Viewport 128x128 (bgSize = nMapSize = 128x128), góc trên-phải.
//  - Player luôn ở TRUNG TÂM; map nền scroll theo (update(): content_Map placed so
//    roleX/16, roleY/32 lands at clipper center).
//  - World→map pixel offset (NPC points, draw_): nNpcOffsetX = (nMpsX-originX)/16,
//    nNpcOffsetY = (nMpsY-originY)/32.  origin = maxMapRc.left*512 / top*1024.
//  - Coord label (setMpsPos): "X,Y" với X,Y = GetMpsByLocalPosition (int PC mps).
//  - Map name label (setMapNameAndPoint): tên map dịch.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Logic tọa độ là phần verify
// được; texture/render (SPR map bg, DrawNode NPC dots) là asset/visual layer riêng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>Loại điểm (POI) trên minimap. Màu theo loại (draw_ + g_MapTraffic).</summary>
    public enum JxMinimapPoiKind
    {
        None = 0,
        Player = 1,   // tự player (trung tâm, luôn vẽ)
        Npc = 2,      // NPC / tên (Label TTF)
        Door = 3,     // lối chuyển map
        Item = 4,     // vật phẩm rơi
        Teammate = 5, // đồng đội (party)
    }

    /// <summary>Màu RGBA cố định theo loại POI (URP default palette, dùng cho USS).html
    /// Tên class USS do adapter sinh: jx-mini-poi-player/npc/door/item/teammate.</summary>
    public static class JxMinimapPoiStyles
    {
        public static readonly Dictionary<JxMinimapPoiKind, string> UssClass =
            new()
            {
                { JxMinimapPoiKind.Player, "jx-mini-poi-player" },
                { JxMinimapPoiKind.Npc, "jx-mini-poi-npc" },
                { JxMinimapPoiKind.Door, "jx-mini-poi-door" },
                { JxMinimapPoiKind.Item, "jx-mini-poi-item" },
                { JxMinimapPoiKind.Teammate, "jx-mini-poi-teammate" },
            };

        public static string ClassFor(JxMinimapPoiKind k) =>
            UssClass.TryGetValue(k, out var c) ? c : UssClass[JxMinimapPoiKind.Npc];
    }

    /// <summary>State thuần cho JX minimap. Verify được trong EditMode.</summary>
    public sealed class JxMinimapState
    {
        /// <summary>Kích thước viewport minimap (clipper.contentSize). Nguồn: 128x128.</summary>
        public const int ViewportSize = 128;

        /// <summary>Tỉ lệ world→map pixel: X chia 16, Y chia 32 (nguồn draw_/update).</summary>
        public static readonly Vector2 WorldToTexel = new(16f, 32f);

        private Vector2 _mapOriginWorld = Vector2.zero;
        private Vector2 _playerWorld = Vector2.zero;
        private string _mapName = string.Empty;
        private bool _isOpen;

        private readonly List<JxMinimapPoi> _pois = new();

        /// <summary>Map đang mở/hiển thị không (isOpen trong source).</summary>
        public bool IsOpen
        {
            get => _isOpen;
            set => _isOpen = value;
        }

        /// <summary>Tên map dịch (setMapNameAndPoint → pMapNameLabel).</summary>
        public string MapName
        {
            get => _mapName;
            set => _mapName = value ?? string.Empty;
        }

        /// <summary>Gốc tọa độ world của map (maxMapRc.left*512, top*1024).</summary>
        public Vector2 MapOriginWorld
        {
            get => _mapOriginWorld;
            set => _mapOriginWorld = value;
        }

        /// <summary>Vị trí world (PC mps) của player (nRoleMpsX/nRoleMpsY).</summary>
        public Vector2 PlayerWorld
        {
            get => _playerWorld;
            set => _playerWorld = value;
        }

        /// <summary>Danh sách POI chỉ-đọc.</summary>
        public IReadOnlyList<JxMinimapPoi> Pois => _pois;

        // ---- API port ----

        /// <summary>setMapNameAndPoint(name, point): đặt tên map + gốc (point).</summary>
        public void SetMap(string name, Vector2 originWorld)
        {
            _mapName = name ?? string.Empty;
            _mapOriginWorld = originWorld;
        }

        /// <summary>setMpsPos(x, y): đặt vị trí world player + label coord "X,Y".</summary>
        public void SetPlayerPos(float worldX, float worldY) =>
            _playerWorld = new Vector2(worldX, worldY);

        /// <summary>upDataMap nạp POI mới (xóa cũ). Mỗi entry = {world, kind, name}.</summary>
        public void SetPois(IEnumerable<JxMinimapPoi> pois)
        {
            _pois.Clear();
            if (pois != null) _pois.AddRange(pois);
        }

        public void AddPoi(JxMinimapPoi poi)
        {
            if (poi != null) _pois.Add(poi);
        }

        public void ClearPois() => _pois.Clear();

        // ---- Tọa độ (verify được) ----

        /// <summary>
        /// Label tọa độ PC (setMpsPos → ptestLabel "X,Y"). Làm tròn int như source
        /// (GetMpsByLocalPosition trả int).
        /// </summary>
        public string CoordText =>
            Mathf.RoundToInt(_playerWorld.x) + "," + Mathf.RoundToInt(_playerWorld.y);

        /// <summary>
        /// Pixel offset của 1 điểm world TÍNH TỪ GỐC MAP (draw_ nNpcOffset).
        /// Nguồn: x = (worldX - originX)/16, y = (worldY - originY)/32.
        /// </summary>
        public Vector2 WorldToMapOffset(Vector2 worldPos)
        {
            return new Vector2(
                (worldPos.x - _mapOriginWorld.x) / WorldToTexel.x,
                (worldPos.y - _mapOriginWorld.y) / WorldToTexel.y);
        }

        /// <summary>
        /// Pixel offset của 1 điểm world TÍNH TỪ TRUNG TÂM VIEWPORT (= player),
        /// theo quy ước UI Toolkit (x tăng phải, y tăng LÊN). Adapter Render dùng
        /// top = center - offset.y nên Y đã lật đúng 1 lần tại đó; ở đây trả Y thẳng.
        /// dx = (worldX - playerX)/16, dy = (worldY - playerY)/32.
        /// NPC phía bắc (worldY+) → offset.y+ → Render vẽ lên trên player. Đúng nguồn
        /// cocos Y-up (draw_: nNpcPosY = base + texH - nNpcOffsetY).
        /// </summary>
        public Vector2 RelativeCenterOffset(Vector2 worldPos)
        {
            return new Vector2(
                (worldPos.x - _playerWorld.x) / WorldToTexel.x,
                (worldPos.y - _playerWorld.y) / WorldToTexel.y);
        }

        /// <summary>
        /// POI có nằm trong viewport (clipper rect 128x128) không, tính từ offset
        /// tương đối trung tâm. Source: m_bScrolling = rect.containsPoint(point).
        /// </summary>
        public bool IsInViewport(Vector2 relativeOffset)
        {
            float half = ViewportSize * 0.5f;
            return Mathf.Abs(relativeOffset.x) <= half && Mathf.Abs(relativeOffset.y) <= half;
        }
    }

    /// <summary>Một điểm POI trên minimap (NPC/door/item/teammate).</summary>
    public sealed class JxMinimapPoi
    {
        public Vector2 WorldPos;
        public JxMinimapPoiKind Kind = JxMinimapPoiKind.Npc;
        public string Name = string.Empty;
    }
}
