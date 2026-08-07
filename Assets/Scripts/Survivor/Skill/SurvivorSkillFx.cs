// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillFx
// Adapter reusing Sandbox PC frame-exact skill VFX (SkillEffectVisualService +
// SkillEffectWorldOverlay) for Survivor's 4 active Cái Bang skills (128/125/1073/1074).
//
// Owns a private SkillEffectVisualService (SprRuntimeService) + a world-space
// overlay GameObject. Does NOT touch SandboxManager or any Sandbox code — the
// overlay's Service field is injected so its LateUpdate renders our service's
// ActiveSkillEffects instead of the SandboxManager singleton.
//
// UNIT BRIDGE (world parity Survivor vs Sandbox): the Sandbox pipeline simulates
// in PC pixel space (1 unit = 1 px, SPRs ppu=1, Sandbox camera ortho 300) while
// Survivor runs in ÷PxPerUnit units (player SPRs ppu=40, camera ortho 6). Without
// conversion a 194px fire-dragon SPR rendered at ppu=1 dwarfs the 12-unit viewport
// (~16×) → invisible, and px-space speeds/radii make the missile arrive instantly.
// Cast() therefore normalizes the effect to Survivor units (÷40) + scale 1/40.
//
// Visual ONLY: gameplay damage is handled separately by SkillCastSpawner
// (MeleeHit / SpawnProjectile). onMissileCollided is NOT wired (no double damage).
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Sprites;
using VLTK.UI;

namespace VLTK.Survivor
{
    public sealed class SurvivorSkillFx : MonoBehaviour
    {
        private SkillEffectVisualService _service;
        private SkillEffectWorldOverlay _overlay;
        private GameObject _overlayGo;

        /// <summary>The owned SkillEffectVisualService (advanced the overlay every frame).</summary>
        public SkillEffectVisualService Service => _service;

        private void Awake()
        {
            _service = new SkillEffectVisualService(new SprRuntimeService());
            _overlayGo = new GameObject("SurvivorSkillFxOverlay");
            _overlay = _overlayGo.AddComponent<SkillEffectWorldOverlay>();
            _overlay.Service = _service;
        }

        private void Update()
        {
            if (_service != null)
                _service.Update(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_overlayGo != null) Destroy(_overlayGo);
        }

        /// <summary>
        /// Play PC frame-exact VFX (precast SPR animation + missile + impact) for a
        /// Cái Bang skill. ConfigureDataDrivenVisuals auto-resolves all SPR data from
        /// skillId via missles1.txt + PcCaiBangLuaLevelService. Gameplay damage is
        /// applied separately by SkillCastSpawner — this is visual-only.
        /// </summary>
        public void Cast(SkillDef def, Vector2 casterPos, Vector2 targetPos, int level)
            => Cast(def, casterPos, targetPos, level, null, null);

        /// <summary>
        /// Play PC frame-exact VFX with optional live target tracking (PC parity:
        /// KMissle.cpp MISSLE_MMK_Follow re-aims each tick toward the followed NPC).
        /// A non-null getCurrentTargetPos makes single missiles home toward the monster
        /// (SkillEffectVisualService single-missile path uses it regardless of MoveKind).
        /// Pass onMissileCollided = null so the service renders the impact SPR itself
        /// (SpawnCollideSubEffect). NormalizeToWorldUnits scales every effect position by
        /// 1/PxPerUnit AFTER PlaySkillCast, so getCurrentTargetPos must return values in
        /// that same post-normalization space (world / PxPerUnit) -- callers scale monster
        /// world positions by 1/PxPerUnit inside the lambda. Visual ONLY.
        /// </summary>
        public void Cast(SkillDef def, Vector2 casterPos, Vector2 targetPos, int level,
            Func<Vector2> getCurrentTargetPos,
            Action<ActiveSkillEffect, int, Vector2> onMissileCollided = null)
        {
            if (_service == null || def == null) return;
            var sd = BuildSkillDefinition(def);
            var fx = _service.PlaySkillCast(sd, casterPos, targetPos, Mathf.Max(1, level),
                getCurrentTargetPos, onMissileCollided);
            if (fx != null)
                NormalizeToWorldUnits(fx);
        }

