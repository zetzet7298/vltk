// -----------------------------------------------------------------------------
// VLTK Mobile — IAttribConstHost: giao diện host cho AttribConstService.
// Cho phép runtime dispatch các side-effect khi load/truy vấn thuộc tính hằng số
// (UI buff/setting, log, SFX, save).
// PC source: settings/attribconstdata.ini + magicdesc.ini + rolevalue.ini + gamesetting.ini.
// PC surfaces: UpdateAttribUI, Msg2Player, PlayAttribSFX, SaveAttribCache.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho AttribConstService. Implement bởi UI/Setting/Save.
    /// </summary>
    public interface IAttribConstHost
    {
        /// <summary>Load attrib registry xong (PC OnAttribLoaded).</summary>
        void OnAttribLoaded(int sectionCount, int totalEntries, long durationMs);

        /// <summary>Attach registry (PC OnAttribRegistryAttached).</summary>
        void OnAttribRegistryAttached(int sectionCount, int totalEntries);

        /// <summary>Truy vấn section thành công (PC OnAttribSectionQueried).</summary>
        void OnAttribSectionQueried(string section, int entryCount);

        /// <summary>Truy vấn section không tìm thấy (PC OnAttribSectionMissing).</summary>
        void OnAttribSectionMissing(string section);

        /// <summary>Truy vấn key thành công (PC OnAttribKeyQueried).</summary>
        void OnAttribKeyQueried(string section, string key, string value);

        /// <summary>Truy vấn key không tìm thấy (PC OnAttribKeyMissing).</summary>
        void OnAttribKeyMissing(string section, string key);

        /// <summary>Resolve magic code thành công (PC OnMagicCodeResolved).</summary>
        void OnMagicCodeResolved(string section, string key, int magicCode);

        /// <summary>Hiển thị UI thuộc tính hằng số (PC ShowAttribUI).</summary>
        void ShowAttribUI(string section, int entryCount);

        /// <summary>Log thông báo thuộc tính hằng số lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogAttribEvent(string section, string key, string message);

        /// <summary>Phát SFX khi load thuộc tính hằng số (PC PlayAttribSFX).</summary>
        void PlayAttribSFX(string action);

        /// <summary>Lưu cache thuộc tính hằng số vào DB (PC SaveAttribCache).</summary>
        void SaveAttribCache(int sectionCount, int totalEntries);
    }
}
