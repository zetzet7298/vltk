// -----------------------------------------------------------------------------
// VLTK Mobile — DialogSys Runtime Service (Hệ Thống Hội Thoại Runtime)
// Kết nối DialogSysIndex.json + DialogSysIndexService vào runtime context.
// PC source: script/dailogsys (5 core Lua scripts: g_dialog, dailog, dailogsay,
//   dialogoption, composeoption).
// Runtime Lua execution / callback dispatch is intentionally out of scope.
// Vietnamese: "Hội Thoại", "Lựa Chọn", "Xác Nhận", "Hủy Bỏ", "Kết Thúc Đối Thoại".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Entry từ DialogSysIndex.json — mô tả một script file trong PC dailogsys.
    /// </summary>
    [Serializable]
    public class DialogSysIndexEntry
    {
        public string FileName;
        public string RelativePath;
        public long SizeBytes;
        public string LastWriteTimeUtc;
    }

    /// <summary>
    /// Context truyền vào khi mở hội thoại NPC.
    /// </summary>
    public class DialogOpenContext
    {
        public int npcTemplateId;
        public string npcName;
        public int playerId;
        public int playerLevel;
        public int playerFaction;
        public int currentMapId;
        public int selectedItemIndex;
    }

    /// <summary>
    /// Kết quả mở hội thoại — chứa thông tin script class và option surfaces.
    /// </summary>
    public class DialogOpenResult
    {
        public bool opened;
        public string npcName;
        public string dialogClass;
        public string titleMsg;
        public List<string> optionSurfaces = new List<string>();
        public List<string> saySurfaces = new List<string>();
    }

    /// <summary>
    /// Runtime service cho hệ thống hội thoại PC dailogsys.
    /// Load index, cung cấp lookup, và mô phỏng PC dialog flow (mock — không Lua VM).
    /// </summary>
    public sealed class DialogSysRuntimeService
    {
        public const string LogTag = "DialogSysRuntime";
        public const string JsonIndexPath = "Reference/PcDialogSys/DialogSysIndex.json";
        public const string SourceIndexRelativeDir = "Reference/PcDialogSys";

        // Tên các script PC core — phải khớp DialogSysIndex.json
        public const string ScriptGDialog = "g_dialog.lua";
        public const string ScriptDailog = "dailog.lua";
        public const string ScriptDailogSay = "dailogsay.lua";
        public const string ScriptDialogOption = "dialogoption.lua";
        public const string ScriptComposeOption = "composeoption.lua";

        // PC dialog class names trích từ source index
        public static readonly string[] PcDialogClasses =
        {
            "G_DIALOG", "DailogClass", "DailogOptionClass", "ComposeOptionClass"
        };

        // PC surface functions trích từ dailogsay.lua
        public static readonly string[] PcSaySurfaces =
        {
            "CreateNewSayEx", "g_DailogBack", "g_AskClientStringEx",
            "g_AskClientStringBackEx", "g_AskClientNumberEx",
            "g_AskClientNumberBackEx", "g_GiveItemUI",
            "g_GiveItemUIBack", "g_GiveItemUICancel"
        };

        // PC surface functions trích từ dialogoption.lua
        public static readonly string[] PcOptionSurfaces =
        {
            "OnSelect", "GetEntry"
        };

        private readonly List<DialogSysIndexEntry> _jsonEntries = new List<DialogSysIndexEntry>();
        private readonly Dictionary<string, DialogSysIndexEntry> _byFileName = new Dictionary<string, DialogSysIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private DialogSysIndexService _sourceIndexService;

        public int JsonEntryCount => _jsonEntries.Count;
        public long TotalJsonSizeBytes { get; private set; }
        public int SourceIndexCount => _sourceIndexService != null ? _sourceIndexService.Count : 0;
        public int SourceLuaFileCount => _sourceIndexService != null ? _sourceIndexService.LuaFileCount : 0;
        public int SourceFunctionCount => _sourceIndexService != null ? _sourceIndexService.TotalFunctionCount : 0;
        public int SourceGlobalSymbolCount => _sourceIndexService != null ? _sourceIndexService.TotalGlobalSymbolCount : 0;
        public int SourceOptionSurfaceCount => _sourceIndexService != null ? _sourceIndexService.TotalOptionSurfaceCount : 0;
        public int SourceSaySurfaceCount => _sourceIndexService != null ? _sourceIndexService.TotalSaySurfaceCount : 0;
        public long SourceTotalSizeBytes => _sourceIndexService != null ? _sourceIndexService.TotalSizeBytes : 0L;
        public IReadOnlyList<DialogSysIndexEntry> JsonEntries => _jsonEntries;
        public IReadOnlyList<PcDialogSysSourceIndexEntry> SourceEntries => _sourceIndexService?.All ?? Array.Empty<PcDialogSysSourceIndexEntry>();

        public DialogSysRuntimeService() { }

        public DialogSysRuntimeService(
            List<DialogSysIndexEntry> jsonEntries,
            DialogSysIndexService sourceIndexService)
        {
            if (jsonEntries != null)
            {
                foreach (var e in jsonEntries) RegisterJsonEntry(e);
            }
            _sourceIndexService = sourceIndexService;
        }

        // ── JSON Index Lookup ────────────────────────────────────────────────

        public DialogSysIndexEntry GetJsonEntryByFileName(string fileName)
            => !string.IsNullOrEmpty(fileName) && _byFileName.TryGetValue(fileName, out var e) ? e : null;

        public bool HasScript(string fileName)
            => _byFileName.ContainsKey(fileName);

        // ── Source Index Lookup (delegated) ───────────────────────────────────

        public PcDialogSysSourceIndexEntry GetSourceByPath(string relativePath)
            => _sourceIndexService?.GetByRelativePath(relativePath);

        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetSourcesByFunction(string functionName)
            => _sourceIndexService?.GetByFunction(functionName) ?? Array.Empty<PcDialogSysSourceIndexEntry>();

        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetSourcesBySurface(string surface)
            => _sourceIndexService?.GetBySurface(surface) ?? Array.Empty<PcDialogSysSourceIndexEntry>();

        // ── Runtime Dialog Flow (mock) ───────────────────────────────────────

        /// <summary>
        /// Mở hội thoại cho NPC. Trả về DialogOpenResult với script metadata.
        /// Không thực thi Lua — chỉ cung cấp thông tin PC dialog class/option/say surface.
        /// </summary>
        public DialogOpenResult OpenDialog(DialogOpenContext ctx)
        {
            if (ctx == null) return new DialogOpenResult { opened = false };

            var result = new DialogOpenResult
            {
                opened = true,
                npcName = ctx.npcName ?? $"NPC_{ctx.npcTemplateId}",
                dialogClass = "DailogClass",
                titleMsg = "Xin chào đại hiệp! Khi hành tẩu giang hồ hãy luôn cẩn trọng!"
            };

            // Trích option surfaces từ dialogoption.lua
            var optEntry = GetSourceByPath("dialogoption.lua");
            if (optEntry != null)
            {
                foreach (var s in optEntry.representativeOptionSurfaces)
                    result.optionSurfaces.Add(s);
            }

            // Trích say surfaces từ dailogsay.lua
            var sayEntry = GetSourceByPath("dailogsay.lua");
            if (sayEntry != null)
            {
                foreach (var s in sayEntry.representativeSaySurfaces)
                    result.saySurfaces.Add(s);
            }

            return result;
        }

        /// <summary>
        /// Chọn option trong hội thoại. Trả về true nếu option surface được tìm thấy.
        /// </summary>
        public bool SelectOption(DialogOpenContext ctx, string optionText)
        {
            if (ctx == null || string.IsNullOrEmpty(optionText)) return false;
            // PC flow: G_DIALOG:OnSelect -> DailogOptionClass:OnSelect -> condition check -> action
            // Mock: chỉ verify option surface tồn tại
            var onSelectSources = GetSourcesBySurface("OnSelect");
            return onSelectSources.Count > 0;
        }

        /// <summary>
        /// Mô phỏng PC CreateNewSayEx flow: hiển thị dialog Say + options cho player.
        /// </summary>
        public DialogOpenResult CreateNewSay(string title, List<string> options)
        {
            var result = new DialogOpenResult
            {
                opened = true,
                dialogClass = "CreateNewSayEx",
                titleMsg = title ?? string.Empty
            };
            if (options != null) result.optionSurfaces.AddRange(options);
            return result;
        }

        // ── JSON Parsing ─────────────────────────────────────────────────────

        private void RegisterJsonEntry(DialogSysIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.FileName)) return;
            if (_byFileName.ContainsKey(e.FileName)) return;
            _jsonEntries.Add(e);
            _byFileName[e.FileName] = e;
            TotalJsonSizeBytes += Math.Max(0L, e.SizeBytes);
        }

        private static List<DialogSysIndexEntry> ParseDialogSysIndexJson(string jsonPath)
        {
            var entries = new List<DialogSysIndexEntry>();
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath)) return entries;

            string json = File.ReadAllText(jsonPath);
            var wrapper = JsonWrapper.FromJson(json);
            if (wrapper?.Files == null) return entries;

            foreach (var f in wrapper.Files)
            {
                if (f == null || string.IsNullOrEmpty(f.FileName)) continue;
                entries.Add(new DialogSysIndexEntry
                {
                    FileName = f.FileName,
                    RelativePath = f.RelativePath,
                    SizeBytes = f.SizeBytes,
                    LastWriteTimeUtc = f.LastWriteTimeUtc
                });
            }
            return entries;
        }

        // ── Static Loaders ───────────────────────────────────────────────────

        public static DialogSysRuntimeService LoadFromStreamingAssets()
        {
            var jsonPath = Path.Combine(Application.streamingAssetsPath, JsonIndexPath);
            var entries = ParseDialogSysIndexJson(jsonPath);

            DialogSysIndexService sourceService = null;
            try
            {
                sourceService = DialogSysIndexService.LoadFromStreamingAssets();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Source index load failed (non-fatal): {ex.Message}");
            }

            var svc = new DialogSysRuntimeService();
            foreach (var e in entries) svc.RegisterJsonEntry(e);
            svc._sourceIndexService = sourceService;
            return svc;
        }

        public static DialogSysRuntimeService LoadFromDirectory(string dir)
        {
            var jsonPath = Path.Combine(dir, "DialogSysIndex.json");
            var entries = ParseDialogSysIndexJson(jsonPath);

            DialogSysIndexService sourceService = null;
            try
            {
                sourceService = DialogSysIndexService.LoadFromDirectory(dir);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Source index load from dir failed (non-fatal): {ex.Message}");
            }

            var svc = new DialogSysRuntimeService();
            foreach (var e in entries) svc.RegisterJsonEntry(e);
            svc._sourceIndexService = sourceService;
            return svc;
        }

        // ── JSON Wrapper (manual, no dependency) ─────────────────────────────

        [Serializable]
        private class JsonWrapper
        {
            public string SourcePath;
            public int TotalFiles;
            public List<JsonFileEntry> Files;

            public static JsonWrapper FromJson(string json)
            {
                try
                {
                    return JsonUtility.FromJson<JsonWrapper>(json);
                }
                catch
                {
                    return null;
                }
            }
        }

        [Serializable]
        private class JsonFileEntry
        {
            public string FileName;
            public string RelativePath;
            public long SizeBytes;
            public string LastWriteTimeUtc;
        }
    }
}