        /// <summary>
        /// Convert a Sandbox ActiveSkillEffect from PC pixel space to Survivor world
        /// units (÷PxPerUnit). The service simulates positions/speeds/radii in PC px
        /// (SPR ppu=1, Sandbox camera ortho 300); Survivor's ÷40 world needs every
        /// px quantity scaled down and the SPR render scale set to 1/PxPerUnit so a
        /// 194px dragon renders ~4.9 units instead of 194 units (16× the viewport).
        /// Fail-safe: only touches the returned effect; Sandbox pipeline untouched.
        /// </summary>
        public static void NormalizeToWorldUnits(ActiveSkillEffect fx)
        {
            const float k = 1f / SkillCastRuntime.PxPerUnit; // 1/40

            fx.casterPos *= k;
            fx.targetPos *= k;
            fx.currentMissilePos *= k;
            fx.missileSpeed *= k;
            fx.pcMissileSpeedPerTick = Mathf.RoundToInt(fx.pcMissileSpeedPerTick * k);
            fx.arrivalRadius *= k;
            fx.rendRadius *= k;
            fx.missileDistance *= k;
            fx.pcSpriteRenderScale = k;

            ScaleArray(fx.missilePositions, k);
            ScaleArray(fx.missileOrigins, k);
            ScaleArray(fx.missileTargets, k);
            ScaleArray(fx.missileTargetOffsets, k);
            if (fx.rendPositions != null)
                for (int i = 0; i < fx.rendPositions.Count; i++)
                    fx.rendPositions[i] *= k;
        }

        private static void ScaleArray(Vector2[] arr, float k)
        {
            if (arr == null) return;
            for (int i = 0; i < arr.Length; i++)
                arr[i] *= k;
        }

        /// <summary>
        /// Build a SkillDefinition from Survivor SkillDef for PlaySkillCast.
        /// ConfigureDataDrivenVisuals resolves PC SPR/frame/direction data from
        /// skillId + childSkillId — only these fields must be correct.
        /// </summary>
        public static SkillDefinition BuildSkillDefinition(SkillDef def)
        {
            return new SkillDefinition
            {
                skillId = def.Id,
                nameRaw = def.Name,
                nameNormalized = def.Name,
                childSkillId = def.ChildMissileId,
                childSkillNum = def.ChildSkillNum,
                missileForm = MapMissileForm(def.Form),
                waitTime = def.WaitTime,
                timePerCast = Mathf.RoundToInt(def.TimePerCast),
                isMelee = def.IsMelee,
                attackRadius = def.AttackRadius,
                // PC precast SPR: pass the staged hash uid as effectSourceId.sourcePath so
                // ConfigureDataDrivenVisuals sees hasPreCast and the overlay resolves
                // SpritesRuntime/{uid}.spr directly (no GBK re-encode risk). Fail-closed
                // khi chưa staged: uid rỗng → null → không precast (giống cũ).
                effectSourceId = string.IsNullOrEmpty(def.PreCastSprUid)
                    ? null
                    : new SourceAssetId { sourcePath = def.PreCastSprUid },
            };
        }

        /// <summary>
        /// Map PC MisslesForm (col 19) → SkillMissileForm enum.
        /// Form 12 (melee) is outside the enum → None; visual via child missile / impact.
        /// Values 0-7 map 1:1 to the enum.
        /// </summary>
        public static SkillMissileForm MapMissileForm(int form)
        {
            if (form == 12) return SkillMissileForm.None;
            if (form >= 0 && form <= 7) return (SkillMissileForm)form;
            return SkillMissileForm.None;
        }
    }
}