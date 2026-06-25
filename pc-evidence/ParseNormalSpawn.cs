using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace PcEvidence
{
    public class ParseNormalSpawn
    {
        public static void Main()
        {
            string inputPath = "/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/settings/normal.txt";
            // We use relative path assuming this is run from the Unity project root or similar.
            string outputDir = "Assets/StreamingAssets/Reference/PcNormalSpawn";
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, "normal.json");

            // Using ISO-8859-1 to preserve exact byte values since the file may contain special encodings (GBK)
            var lines = File.ReadAllLines(inputPath, Encoding.GetEncoding("ISO-8859-1"));
            var result = new List<string[]>();
            foreach (var line in lines)
            {
                result.Add(line.Split('\t'));
            }
            
            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Generated JSON with {result.Count} rows at {outputPath}");
        }
    }
}
