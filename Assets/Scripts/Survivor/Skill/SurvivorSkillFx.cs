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
// Visual ONLY: gameplay damage is handled separately by SkillCastSpawner
// (MeleeHit / SpawnProjectile). onMissileCollided is NOT wired (no double damage).
// -----------------------------------------------------------------------------

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
        {
            if (_service == null || def == null) return;
            var sd = BuildSkillDefinition(def);
            _service.PlaySkillCast(sd, casterPos, targetPos, Mathf.Max(1, level));
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