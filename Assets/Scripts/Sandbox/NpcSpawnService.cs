using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>A live NPC instance produced from a spawn entry (sandbox placeholder).</summary>
    public class NpcInstance
    {
        public int instanceId;
        public NpcSpawn spawn;
        public NpcTemplate template;     // null if template unresolved
        public string spriteClipRef;     // resolved clip key, null if missing
        public bool spriteResolved;
        public Vector2 worldPosition;

        /// <summary>AC#3 — source ids surfaced by the inspector.</summary>
        public string InspectorSummary()
        {
            string tmpl = template != null ? template.templateId.ToString() : $"{spawn.templateId} (missing)";
            string script = !string.IsNullOrEmpty(spawn.scriptRef) ? spawn.scriptRef : "<none>";
            return $"spawn#{spawn.spawnIndex} template={tmpl} script={script} sprite={(spriteResolved ? spriteClipRef : "<missing>")}";
        }
    }

    /// <summary>
    /// M3.2 — NPC spawn manager for the sandbox. Pure C# (no MonoBehaviour) so it is
    /// fully EditMode-testable. Spawns placeholders from a region's spawn manifest
    /// when NPCs are toggled on (AC#1), resolves a decoded sprite/clip per template
    /// (AC#2), exposes source template/spawn/script ids for the inspector (AC#3), and
    /// despawns without reloading the map (AC#4). A MonoBehaviour driver maps each
    /// <see cref="NpcInstance"/> to a scene GameObject.
    /// </summary>
    public class NpcSpawnService
    {
        private readonly NpcTemplateRegistry _templates;
        private readonly List<NpcInstance> _live = new();
        private int _nextInstanceId = 1;

        public bool NpcsVisible { get; private set; }
        public IReadOnlyList<NpcInstance> Live => _live;
        public int LiveCount => _live.Count;

        public NpcSpawnService(NpcTemplateRegistry templates)
        {
            _templates = templates;
        }

        /// <summary>
        /// AC#1 — toggle NPC visibility. Turning on spawns instances from the manifest;
        /// turning off despawns them (AC#4). Returns the new visibility state.
        /// </summary>
        public bool ToggleNpcs(bool visible, RegionSpawnManifest manifest)
        {
            NpcsVisible = visible;
            if (visible) SpawnFrom(manifest);
            else DespawnAll();
            return NpcsVisible;
        }

        /// <summary>AC#1/AC#2 — spawn placeholders from a region manifest.</summary>
        public void SpawnFrom(RegionSpawnManifest manifest)
        {
            DespawnAll();
            if (manifest?.npcSpawns == null) return;

            foreach (var spawn in manifest.npcSpawns)
            {
                var template = _templates?.Resolve(spawn.templateId);
                bool spriteResolved = template != null && template.spriteResolved
                                      && !string.IsNullOrEmpty(template.spriteClipRef);
                var inst = new NpcInstance
                {
                    instanceId = _nextInstanceId++,
                    spawn = spawn,
                    template = template,
                    spriteClipRef = template?.spriteClipRef,
                    spriteResolved = spriteResolved,
                    worldPosition = new Vector2(spawn.posX, spawn.posY),
                };
                _live.Add(inst);
            }
            SubsystemLog.Info("NpcSpawn", $"Spawned {_live.Count} NPC placeholder(s)");
        }

        /// <summary>AC#3 — find a live instance by its id (e.g. clicked in scene).</summary>
        public NpcInstance GetInstance(int instanceId)
            => _live.Find(i => i.instanceId == instanceId);

        /// <summary>AC#4 — remove all NPCs without touching the loaded map.</summary>
        public void DespawnAll()
        {
            if (_live.Count == 0) return;
            int n = _live.Count;
            _live.Clear();
            SubsystemLog.Info("NpcSpawn", $"Despawned {n} NPC(s)");
        }
    }
}
