using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace VLTK.Editor.Porting
{
    public class PcDialogSysIndexer
    {
        private const string PcDialogSysPath = "/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/dailogsys";
        private const string OutputPath = "Assets/StreamingAssets/Reference/PcDialogSys/DialogSysIndex.json";

        [MenuItem("Tools/Porting/Index PC DialogSys")]
        public static void IndexDialogSys()
        {
            if (!Directory.Exists(PcDialogSysPath))
            {
                Debug.LogError($"[PcDialogSysIndexer] PC path not found: {PcDialogSysPath}");
                return;
            }

            var files = Directory.GetFiles(PcDialogSysPath, "*.lua", SearchOption.AllDirectories);
            var indexData = files.Select(file =>
            {
                var fileInfo = new FileInfo(file);
                return new DialogSysFileMeta
                {
                    FileName = fileInfo.Name,
                    RelativePath = file.Replace(PcDialogSysPath + "/", ""),
                    SizeBytes = fileInfo.Length,
                    LastWriteTimeUtc = fileInfo.LastWriteTimeUtc.ToString("o")
                };
            }).ToArray();

            var wrapper = new DialogSysIndexWrapper
            {
                SourcePath = PcDialogSysPath,
                IndexedAt = System.DateTime.UtcNow.ToString("o"),
                TotalFiles = files.Length,
                Files = indexData
            };

            var json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
            
            var outDir = Path.GetDirectoryName(OutputPath);
            if (!Directory.Exists(outDir))
            {
                Directory.CreateDirectory(outDir);
            }

            File.WriteAllText(OutputPath, json);
            AssetDatabase.Refresh();

            Debug.Log($"[PcDialogSysIndexer] Indexed {files.Length} files to {OutputPath}");
        }

        [System.Serializable]
        public class DialogSysIndexWrapper
        {
            public string SourcePath;
            public string IndexedAt;
            public int TotalFiles;
            public DialogSysFileMeta[] Files;
        }

        [System.Serializable]
        public class DialogSysFileMeta
        {
            public string FileName;
            public string RelativePath;
            public long SizeBytes;
            public string LastWriteTimeUtc;
        }
    }
}
