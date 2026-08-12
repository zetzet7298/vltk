// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime service cho bảng cường hóa thuộc tính hiếm (PC rare.txt)
//
// Wraps PcRareEnchantTable. rare.txt là BẢNG CƯỜNG HÓA THUỘC TÍNH HIẾM
// (weapon-enchant / magic-attribute roll table) của hệ thống cường hóa / đổi
// vật phẩm — KHÔNG phải bảng spawn quái. Service tra cứu theo MAGIC_ID.
//
// PC source: settings/rare.txt = itemexchange_setting/rare.txt (byte-identical).
// Việt hoá: tên thuộc tính (nameRaw, Trung văn) cần map sang tiếng Việt ở tầng UI.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn bảng cường hóa thuộc tính hiếm.
    /// PC source: settings/rare.txt (29 cột: NAME, MAGIC_ID, MAG_P1_MIN/MAX,
    /// trọng số roll theo vũ khí / vị trí trang bị / hệ ngũ hành).
    /// </summary>
    public class RareEnchantService
    {
        public const string LogTag = "RareEnchant";

        /// <summary>
        /// Thư mục StreamingAssets chứa rare.txt. rare.txt hiện được commit chung
        /// thư mục Reference/PcNpc với các bảng tham chiếu khác.
        /// </summary>
        public const string DefaultStreamingDir = "Reference/PcNpc";

        private PcRareEnchantTable _table = new();

        public event Action OnLoaded;
        public int Count => _table?.Count ?? 0;
        public int MagicIdCount => _table?.MagicIdCount ?? 0;

        public void AttachTable(PcRareEnchantTable table)
        {
            _table = table ?? new PcRareEnchantTable();
            OnLoaded?.Invoke();
        }

        public static RareEnchantService LoadFromStreamingAssets(string relativeDir = DefaultStreamingDir)
        {
            var svc = new RareEnchantService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var table = PcRareEnchantParser.BuildTable(path);
                svc.AttachTable(table);
                Debug.Log($"[{LogTag}] Đã nạp {svc.Count} dòng cường hóa thuộc tính hiếm ({svc.MagicIdCount} MAGIC_ID) từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Nạp thất bại: {ex.Message}");
            }
            return svc;
        }

        /// <summary>Tất cả tier của một MAGIC_ID.</summary>
        public List<PcRareEnchantEntry> GetByMagicId(int magicId) => _table.GetByMagicId(magicId);

        public IReadOnlyList<PcRareEnchantEntry> All => _table.All;
    }
}
