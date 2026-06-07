using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Manages the 2 combat skill slots (left and right) with skill assignment
    /// and auto-target attack flow. Matches the mobile mockup layout:
    /// - Tap empty slot → open skill picker to assign a skill
    /// - Tap assigned slot → auto-attack nearest enemy
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatSkillSlotController : MonoBehaviour
    {
        [Header("Config")]
        public int leftSlotSkillId;
        public int rightSlotSkillId;

        private VisualElement _leftSlot;
        private VisualElement _rightSlot;
        private VisualElement _skillPickerOverlay;
        private VisualElement _skillPickerWindow;
        private VisualElement _skillPickerClose;
        private ScrollView _skillPickerList;
        private Label _skillPickerTitle;
        private VisualElement _leftSkillIcon;
        private VisualElement _rightSkillIcon;
        private Label _leftSkillLabel;
        private Label _rightSkillLabel;

        private const long LongPressMs = 450;
        // Finger jitter while holding must not cancel the long-press; only a real drag does.
        private const float SlotDragCancelThreshold = 45f;

        private int _activeSlot = -1; // 0=left, 1=right
        private int _pressedSlot = -1;
        private int _pressedPointerId = -1;
        private Vector2 _startPointerPos;
        private bool _slotPointerDown;
        private bool _longPressOpened;
        private bool _initialized;

        // Picker tap tracking — ScrollView captures the pointer for drag-scroll on
        // touch, which swallows ClickEvent on child items, so we detect the tap at
        // list level: record press pos, and on release within a small move threshold
        // assign the skill under the release point.
        private Vector2 _pickerPressPos;
        private bool _pickerPointerDown;
        private const float PickerTapMoveThreshold = 12f;
        private SkillCatalog _catalog;
        private PlayerProgressionState _progression;

        public int LeftSkillId => leftSlotSkillId;
        public int RightSkillId => rightSlotSkillId;
        public bool IsPickerVisible => _skillPickerOverlay != null && !_skillPickerOverlay.ClassListContains("hidden");

        private void Start()
        {
            BindElements();
        }

        private void BindElements()
        {
            if (_initialized) return;

            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;

            // Auto-assign default hotbar skills if empty
            if (leftSlotSkillId == 0 && rightSlotSkillId == 0)
            {
                var manager = SandboxManager.Instance;
                if (manager != null && manager.PlayerProgression != null)
                {
                    var prog = manager.PlayerProgression;
                    GetDefaultSkillsForFaction(prog.faction, out int defaultLeft, out int defaultRight);
                    if (prog.knownSkills.Contains(defaultLeft)) leftSlotSkillId = defaultLeft;
                    if (prog.knownSkills.Contains(defaultRight)) rightSlotSkillId = defaultRight;
                }
            }

            var root = doc.rootVisualElement.Q("GameHud");
            if (root == null) return;

            _leftSlot = root.Q("LeftSkillSlot");
            _rightSlot = root.Q("RightSkillSlot");

            // The skill panel is absolute-positioned over the baked T/P art. ToolbarRight
            // (menu buttons incl. "Kỹ năng") and BtnTreasure are later flex siblings, so
            // without this they paint over the T/P center and steal the tap (= tap opens the
            // skill panel / treasure instead, and long-press never reaches the slot handler).
            // GameHudController rebuilds BottomPanel children after bind, which pushes our
            // panel back behind ToolbarRight, so a one-shot BringToFront() does NOT stick.
            // Re-assert front order on every layout change of the parent.
            var skillPanel = _leftSlot?.parent;
            if (skillPanel?.parent != null)
            {
                skillPanel.BringToFront();
                skillPanel.parent.RegisterCallback<GeometryChangedEvent>(_ => skillPanel.BringToFront());
            }
            // ROOT-LEVEL TRICKLE-DOWN INTERCEPT (bulletproof against sibling-order rebuilds):
            // Unity routes pointer events by VisualElement hierarchy, NOT draw order. After a
            // BottomPanel rebuild, ToolbarRight/BtnTreasure become the pick-target over the
            // T/P slots and eat the tap, so BringToFront alone is unreliable. Per Unity's own
            // guidance, intercept PointerDown at the root during the trickle-down phase, hit-test
            // the T/P slot bounds ourselves, and StopImmediatePropagation so no menu button can
            // steal the tap. Pointer capture (set in BeginSlotPress) then routes move/up/cancel.
            root.RegisterCallback<PointerDownEvent>(OnRootPointerDownCapture, TrickleDown.TrickleDown);
            _skillPickerOverlay = root.Q("SkillPickerOverlay");
            _skillPickerWindow = root.Q("SkillPickerWindow");
            _skillPickerClose = root.Q("SkillPickerClose");
            _skillPickerList = root.Q<ScrollView>("SkillPickerList");
            _skillPickerTitle = root.Q<Label>("SkillPickerTitle");

            if (_leftSlot != null)
            {
                _leftSkillIcon = _leftSlot.Q("SlotIcon");
                _leftSkillLabel = _leftSlot.Q<Label>("SlotLabel");
                _leftSlot.pickingMode = PickingMode.Position;
                _leftSlot.RegisterCallback<PointerDownEvent>(OnLeftSlotDown);
                _leftSlot.RegisterCallback<PointerMoveEvent>(OnSlotMove);
                _leftSlot.RegisterCallback<PointerUpEvent>(OnSlotUp);
                _leftSlot.RegisterCallback<PointerCancelEvent>(OnSlotCancel);
            }

            if (_rightSlot != null)
            {
                _rightSkillIcon = _rightSlot.Q("SlotIcon");
                _rightSkillLabel = _rightSlot.Q<Label>("SlotLabel");
                _rightSlot.pickingMode = PickingMode.Position;
                _rightSlot.RegisterCallback<PointerDownEvent>(OnRightSlotDown);
                _rightSlot.RegisterCallback<PointerMoveEvent>(OnSlotMove);
                _rightSlot.RegisterCallback<PointerUpEvent>(OnSlotUp);
                _rightSlot.RegisterCallback<PointerCancelEvent>(OnSlotCancel);
            }

            if (_skillPickerWindow != null)
            {
                _skillPickerWindow.pickingMode = PickingMode.Position;
                _skillPickerWindow.RegisterCallback<PointerDownEvent>(evt => evt.StopPropagation());
                _skillPickerWindow.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
                _skillPickerWindow.RegisterCallback<PointerUpEvent>(evt => evt.StopPropagation());
            }

            if (_skillPickerClose != null)
            {
                _skillPickerClose.pickingMode = PickingMode.Position;
                _skillPickerClose.RegisterCallback<PointerDownEvent>(evt =>
                {
                    CloseSkillPicker();
                    evt.StopPropagation();
                });
            }

            if (_skillPickerOverlay != null)
            {
                _skillPickerOverlay.pickingMode = PickingMode.Position;
                _skillPickerOverlay.RegisterCallback<PointerDownEvent>(evt =>
                {
                    // Tap outside list closes picker; list itself stops propagation so touch-drag scroll works.
                    CloseSkillPicker();
                    evt.StopPropagation();
                });
            }

            if (_skillPickerList != null)
            {
                _skillPickerList.pickingMode = PickingMode.Position;
                _skillPickerList.mode = ScrollViewMode.Vertical;
                _skillPickerList.verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;
                _skillPickerList.RegisterCallback<PointerDownEvent>(evt =>
                {
                    _pickerPressPos = (Vector2)evt.position;
                    _pickerPointerDown = true;
                    evt.StopPropagation();
                });
                _skillPickerList.RegisterCallback<PointerMoveEvent>(evt => evt.StopPropagation());
                _skillPickerList.RegisterCallback<PointerUpEvent>(evt =>
                {
                    evt.StopPropagation();
                    if (!_pickerPointerDown) return;
                    _pickerPointerDown = false;
                    // Treat as a tap only if the pointer barely moved (otherwise it was a scroll drag).
                    if (Vector2.Distance((Vector2)evt.position, _pickerPressPos) > PickerTapMoveThreshold)
                        return;
                    AssignSkillUnderPoint((Vector2)evt.position);
                });
                _skillPickerList.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
            }

            _initialized = true;
            RefreshSlotVisuals();
        }

        /// <summary>Initialize with catalog and progression data.</summary>
        public void Initialize(SkillCatalog catalog, PlayerProgressionState progression)
        {
            _catalog = catalog;
            _progression = progression;
            BindElements();
        }

        private void GetDefaultSkillsForFaction(CombatFaction faction, out int leftSkill, out int rightSkill)
        {
            switch (faction)
            {
                case CombatFaction.CaiBang:
                    leftSkill = 357;
                    rightSkill = 359;
                    break;
                case CombatFaction.WuDang:
                    leftSkill = 153;
                    rightSkill = 155;
                    break;
                case CombatFaction.Shaolin:
                    leftSkill = 10;
                    rightSkill = 11;
                    break;
                case CombatFaction.TangMen:
                    leftSkill = 47;
                    rightSkill = 58;
                    break;
                case CombatFaction.EMei:
                    leftSkill = 80;
                    rightSkill = 91;
                    break;
                case CombatFaction.TianWang:
                    leftSkill = 40;
                    rightSkill = 41;
                    break;
                case CombatFaction.WuDu:
                    leftSkill = 63;
                    rightSkill = 65;
                    break;
                case CombatFaction.CuiYan:
                    leftSkill = 99;
                    rightSkill = 105;
                    break;
                case CombatFaction.TianRen:
                    leftSkill = 142;
                    rightSkill = 148;
                    break;
                case CombatFaction.KunLun:
                    leftSkill = 172;
                    rightSkill = 182;
                    break;
                default:
                    leftSkill = 0;
                    rightSkill = 0;
                    break;
            }
        }

        /// <summary>Assign a skill to a specific slot.</summary>
        public void AssignSkill(int slot, int skillId)
        {
            if (slot == 0) leftSlotSkillId = skillId;
            else if (slot == 1) rightSlotSkillId = skillId;
            RefreshSlotVisuals();
            SubsystemLog.Info("Combat", $"Assigned skill {skillId} to slot {slot}");
        }

        /// <summary>Clear a slot (remove assigned skill).</summary>
        public void ClearSlot(int slot)
        {
            AssignSkill(slot, 0);
        }

        private void OnLeftSlotDown(PointerDownEvent evt)
        {
            BeginSlotPress(0, evt.pointerId, evt.position);
            evt.StopPropagation();
        }

        private void OnRightSlotDown(PointerDownEvent evt)
        {
            BeginSlotPress(1, evt.pointerId, evt.position);
            evt.StopPropagation();
        }

        // Root trickle-down intercept: fires BEFORE the event reaches ToolbarRight/BtnTreasure,
        // so a tap on the T/P slot area always starts a slot press — immune to sibling-order
        // rebuilds that would otherwise let a menu button become the pick-target and eat the tap.
        private void OnRootPointerDownCapture(PointerDownEvent evt)
        {
            var pos = (Vector2)evt.position;
            if (_leftSlot != null && _leftSlot.worldBound.Contains(pos))
            {
                BeginSlotPress(0, evt.pointerId, evt.position);
                evt.StopImmediatePropagation();
            }
            else if (_rightSlot != null && _rightSlot.worldBound.Contains(pos))
            {
                BeginSlotPress(1, evt.pointerId, evt.position);
                evt.StopImmediatePropagation();
            }
        }

        private void BeginSlotPress(int slot, int pointerId, Vector2 screenPos)
        {
            _pressedSlot = slot;
            _pressedPointerId = pointerId;
            _startPointerPos = screenPos;
            _slotPointerDown = true;
            _longPressOpened = false;
            (slot == 0 ? _leftSlot : _rightSlot)?.CapturePointer(pointerId);
            StartCoroutine(OpenPickerAfterLongPress(slot, pointerId));
        }

        private IEnumerator OpenPickerAfterLongPress(int slot, int pointerId)
        {
            yield return new WaitForSeconds(LongPressMs / 1000f);
            if (!_slotPointerDown || _pressedSlot != slot || _pressedPointerId != pointerId) yield break;
            _longPressOpened = true;
            OpenSkillPicker(slot);
        }

        private void OnSlotMove(PointerMoveEvent evt)
        {
            // Only a real drag (finger moved far) cancels the long-press; small jitter
            // while holding must NOT abort it, otherwise the picker never opens on device.
            if (_slotPointerDown && !_longPressOpened &&
                Vector2.Distance((Vector2)evt.position, _startPointerPos) > SlotDragCancelThreshold)
            {
                CancelSlotPress();
            }
            evt.StopPropagation();
        }

        private void OnSlotUp(PointerUpEvent evt)
        {
            int slot = _pressedSlot;
            (slot == 0 ? _leftSlot : _rightSlot)?.ReleasePointer(evt.pointerId);
            _slotPointerDown = false;
            _pressedSlot = -1;
            _pressedPointerId = -1;

            if (!_longPressOpened && slot >= 0)
            {
                int skillId = slot == 0 ? leftSlotSkillId : rightSlotSkillId;
                if (skillId > 0) TriggerSkillSlot(slot, skillId);
                else OpenSkillPicker(slot);
            }

            _longPressOpened = false;
            evt.StopPropagation();
        }

        private void OnSlotCancel(PointerCancelEvent evt)
        {
            // Device fires PointerCancel on finger jitter / capture loss while holding.
            // Do NOT kill the press here — that would stop the long-press from ever opening
            // the picker. Just release the pointer capture; the Update-independent coroutine
            // keeps running and opens the picker at 450ms unless a real PointerUp arrives.
            int slot = _pressedSlot;
            if (slot >= 0)
                (slot == 0 ? _leftSlot : _rightSlot)?.ReleasePointer(_pressedPointerId);
            evt.StopPropagation();
        }

        private void CancelSlotPress()
        {
            int slot = _pressedSlot;
            if (slot >= 0)
            {
                (slot == 0 ? _leftSlot : _rightSlot)?.ReleasePointer(_pressedPointerId);
            }
            _slotPointerDown = false;
            _pressedSlot = -1;
            _pressedPointerId = -1;
            _longPressOpened = false;
        }

        /// <summary>Open the skill picker overlay for a specific slot.</summary>
        public void OpenSkillPicker(int slot)
        {
            _activeSlot = slot;
            BindElements();

            if (_skillPickerOverlay != null)
            {
                _skillPickerOverlay.RemoveFromClassList("hidden");
            }
            if (_skillPickerTitle != null)
            {
                _skillPickerTitle.text = slot == 0 ? "Chọn kỹ năng (Trái)" : "Chọn kỹ năng (Phải)";
            }
            PopulateSkillPicker();

            SubsystemLog.Info("Combat", $"Skill picker opened for slot {slot}");
        }

        /// <summary>Close the skill picker overlay.</summary>
        public void CloseSkillPicker()
        {
            _activeSlot = -1;
            if (_skillPickerOverlay != null)
            {
                _skillPickerOverlay.AddToClassList("hidden");
            }
        }

        private void PopulateSkillPicker()
        {
            if (_skillPickerList == null) return;
            _skillPickerList.Clear();

            var catalog = _catalog ?? SandboxManager.Instance?.CombatSkillCatalog;
            if (catalog == null) return;

            var progression = _progression ?? SandboxManager.Instance?.PlayerProgression;
            string artPath = HudArtPathResolver.ResolveGeneratedArtRoot("UI/HUD/Art");

            CombatFaction playerFaction = progression?.faction ?? CombatFaction.CaiBang;
            var factionSkillOrder = PcSkillPanelService.GetPcSkillOrder(playerFaction);

            var activeSkillIds = new System.Collections.Generic.List<int>();
            foreach (var skillId in factionSkillOrder)
            {
                if (skillId == PcSkillPanelService.NpcVariantSkillId)
                    continue;

                var skill = catalog.Resolve(skillId);
                if (skill == null) continue;

                if (skill.skillStyle == PcSkillStyle.PassivityNpcState)
                    continue;

                int learnedLevel = progression?.GetSkillLevel(skill.skillId) ?? 0;
                if (learnedLevel <= 0) continue;

                activeSkillIds.Add(skillId);
            }

            foreach (var skillId in activeSkillIds)
            {
                var skill = catalog.Resolve(skillId);
                if (skill == null) continue;

                int learnedLevel = progression?.GetSkillLevel(skill.skillId) ?? 0;
                if (learnedLevel <= 0) continue; // Only show learned skills

                var item = new VisualElement();
                item.AddToClassList("skill-picker-item");
                item.pickingMode = PickingMode.Position;

                var icon = new VisualElement();
                icon.AddToClassList("skill-picker-icon");
                GameHudController.LoadIconStatic(this, icon, artPath, $"cai_bang_skill_{skillId}");
                item.Add(icon);

                var info = new VisualElement();
                info.AddToClassList("skill-picker-info");

                var nameLabel = new Label(skill.DisplayName);
                nameLabel.AddToClassList("skill-picker-name");
                info.Add(nameLabel);

                var levelLabel = new Label($"Cấp {learnedLevel}");
                levelLabel.AddToClassList("skill-picker-level");
                info.Add(levelLabel);

                var typeLabel = new Label(skill.missileForm switch
                {
                    SkillMissileForm.Single => "Tấn công đơn mục tiêu",
                    SkillMissileForm.Surround => "Tấn công phạm vi",
                    SkillMissileForm.Fan => "Tấn công hình quạt",
                    _ => skill.isAura ? "Trận pháp" : "Hỗ trợ",
                });
                typeLabel.AddToClassList("skill-picker-type");
                info.Add(typeLabel);

                item.Add(info);

                int capturedId = skillId;
                item.userData = capturedId; // used by AssignSkillUnderPoint (touch tap path)
                item.RegisterCallback<ClickEvent>(evt =>
                {
                    AssignSkill(_activeSlot, capturedId);
                    CloseSkillPicker();
                    evt.StopPropagation();
                });

                _skillPickerList.Add(item);
            }
        }

        /// <summary>
        /// Assign the skill whose picker item is under the given panel point. Used by the
        /// touch tap path because ScrollView pointer-capture swallows ClickEvent on items.
        /// </summary>
        private void AssignSkillUnderPoint(Vector2 panelPoint)
        {
            if (_skillPickerList == null) return;
            var container = _skillPickerList.contentContainer;
            foreach (var child in container.Children())
            {
                if (!child.ClassListContains("skill-picker-item")) continue;
                if (!child.worldBound.Contains(panelPoint)) continue;
                if (child.userData is int skillId)
                {
                    AssignSkill(_activeSlot, skillId);
                    CloseSkillPicker();
                }
                return;
            }
        }

        private void RefreshSlotVisuals()
        {
            string artPath = HudArtPathResolver.ResolveGeneratedArtRoot("UI/HUD/Art");

            // The bottom bar bakes PC fist art into the background. The USS rule
            // `#BottomPanel .hud-skill-slot-icon { display: none; }` hides slot icons
            // so that baked art shows through on EMPTY slots. When a skill is assigned
            // we override that inline (inline style beats USS) so the assigned skill
            // icon draws over the baked fist — matching PC where T/P show the bound skill.
            if (_leftSkillIcon != null)
            {
                if (leftSlotSkillId > 0)
                {
                    GameHudController.LoadIconStatic(this, _leftSkillIcon, artPath, $"cai_bang_skill_{leftSlotSkillId}");
                    _leftSkillIcon.RemoveFromClassList("empty");
                    _leftSkillIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _leftSkillIcon.style.backgroundImage = new StyleBackground();
                    _leftSkillIcon.AddToClassList("empty");
                    _leftSkillIcon.style.display = StyleKeyword.Null; // fall back to USS (hidden in bottom bar)
                }
            }

            if (_rightSkillIcon != null)
            {
                if (rightSlotSkillId > 0)
                {
                    GameHudController.LoadIconStatic(this, _rightSkillIcon, artPath, $"cai_bang_skill_{rightSlotSkillId}");
                    _rightSkillIcon.RemoveFromClassList("empty");
                    _rightSkillIcon.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _rightSkillIcon.style.backgroundImage = new StyleBackground();
                    _rightSkillIcon.AddToClassList("empty");
                    _rightSkillIcon.style.display = StyleKeyword.Null; // fall back to USS (hidden in bottom bar)
                }
            }

            UpdateSlotLabel(_leftSkillLabel, leftSlotSkillId);
            UpdateSlotLabel(_rightSkillLabel, rightSlotSkillId);
        }

        private void UpdateSlotLabel(Label label, int skillId)
        {
            if (label == null) return;
            if (skillId <= 0)
            {
                label.text = "+";
                return;
            }
            var catalog = _catalog ?? SandboxManager.Instance?.CombatSkillCatalog;
            var skill = catalog?.Resolve(skillId);
            label.text = skill?.DisplayName ?? $"Skill {skillId}";
        }

        /// <summary>Trigger a skill slot — auto-target nearest enemy and cast.</summary>
        public void TriggerSkillSlot(int slot, int skillId)
        {
            var manager = SandboxManager.Instance;
            if (manager == null) return;

            var catalog = manager.CombatSkillCatalog;
            var skill = catalog?.Resolve(skillId);
            if (skill == null) return;

            // Get caster position from player controller
            var player = manager.PlayerController;
            if (player == null) return;

            Vector2 casterPos = player.transform.position;

            // Play the PC character cast animation selected by Skills.txt CharAnimId.
            // PC KNpc.cpp uses m_CastFrame (default 20 ticks) for non-physical magic skills.
            // The animation suffix depends on equipped weapon (MG01=empty, MG04=staff).
            player.PlayPcSkillAction(skill.charAnimId, PcCastAnimationDurationSeconds(skill));

            // Get enemies in scene
            var enemies = CollectEnemies();

            // Find nearest enemy in range. PC Kháng Long VM tuning expands range by level.
            int skillLevel = manager.PlayerProgression?.skillLevels.TryGetValue(skillId, out var lv) == true ? lv : skill.maxLevel;
            var targetService = new CombatAutoTargetService();
            var target = targetService.FindNearestEnemy(casterPos, skill, enemies, skillLevel);

            if (target != null)
            {
                // Face toward target
                int facing = CombatAutoTargetService.ComputeFacing8Way(casterPos, target.position);
                player.SetFacing(facing);

                // Cast the skill
                var combatRuntime = manager.CombatRuntime;
                if (combatRuntime != null)
                {
                    var caster = CreateCombatActor(player, skill);
                    var targetActor = CreateTargetActor(target);

                    var report = combatRuntime.Cast(
                        caster, targetActor, skillId,
                        target.position, CombatRelation.Enemy);

                    if (report.success)
                    {
                        // Play skill visual effect first. PC applies damage when missile/skill resolves,
                        // so mirror HP to the live BaLangEnemyAi when the visual reaches impact.
                        var effectService = manager.SkillEffectVisual;
                        // PC missiles track the enemy NPC's current position each tick.
                        // Capture a callback that returns the live target's transform position so
                        // the visual effect chases the enemy as it moves (homing).
                        BaLangEnemyAi liveTarget = target.enemyBehaviour;
                        System.Func<Vector2> currentTargetPos = liveTarget != null
                            ? (System.Func<Vector2>)(() => (Vector2)liveTarget.transform.position)
                            : null;
                        var fx = effectService?.PlaySkillCast(skill, casterPos, target.position, report.skillLevel, currentTargetPos);
                        if (target.enemyBehaviour != null)
                            StartCoroutine(ApplyLiveEnemyHpAtImpact(target, targetActor.currentLife, skillId, report.skillLevel, report, fx));

                        SubsystemLog.Info("Combat",
                            $"Cast {skill.DisplayName} → {target.name} " +
                            $"(dmg={report.damageResults.Count}, pendingHp={targetActor.currentLife}, range={target.distance:F0})");
                    }
                    else
                    {
                        SubsystemLog.Warn("Combat",
                            $"Cast {skill.DisplayName} FAILED: {report.reason} — {report.detail}");
                    }
                }
            }
            else
            {
                // No enemy in range — still play cast animation but no damage
                var effectService = manager.SkillEffectVisual;
                effectService?.PlaySkillCast(skill, casterPos, casterPos + new Vector2(0, -50f), 1);
                SubsystemLog.Info("Combat", $"Cast {skill.DisplayName} — no enemy in range");
            }
        }

        private static float PcCastAnimationDurationSeconds(SkillDefinition skill)
        {
            if (skill == null) return 0f;
            if (skill.charAnimId == 14) return 0f; // cdo_none
            // PC default player CastSpeed is 20 ticks, advanced by SubWorld.m_dwCurrentTime (~18 ticks/sec).
            return 20f / 18f;
        }

        private IEnumerator ApplyLiveEnemyHpAtImpact(CombatTargetInfo target, int hp, int skillId, int skillLevel, CombatCastReport report, ActiveSkillEffect fx)
        {
            if (target?.enemyBehaviour == null) yield break;
            if (fx == null)
            {
                target.enemyBehaviour.SetLife(hp);
                yield break;
            }

            while (fx.phase != SkillEffectPhase.Impact && fx.phase != SkillEffectPhase.Finished)
                yield return null;

            if (target.enemyBehaviour != null)
            {
                target.enemyBehaviour.SetLife(hp);

                if (skillId == 357 && skillLevel >= 11)
                {
                    var manager = SandboxManager.Instance;
                    if (manager != null)
                    {
                        var subSkill = manager.CombatSkillCatalog?.Resolve(389);
                        if (subSkill != null)
                        {
                            manager.SkillEffectVisual?.PlaySkillCast(subSkill, target.position, target.position, skillLevel);
                        }
                    }

                    if (report.damageResults.Count > 1)
                    {
                        int aoeDamage = report.damageResults[1].finalDamage;
                        var allEnemies = CollectEnemies();
                        foreach (var enemy in allEnemies)
                        {
                            if (enemy.enemyBehaviour != null && enemy.enemyBehaviour != target.enemyBehaviour && enemy.alive)
                            {
                                float dist = Vector2.Distance(target.position, enemy.position);
                                if (dist <= 3.0f)
                                {
                                    int newLife = Mathf.Max(0, enemy.currentLife - aoeDamage);
                                    enemy.enemyBehaviour.SetLife(newLife);
                                }
                            }
                        }
                    }
                }
            }
        }

        private List<EnemyRuntimeInfo> CollectEnemies()
        {
            var enemies = new List<EnemyRuntimeInfo>();
            var runtime = SandboxManager.Instance?.EnemyRuntime;
            if (runtime == null) return enemies;

            // Collect from BaLangEnemySpawnRuntime
            var spawns = runtime.GetActiveEnemies();
            if (spawns != null)
            {
                foreach (var spawn in spawns)
                {
                    enemies.Add(new EnemyRuntimeInfo
                    {
                        enemyId = spawn.enemyId,
                        displayName = spawn.displayName,
                        position = spawn.position,
                        alive = spawn.alive,
                        currentLife = spawn.currentLife,
                        maxLife = spawn.maxLife,
                        enemyBehaviour = spawn.enemyBehaviour,
                    });
                }
            }
            return enemies;
        }

        private CombatActorState CreateCombatActor(SandboxPlayerController player, SkillDefinition skill)
        {
            var manager = SandboxManager.Instance;
            var progression = _progression ?? manager?.PlayerProgression ?? new PlayerProgressionState();

            // Ensure progression has been granted so level/skills are populated.
            if (manager != null)
            {
                manager.GrantFactionSkillPanelProgression(progression.faction);
            }

            int playerLevel = progression.level;
            int playerMana = PcMaxManaFormula.Compute(playerLevel, 0, progression.faction);
            var actor = new CombatActorState
            {
                actorId = 1,
                faction = progression.faction,
                level = playerLevel,
                fightMode = true,
                position = player.transform.position,
                currentMana = playerMana,
                currentLife = 100,
                maxLife = 100,
            };

            // Copy known skills and levels from progression (populated by GrantFactionSkillPanelProgression).
            foreach (var id in progression.knownSkills)
                actor.knownSkills.Add(id);
            foreach (var kv in progression.skillLevels)
                actor.skillLevels[kv.Key] = kv.Value > 0 ? kv.Value : 1;

            // Ensure the active skill has at least level 1
            if (!actor.skillLevels.ContainsKey(skill.skillId) || actor.skillLevels[skill.skillId] <= 0)
                actor.skillLevels[skill.skillId] = 1;

            return actor;
        }

        private CombatActorState CreateTargetActor(CombatTargetInfo target)
        {
            return new CombatActorState
            {
                actorId = target.enemyId + 1000,
                faction = CombatFaction.None,
                position = target.position,
                currentLife = target.currentLife,
                maxLife = target.maxLife,
            };
        }
    }
}
