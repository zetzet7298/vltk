// -----------------------------------------------------------------------------
// VLTK Mobile — PC missles1.txt full visual parser (57 columns)
// Source: Assets/StreamingAssets/Reference/PcAttrib/missles1.txt
// Purpose: Parse ALL visual data from PC missile definitions including:
//   AnimFile1-4 (flight SPR paths), AnimFileB1-4 (explosion SPR paths),
//   AnimFileInfo1-4 (frames,directions,interval), light color, speed, lifetime.
// This replaces the hardcoded per-skill visual switch-cases with data-driven lookups.
// PC source: KMissle struct in KSkill.h / KSkill.cpp
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Full visual data for a single PC missile definition.
    /// Maps directly to PC missles1.txt 57-column format.
    /// </summary>
    [Serializable]
    public class PcMissileFullVisual
    {
        public int missileId;
        public string nameRaw;

        // Movement
        public int moveKind;         // 0=stationary, 1=linear, 2=parabolic, 3=homing
        public int followKind;       // 0=none, 1=follow target
        public int missleHeight;     // Z offset
        public int speed;            // pixels per tick
        public int lifetime;         // ticks
        public int zspeed;           // Z velocity
        public int zacc;             // Z acceleration

        // Collision
        public int collidRange;
        public int isRangeDmg;       // 1=AOE
        public int dmgRange;         // AOE radius
        public int dmgInterval;

        // Loop / sub-animation
        public int loopPlay;
        public int subLoop;
        public int subStart;
        public int subStop;

        // Response skill
        public int responseSkill;
        public int canDestroy;
        public int colVanish;        // disappear on collision
        public int canSlow;
        public int canColFriend;
        public int autoExplode;

        // Visual: Flight animation SPR files + info
        public MissileAnimSlot[] flightAnims = new MissileAnimSlot[4];  // AnimFile1-4
        public MissileAnimSlot[] explodeAnims = new MissileAnimSlot[4]; // AnimFileB1-4

        // Light
        public int redLum;
        public int greenLum;
        public int blueLum;
        public int lightRadius;

        /// <summary>Get primary flight SPR path (first non-empty AnimFile).</summary>
        public string PrimaryFlightSpr => FirstNonEmpty(flightAnims);

        /// <summary>Get primary explosion SPR path (first non-empty AnimFileB).</summary>
        public string PrimaryExplodeSpr => FirstNonEmpty(explodeAnims);

        /// <summary>Get primary flight anim info.</summary>
        public MissileAnimSlot PrimaryFlight => FirstSlot(flightAnims);

        /// <summary>Get primary explosion anim info.</summary>
        public MissileAnimSlot PrimaryExplode => FirstSlot(explodeAnims);

        /// <summary>Is this a stationary/area effect (MoveKind=0)?</summary>
        public bool IsStationary => moveKind == 0;

        /// <summary>Light color as Unity Color (0-1 range).</summary>
        public Color LightColor => new Color(
            Mathf.Clamp01(redLum / 255f),
            Mathf.Clamp01(greenLum / 255f),
            Mathf.Clamp01(blueLum / 255f));

        private static string FirstNonEmpty(MissileAnimSlot[] slots)
        {
            if (slots == null) return null;
            foreach (var s in slots)
                if (s != null && !string.IsNullOrEmpty(s.sprPath)) return s.sprPath;
            return null;
        }

        private static MissileAnimSlot FirstSlot(MissileAnimSlot[] slots)
        {
            if (slots == null) return null;
            foreach (var s in slots)
                if (s != null && !string.IsNullOrEmpty(s.sprPath)) return s;
            return null;
        }
    }

    /// <summary>
    /// Single animation slot from missles1.txt (AnimFile + AnimFileInfo + SndFile).
    /// AnimFileInfo format: "frames,directions,interval" (e.g., "64,16,1")
    /// </summary>
    [Serializable]
    public class MissileAnimSlot
    {
        public string sprPath;       // Raw PC SPR path (e.g., \spr\skill\xxx\yyy.spr)
        public int totalFrames;      // From AnimFileInfo col 1
        public int directions;       // From AnimFileInfo col 2
        public int intervalTicks;    // From AnimFileInfo col 3
        public string soundPath;     // Sound file path

        /// <summary>Duration in seconds at 18 ticks/sec.</summary>
        public float DurationSeconds => totalFrames > 0 && intervalTicks > 0
            ? (totalFrames * intervalTicks) / 18f : 0.5f;

        /// <summary>Has valid SPR data?</summary>
        public bool HasSpr => !string.IsNullOrEmpty(sprPath);
    }

    /// <summary>
    /// Registry of full visual data for all PC missiles.
    /// Parses missles1.txt once and provides lookup by missileId.
    /// </summary>
    public sealed class PcMissileFullVisualRegistry
    {
        private readonly Dictionary<int, PcMissileFullVisual> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcMissileFullVisual v)
        {
            if (v == null || v.missileId <= 0) return;
            _byId[v.missileId] = v;
        }

        public PcMissileFullVisual Get(int missileId)
            => _byId.TryGetValue(missileId, out var v) ? v : null;

        public bool TryGet(int missileId, out PcMissileFullVisual v)
            => _byId.TryGetValue(missileId, out v);

        private static void RegisterCodePages()
        {
            try
            {
                var pt = System.Type.GetType("System.Text.CodePagesEncodingProvider, System.Text.Encoding.CodePages");
                var prov = pt?.GetProperty("Instance")?.GetValue(null, null) as System.Text.EncodingProvider;
                if (prov != null) System.Text.Encoding.RegisterProvider(prov);
            }
            catch { }
        }

        /// <summary>
        /// Parse missles1.txt (57 columns) into full visual entries.
        /// Returns registry ready for lookup.
        /// </summary>
        public static PcMissileFullVisualRegistry ParseFromFile(string path)
        {
            RegisterCodePages();
            var reg = new PcMissileFullVisualRegistry();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return reg;

            string[] lines;
            try
            {
                // missles/missles1 store SPR paths as GB2312/GBK Chinese paths
                // (e.g. \spr\skill\丐帮\mag_gb_01_投石问路.spr).  PcText's
                // Vietnamese-friendly decoding can mojibake those path bytes,
                // which then hashes to non-existent UIDs and makes combat VFX
                // fall back to dots/rings.  For visual asset lookup, preserve
                // the PC Chinese path by decoding this table as GB2312 directly.
                var encoding = Encoding.GetEncoding(PcItemCommon.GbkFallbackEncoding);
                lines = File.ReadAllText(path, encoding)
                    .Replace("\r\n", "\n")
                    .Replace('\r', '\n')
                    .Split('\n');
            }
            catch
            {
                try { lines = File.ReadAllLines(path); } catch { return reg; }
            }

            if (lines.Length < 2) return reg;

            // Parse header to get column indices
            var header = lines[0].Split('\t');
            var col = new Dictionary<string, int>();
            for (int i = 0; i < header.Length; i++)
                col[header[i].Trim()] = i;

            for (int li = 1; li < lines.Length; li++)
            {
                var line = lines[li];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var c = line.Split('\t');

                int id = IntVal(c, col, "MissleId");
                if (id <= 0) continue;

                var entry = new PcMissileFullVisual
                {
                    missileId = id,
                    nameRaw = StrVal(c, col, "MissleName"),
                    moveKind = IntVal(c, col, "MoveKind"),
                    followKind = IntVal(c, col, "FollowKind"),
                    missleHeight = IntVal(c, col, "MissleHeight"),
                    collidRange = IntVal(c, col, "CollidRange"),
                    isRangeDmg = IntVal(c, col, "IsRangeDmg"),
                    dmgRange = IntVal(c, col, "DmgRange"),
                    dmgInterval = IntVal(c, col, "DmgInterval"),
                    speed = IntVal(c, col, "Speed"),
                    lifetime = IntVal(c, col, "LifeTime"),
                    zspeed = IntVal(c, col, "Zspeed"),
                    zacc = IntVal(c, col, "Zacc"),
                    loopPlay = IntVal(c, col, "LoopPlay"),
                    subLoop = IntVal(c, col, "SubLoop"),
                    subStart = IntVal(c, col, "SubStart"),
                    subStop = IntVal(c, col, "SubStop"),
                    responseSkill = IntVal(c, col, "ResponseSkill"),
                    canDestroy = IntVal(c, col, "CanDestroy"),
                    colVanish = IntVal(c, col, "ColVanish"),
                    canSlow = IntVal(c, col, "CanSlow"),
                    canColFriend = IntVal(c, col, "CanColFriend"),
                    autoExplode = IntVal(c, col, "AutoExplode"),
                    redLum = IntVal(c, col, "RedLum"),
                    greenLum = IntVal(c, col, "GreenLum"),
                    blueLum = IntVal(c, col, "BlueLum"),
                    lightRadius = IntVal(c, col, "LightRadius"),
                };

                // Parse flight animation slots (AnimFile1-4, AnimFileInfo1-4, SndFile1-4)
                for (int i = 1; i <= 4; i++)
                {
                    entry.flightAnims[i - 1] = ParseAnimSlot(c, col,
                        $"AnimFile{i}", $"AnimFileInfo{i}", $"SndFile{i}");
                }

                // Parse explosion animation slots (AnimFileB1-4, AnimFileInfoB1-4, SndFileB1-4)
                for (int i = 1; i <= 4; i++)
                {
                    entry.explodeAnims[i - 1] = ParseAnimSlot(c, col,
                        $"AnimFileB{i}", $"AnimFileInfoB{i}", $"SndFileB{i}");
                }

                reg.Register(entry);
            }

            SubsystemLog.Info("MissileVisual", $"Parsed {reg.Count} missile visual entries");
            return reg;
        }

        private static MissileAnimSlot ParseAnimSlot(string[] c, Dictionary<string, int> col,
            string sprCol, string infoCol, string sndCol)
        {
            var spr = StrVal(c, col, sprCol);
            var info = StrVal(c, col, infoCol);
            var snd = StrVal(c, col, sndCol);

            if (string.IsNullOrEmpty(spr) && string.IsNullOrEmpty(info))
                return null;

            var slot = new MissileAnimSlot
            {
                sprPath = spr,
                soundPath = snd,
            };

            // Parse info: "frames,directions,interval"
            if (!string.IsNullOrEmpty(info))
            {
                var parts = info.Split(',');
                if (parts.Length >= 1 && int.TryParse(parts[0], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int f)) slot.totalFrames = f;
                if (parts.Length >= 2 && int.TryParse(parts[1], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int d)) slot.directions = d;
                if (parts.Length >= 3 && int.TryParse(parts[2], NumberStyles.Integer,
                    CultureInfo.InvariantCulture, out int t)) slot.intervalTicks = t;
            }

            return slot;
        }

        private static string StrVal(string[] c, Dictionary<string, int> col, string name)
        {
            if (!col.TryGetValue(name, out int idx) || idx < 0 || idx >= c.Length)
                return string.Empty;
            return (c[idx] ?? string.Empty).Trim();
        }

        private static int IntVal(string[] c, Dictionary<string, int> col, string name)
        {
            var s = StrVal(c, col, name);
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }
    }
}
