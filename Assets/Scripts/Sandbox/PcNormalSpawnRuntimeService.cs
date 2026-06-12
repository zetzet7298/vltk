// -----------------------------------------------------------------------------
// VLTK Mobile — PC normal spawn runtime lookup service.
// Source: Assets/StreamingAssets/Reference/PcNormalSpawn/normal.json (5,384 rows)
// Purpose: Load the full PC normal.json catalog at runtime and expose
//          by-template-ID and aggregate lookups via PcNormalSpawnRegistry.
// Pattern: follows PcMapTravelRuntimeService / HongbaoRuntimeBehaviorService wiring.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime service that loads the PC normal.json (5,384 item/spawn rows)
    /// from StreamingAssets and exposes lookup by template ID.
    /// The underlying JSON is a 2-D array (header row + 5,384 data rows, 78 cols each).
    /// Col layout matches PcNormalSpawnParser column constants:
    ///   [0] name, [1] quality, [2] templateId, [7] level, [76-77] price.
    /// </summary>
    public sealed class PcNormalSpawnRuntimeService
    {
        private readonly PcNormalSpawnRegistry _registry;

        /// <summary>Total data rows loaded (should be exactly 5,384).</summary>
        public int Count => _registry.Count;

        /// <summary>Number of unique positive template IDs in the catalog.</summary>
        public int UniquePositiveTemplateCount { get; }

        /// <summary>Number of columns per row in the source JSON (78).</summary>
        public int SourceColumnCount { get; }

        /// <summary>Name of the first data row (col[0]) — sanity anchor.</summary>
        public string FirstNameRaw { get; }

        /// <summary>Template ID of the first data row (col[2]) — sanity anchor.</summary>
        public int FirstTemplateId { get; }

        /// <summary>Level of the first data row (col[7]) — sanity anchor.</summary>
        public int FirstLevel { get; }

        public PcNormalSpawnRuntimeService() : this(null) { }

        public PcNormalSpawnRuntimeService(PcNormalSpawnRegistry registry)
        {
            _registry = registry ?? new PcNormalSpawnRegistry();

            // Compute aggregate facts from loaded registry
            var seen = new HashSet<int>();
            foreach (var sp in _registry.All)
            {
                if (sp.npcTemplateId > 0)
                    seen.Add(sp.npcTemplateId);
            }
            UniquePositiveTemplateCount = seen.Count;

            // Anchor fields from first row (if any)
            var first = _registry.Get(1); // template ID 1
            FirstNameRaw = first?.nameRaw ?? "";
            FirstTemplateId = first?.npcTemplateId ?? 0;
            FirstLevel = first?.level ?? 0;
            SourceColumnCount = 78;
        }

        // ---- Static factory methods ----

        private const string DefaultRelativeDir = "Reference/PcNormalSpawn";
        private const string DefaultFileName = "normal.json";

        /// <summary>
        /// Load from StreamingAssets default path.
        /// </summary>
        public static PcNormalSpawnRuntimeService LoadFromStreamingAssets(
            string relativeDir = DefaultRelativeDir)
        {
            return LoadFromDirectory(
                Path.Combine(Application.streamingAssetsPath, relativeDir));
        }

        /// <summary>
        /// Load from a given directory containing normal.json.
        /// </summary>
        public static PcNormalSpawnRuntimeService LoadFromDirectory(string dir)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                return Empty();

            string jsonPath = Path.Combine(dir, DefaultFileName);
            if (!File.Exists(jsonPath))
                return Empty();

            string json = File.ReadAllText(jsonPath);
            return ParseJson(json);
        }

        /// <summary>
        /// Parse the JSON string (2-D array) and build the registry.
        /// </summary>
        public static PcNormalSpawnRuntimeService ParseJson(string json)
        {
            var registry = new PcNormalSpawnRegistry();

            if (string.IsNullOrEmpty(json))
                return new PcNormalSpawnRuntimeService(registry);

            // Parse the outer JSON array: [[header], [row1], [row2], ...]
            var outer = MiniJsonDeserialize(json);
            if (outer == null || outer.Count == 0)
                return new PcNormalSpawnRuntimeService(registry);

            // First row is header — skip (index 0)
            for (int i = 1; i < outer.Count; i++)
            {
                var row = outer[i];
                if (row == null || row.Count < 8)
                    continue;

                var point = new SpawnPoint
                {
                    nameRaw = StrCol(row, 0),
                    npcTemplateId = IntCol(row, 2),
                    level = IntCol(row, 7),
                    mapId = 0,
                    x = 0,
                    y = 0,
                    direction = 0,
                    count = 0,
                    respawnSec = 0,
                    aiMode = 0,
                    groupId = 0,
                    sourceFile = "normal.json",
                    rowIndex = registry.Count,
                };

                registry.Register(point);
            }

            return new PcNormalSpawnRuntimeService(registry);
        }

        public static PcNormalSpawnRuntimeService Empty()
        {
            return new PcNormalSpawnRuntimeService(new PcNormalSpawnRegistry());
        }

        // ---- Lookup methods ----

        /// <summary>Get first SpawnPoint matching template ID, or null.</summary>
        public SpawnPoint GetByTemplateId(int templateId)
            => _registry.Get(templateId);

        /// <summary>Get all SpawnPoints for a given map ID.</summary>
        public List<SpawnPoint> GetByMapId(int mapId)
            => _registry.GetByMap(mapId);

        /// <summary>Enumerate all loaded SpawnPoints.</summary>
        public IEnumerable<SpawnPoint> All => _registry.All;

        // ---- JSON parsing helpers ----

        private static string StrCol(List<string> row, int col)
        {
            return col < row.Count ? (row[col] ?? "") : "";
        }

        private static int IntCol(List<string> row, int col)
        {
            if (col >= row.Count) return 0;
            int v;
            int.TryParse(row[col], out v);
            return v;
        }

        /// <summary>
        /// Minimal JSON deserializer for a 2-D string array: [[...], [...], ...].
        /// Returns outer list of inner string lists.
        /// </summary>
        private static List<List<string>> MiniJsonDeserialize(string json)
        {
            // Unity's JsonUtility does not handle 2-D arrays.
            // Use manual parsing for this simple structure.
            var result = new List<List<string>>();
            int i = 0;

            if (!SkipWhitespace(json, ref i) || json[i] != '[')
                return result;
            i++; // skip outer [

            while (SkipWhitespace(json, ref i))
            {
                if (json[i] == ']')
                    break;
                if (json[i] == ',')
                { i++; continue; }

                var inner = ParseInnerArray(json, ref i);
                result.Add(inner);
            }

            return result;
        }

        private static List<string> ParseInnerArray(string json, ref int i)
        {
            var items = new List<string>();
            if (json[i] != '[') return items;
            i++; // skip [

            while (SkipWhitespace(json, ref i))
            {
                if (json[i] == ']')
                { i++; break; }
                if (json[i] == ',')
                { i++; continue; }

                string val = ParseStringOrNull(json, ref i);
                items.Add(val);
            }

            return items;
        }

        private static string ParseStringOrNull(string json, ref int i)
        {
            SkipWhitespace(json, ref i);
            if (i >= json.Length) return "";

            // Null literal
            if (json[i] == 'n')
            {
                i += 4; // skip "null"
                return "";
            }

            // String literal
            if (json[i] == '"')
            {
                i++; // skip opening "
                int start = i;
                var sb = new System.Text.StringBuilder();
                while (i < json.Length && json[i] != '"')
                {
                    if (json[i] == '\\' && i + 1 < json.Length)
                    {
                        char next = json[i + 1];
                        if (next == '"' || next == '\\' || next == '/')
                        { sb.Append(next); i += 2; }
                        else if (next == 'n') { sb.Append('\n'); i += 2; }
                        else if (next == 't') { sb.Append('\t'); i += 2; }
                        else if (next == 'u' && i + 5 < json.Length)
                        {
                            string hex = json.Substring(i + 2, 4);
                            int codePoint;
                            if (int.TryParse(hex,
                                System.Globalization.NumberStyles.HexNumber,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out codePoint))
                            {
                                sb.Append((char)codePoint);
                            }
                            i += 6;
                        }
                        else { sb.Append(json[i]); i++; }
                    }
                    else
                    {
                        sb.Append(json[i]);
                        i++;
                    }
                }
                if (i < json.Length && json[i] == '"') i++; // skip closing "
                return sb.ToString();
            }

            // Number or other bare token
            {
                int start = i;
                while (i < json.Length && json[i] != ',' && json[i] != ']' && json[i] != '}')
                    i++;
                return json.Substring(start, i - start).Trim();
            }
        }

        private static bool SkipWhitespace(string json, ref int i)
        {
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            return i < json.Length;
        }
    }
}
