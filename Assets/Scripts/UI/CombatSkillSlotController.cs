using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Backend.Combat;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Mobile-first combat hotbar using PC-derived JX icon art.
    /// Layout semantics: 4 assignable skill slots, A/B deck switch,
    /// tap-to-auto-target, hold/drag-to-aim with cancel zone, and target lock.
    /// PC behavior source remains KNpc nearest-enemy style targeting via CombatAutoTargetService.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CombatSkillSlotController : MonoBehaviour
    {
        [Header("Legacy slot compatibility")]
        public int leftSlotSkillId;
        public int rightSlotSkillId;

        [Header("Mobile decks")]
        [SerializeField] private int[] deckASkillIds = new int[MobileSkillSlotCount];
        [SerializeField] private int[] deckBSkillIds = new int[MobileSkillSlotCount];

        public const int MobileSkillSlotCount = 5;
        private const float SlotDragCancelThreshold = 45f;
        private const float PickerTapMoveThreshold = 12f;
        private const int PrimaryAttackPseudoSlot = -2;

        private readonly VisualElement[] _skillSlots = new VisualElement[MobileSkillSlotCount];
        private readonly VisualElement[] _skillIcons = new VisualElement[MobileSkillSlotCount];
        private readonly Label[] _skillLabels = new Label[MobileSkillSlotCount];

        private VisualElement _primaryAttackBtn;
        private VisualElement _deckSwitchBtn;
        private Label _deckSwitchLabel;
        private VisualElement _cancelCastZone;
        private VisualElement _targetLockMarker;
        private Label _targetLockName;

        private VisualElement _skillPickerOverlay;
        private VisualElement _skillPickerWindow;
        private VisualElement _skillPickerClose;
        private ScrollView _skillPickerList;
        private Label _skillPickerTitle;

        private int _activeDeckIndex;
        private int _activeSlot = -1;
        private int _pressedSlot = -1;
        private int _pressedPointerId = -1;
        private Vector2 _startPointerPos;
        private bool _slotPointerDown;
        private bool _aimingDrag;
        private bool _initialized;
        private VisualElement _boundRoot;

        private Vector2 _pickerPressPos;
        private bool _pickerPointerDown;
        private SkillCatalog _catalog;
        private PlayerProgressionState _progression;

        private int _lockedTargetId = -1;
        private string _lockedTargetName = string.Empty;

        public int LeftSkillId => GetDeck(0)[0];
        public int RightSkillId => GetDeck(0)[1];
        public int ActiveDeckIndex => _activeDeckIndex;
        public int LockedTargetId => _lockedTargetId;
        public bool IsPickerVisible => _skillPickerOverlay != null && !_skillPickerOverlay.ClassListContains("hidden");
        public bool IsAimingDrag => _aimingDrag;
        public bool IsCancelCastVisible => _cancelCastZone != null && !_cancelCastZone.ClassListContains("hidden");

        private void Start()
        {
            BindElements();
        }

        private void Update()
        {
            EnsureRuntimeReady();
        }

        private void EnsureRuntimeReady()
        {
            if (_initialized && !IsBoundToCurrentVisualTree())
            {
                _initialized = false;
            }

            if (!_initialized)
            {
                BindElements();
            }
        }

        private bool IsBoundToCurrentVisualTree()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc?.rootVisualElement?.Q("GameHud");
            if (root == null || _boundRoot == null || !ReferenceEquals(root, _boundRoot))
                return false;
            return true;
        }

        /// <summary>Initialize with catalog and progression data.</summary>
        public void Initialize(SkillCatalog catalog, PlayerProgressionState progression)
        {
            _catalog = catalog;
            _progression = progression;
            BindElements();
        }

        private void BindElements()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc?.rootVisualElement?.Q("GameHud");

            if (_initialized)
            {
                if (root != null && _boundRoot != null && ReferenceEquals(root, _boundRoot))
                {
                    return;
                }
                _initialized = false;
            }

            EnsureDeckArrays();
            ImportLegacySlotsIfNeeded();
            FillDefaultDeckIfEmpty();
            MigrateCaiBangDeckToDefaultIfNeeded();

            if (doc == null || doc.rootVisualElement == null) return;
            if (root == null) return;

            for (int i = 0; i < MobileSkillSlotCount; i++)
            {
                int slotIndex = i;
                var slot = root.Q($"SkillSlot{i}") ?? root.Q(i == 0 ? "LeftSkillSlot" : i == 1 ? "RightSkillSlot" : string.Empty);
                _skillSlots[i] = slot;
                if (slot == null) continue;

                _skillIcons[i] = slot.Q("SlotIcon");
                _skillLabels[i] = slot.Q<Label>("SlotLabel");
                slot.pickingMode = PickingMode.Position;
                slot.RegisterCallback<PointerDownEvent>(evt => OnSkillSlotDown(slotIndex, evt));
                slot.RegisterCallback<PointerMoveEvent>(OnSlotMove);
                slot.RegisterCallback<PointerUpEvent>(OnSlotUp);
                slot.RegisterCallback<PointerCancelEvent>(OnSlotCancel);
            }

            _primaryAttackBtn = root.Q("PrimaryAttackBtn");
            if (_primaryAttackBtn != null)
            {
                _primaryAttackBtn.pickingMode = PickingMode.Position;
                _primaryAttackBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    BeginSlotPress(PrimaryAttackPseudoSlot, evt.pointerId, evt.position);
                    evt.StopPropagation();
                });
                _primaryAttackBtn.RegisterCallback<PointerMoveEvent>(OnSlotMove);
                _primaryAttackBtn.RegisterCallback<PointerUpEvent>(OnSlotUp);
                _primaryAttackBtn.RegisterCallback<PointerCancelEvent>(OnSlotCancel);
            }

            _deckSwitchBtn = root.Q("DeckSwitchBtn");
            _deckSwitchLabel = root.Q<Label>("DeckSwitchLabel");
            if (_deckSwitchBtn != null)
            {
                _deckSwitchBtn.pickingMode = PickingMode.Position;
                _deckSwitchBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    ToggleDeck();
                    evt.StopPropagation();
                });
            }

            _cancelCastZone = root.Q("CancelCastZone");
            _targetLockMarker = root.Q("TargetLockMarker");
            _targetLockName = root.Q<Label>("TargetLockName");
            UpdateTargetLockMarker();

            root.RegisterCallback<PointerDownEvent>(OnRootPointerDownCapture, TrickleDown.TrickleDown);

            _skillPickerOverlay = root.Q("SkillPickerOverlay");
            _skillPickerWindow = root.Q("SkillPickerWindow");
            _skillPickerClose = root.Q("SkillPickerClose");
            _skillPickerList = root.Q<ScrollView>("SkillPickerList");
            _skillPickerTitle = root.Q<Label>("SkillPickerTitle");

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
                    if (Vector2.Distance((Vector2)evt.position, _pickerPressPos) > PickerTapMoveThreshold)
                        return;
                    AssignSkillUnderPoint((Vector2)evt.position);
                });
                _skillPickerList.RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
            }

            _boundRoot = root;
            _initialized = true;
            RefreshSlotVisuals();
        }

        private void EnsureDeckArrays()
        {
            if (deckASkillIds == null || deckASkillIds.Length != MobileSkillSlotCount)
                Array.Resize(ref deckASkillIds, MobileSkillSlotCount);
            if (deckBSkillIds == null || deckBSkillIds.Length != MobileSkillSlotCount)
                Array.Resize(ref deckBSkillIds, MobileSkillSlotCount);
        }

        private int[] GetDeck(int deckIndex) => deckIndex == 1 ? deckBSkillIds : deckASkillIds;
        private int[] ActiveDeck => GetDeck(_activeDeckIndex);

        private bool IsDeckEmpty(int[] deck)
        {
            if (deck == null) return true;
            for (int i = 0; i < MobileSkillSlotCount; i++)
                if (deck[i] > 0) return false;
            return true;
        }

        private void ImportLegacySlotsIfNeeded()
        {
            if (IsDeckEmpty(deckASkillIds))
            {
                deckASkillIds[0] = leftSlotSkillId;
                deckASkillIds[1] = rightSlotSkillId;
            }
            else
            {
                leftSlotSkillId = deckASkillIds[0];
                rightSlotSkillId = deckASkillIds[1];
            }
        }

        // PC source skill order per faction. PC gốc JX: 1 ô là skill tấn công cơ bản
        // của phái, các ô còn lại là skill cao cấp / chiêu thức đặc trưng.
        // MobileSkillSlotCount = 5 → set đủ 5 skill theo thứ tự PC.
        // PC source: bin/client/script/skill/{gaibang,shaolin,...}.lua + skills.txt.
        // Fallback: lấy 5 skill active đầu tiên từ PcSkillPanelService.GetPcSkillOrder(faction).
        private static readonly int[][] LegacyCaiBangDefaultDecks =
        {
            new[] { 210, 357, 358, 1073, 130 },
            new[] { 357, 210, 358, 1073, 130 },
        };

        private static readonly System.Collections.Generic.Dictionary<CombatFaction, int[]> DefaultDeckByFaction =
            new System.Collections.Generic.Dictionary<CombatFaction, int[]>
            {
                // ponytail: PC quickbar is user-configurable; sandbox defaults to first five canonical
                // player damage skills instead of mixing utility, inert 358, buff 130, and tier-150 events.
                { CombatFaction.CaiBang, new[] { 117, 119, 122, 125, 128 } },
            };

        private void FillDefaultDeckIfEmpty()
        {
            if (!IsDeckEmpty(deckASkillIds)) return;

            var manager = SandboxManager.Instance;
            var prog = _progression ?? manager?.PlayerProgression;
            if (prog == null) return;

            int[] defaults;
            if (DefaultDeckByFaction.TryGetValue(prog.faction, out var perFaction))
            {
                // Per-faction PC-source order (Cái Bang) — cố định 5 skill.
                defaults = perFaction;
            }
            else
            {
                // Fallback: lấy 5 skill active đầu tiên từ PC order (skip passives, NPC variant, unknown).
                var catalog = _catalog ?? manager?.CombatSkillCatalog;
                var order = PcSkillPanelService.GetPcSkillOrder(prog.faction);
                var list = new System.Collections.Generic.List<int>();
                foreach (var skillId in order)
                {
                    if (list.Count >= MobileSkillSlotCount) break;
                    if (PcSkillPanelService.IsNpcVariant(skillId)) continue;
                    if (!prog.knownSkills.Contains(skillId) && prog.GetSkillLevel(skillId) <= 0) continue;
                    var skill = catalog?.Resolve(skillId);
                    if (skill != null && skill.skillStyle == PcSkillStyle.PassivityNpcState) continue;
                    list.Add(skillId);
                }
                defaults = list.ToArray();
            }

            for (int i = 0; i < MobileSkillSlotCount && i < defaults.Length; i++)
                deckASkillIds[i] = defaults[i];

            leftSlotSkillId = deckASkillIds[0];
            rightSlotSkillId = deckASkillIds[1];
        }

        // Normalize only empty or known generated Cái Bang decks; preserve user-customized slots.
        private void MigrateCaiBangDeckToDefaultIfNeeded()
        {
            var manager = SandboxManager.Instance;
            var prog = _progression ?? manager?.PlayerProgression;
            if (prog == null || prog.faction != CombatFaction.CaiBang) return;
            if (!DefaultDeckByFaction.TryGetValue(CombatFaction.CaiBang, out var defaults)) return;
            if (MatchesDeck(deckASkillIds, defaults)) return; // already the current default
            if (!IsEmptyDeck(deckASkillIds) && !MatchesAnyDeck(deckASkillIds, LegacyCaiBangDefaultDecks)) return;

            for (int i = 0; i < MobileSkillSlotCount; i++)
                deckASkillIds[i] = i < defaults.Length ? defaults[i] : 0;
            SyncLegacySlotFields();
        }

        private static bool IsEmptyDeck(int[] deck)
        {
            if (deck == null) return true;
            for (int i = 0; i < deck.Length; i++)
                if (deck[i] > 0) return false;
            return true;
        }

        private static bool MatchesDeck(int[] deck, int[] expected)
        {
            if (deck == null || expected == null || deck.Length != expected.Length) return false;
            for (int i = 0; i < expected.Length; i++)
                if (deck[i] != expected[i]) return false;
            return true;
        }

        private static bool MatchesAnyDeck(int[] deck, int[][] expectedDecks)
        {
            if (expectedDecks == null) return false;
            foreach (var expected in expectedDecks)
                if (MatchesDeck(deck, expected)) return true;
            return false;
        }

        // (Removed GetDefaultSkillsForFaction hardcode - now uses PC source order via PcSkillPanelService)

        public int GetAssignedSkill(int slot, int deckIndex = -1)
        {
            EnsureDeckArrays();
            if (slot < 0 || slot >= MobileSkillSlotCount) return 0;
            return GetDeck(deckIndex < 0 ? _activeDeckIndex : deckIndex)[slot];
        }

        /// <summary>Assign a skill to a specific slot on the active deck.</summary>
        /// <summary>
        /// Reset cả 2 deck A/B về 0 cho tất cả 4 slot, sau đó gán default skills
        /// cho deck A theo skillId array. Force _activeDeckIndex về 0 (deck A) để
        /// user thấy thay đổi ngay lập tức. Đây là hard-reset, dùng khi switch phái.
        /// </summary>
        public void ResetDeckToDefaults(int[] defaultSkillIds, bool forceActiveDeckA = true)
        {
            EnsureDeckArrays();
            // Force về deck A để user thấy đúng deck mà họ vừa gán
            if (forceActiveDeckA) _activeDeckIndex = 0;
            // Clear cả 2 deck hoàn toàn (zero out all slots)
            for (int i = 0; i < MobileSkillSlotCount; i++)
            {
                deckASkillIds[i] = 0;
                deckBSkillIds[i] = 0;
            }
            // Gán default skills cho deck A (đang active sau khi force)
            if (defaultSkillIds != null)
            {
                int count = Mathf.Min(defaultSkillIds.Length, MobileSkillSlotCount);
                for (int i = 0; i < count; i++)
                {
                    if (defaultSkillIds[i] > 0)
                        deckASkillIds[i] = defaultSkillIds[i];
                }
            }
            SyncLegacySlotFields();
            RefreshSlotVisuals();
            SubsystemLog.Info("Combat", $"ResetDeckToDefaults: forced deck A active, deckA=[{string.Join(",", deckASkillIds)}], deckB=[{string.Join(",", deckBSkillIds)}]");
        }

        public void AssignSkill(int slot, int skillId)
        {
            EnsureDeckArrays();
            if (slot < 0 || slot >= MobileSkillSlotCount) return;
            ActiveDeck[slot] = Mathf.Max(0, skillId);
            SyncLegacySlotFields();
            RefreshSlotVisuals();
            SubsystemLog.Info("Combat", $"Assigned skill {skillId} to deck {ActiveDeckName()} slot {slot}");
        }

        /// <summary>
        /// UI bridge used by the full skill-management panel. Keeps assignment validation
        /// in the hotbar owner while assigning to whichever deck is currently active.
        /// </summary>
        public bool TryAssignLearnedActiveSkill(int slot, int skillId)
        {
            if (slot < 0 || slot >= MobileSkillSlotCount) return false;

            var catalog = _catalog ?? SandboxManager.Instance?.CombatSkillCatalog;
            var progression = _progression ?? SandboxManager.Instance?.PlayerProgression;
            var skill = catalog?.Resolve(skillId);
            if (skill == null || skill.skillStyle == PcSkillStyle.PassivityNpcState) return false;
            if ((progression?.GetSkillLevel(skillId) ?? 0) <= 0) return false;

            AssignSkill(slot, skillId);
            return true;
        }

        public void ClearSlot(int slot) => AssignSkill(slot, 0);

        public void ToggleDeck()
        {
            _activeDeckIndex = _activeDeckIndex == 0 ? 1 : 0;
            SyncLegacySlotFields();
            RefreshSlotVisuals();
            SubsystemLog.Info("Combat", $"Switch combat deck {ActiveDeckName()}");
        }

        private string ActiveDeckName() => _activeDeckIndex == 0 ? "A" : "B";

        private void SyncLegacySlotFields()
        {
            leftSlotSkillId = deckASkillIds != null && deckASkillIds.Length > 0 ? deckASkillIds[0] : 0;
            rightSlotSkillId = deckASkillIds != null && deckASkillIds.Length > 1 ? deckASkillIds[1] : 0;
        }

        private void OnSkillSlotDown(int slot, PointerDownEvent evt)
        {
            BeginSlotPress(slot, evt.pointerId, evt.position);
            evt.StopPropagation();
        }

        private void OnRootPointerDownCapture(PointerDownEvent evt)
        {
            var pos = (Vector2)evt.position;
            for (int i = 0; i < MobileSkillSlotCount; i++)
            {
                if (_skillSlots[i] == null || !_skillSlots[i].worldBound.Contains(pos)) continue;
                BeginSlotPress(i, evt.pointerId, evt.position);
                evt.StopImmediatePropagation();
                return;
            }

            if (_primaryAttackBtn != null && _primaryAttackBtn.worldBound.Contains(pos))
            {
                BeginSlotPress(PrimaryAttackPseudoSlot, evt.pointerId, evt.position);
                evt.StopImmediatePropagation();
            }
        }

        private void BeginSlotPress(int slot, int pointerId, Vector2 screenPos)
        {
            if (slot != PrimaryAttackPseudoSlot && (slot < 0 || slot >= MobileSkillSlotCount)) return;
            SubsystemLog.Info("CombatTouch", $"BeginSlotPress: slot={slot}, pointerId={pointerId}, pos={screenPos}");
            _pressedSlot = slot;
            _pressedPointerId = pointerId;
            _startPointerPos = screenPos;
            _slotPointerDown = true;
            _aimingDrag = false;
            HideCancelCastZone();

            if (slot == PrimaryAttackPseudoSlot)
                _primaryAttackBtn?.CapturePointer(pointerId);
            else
                _skillSlots[slot]?.CapturePointer(pointerId);
        }

        private void OnSlotMove(PointerMoveEvent evt)
        {
            if (_slotPointerDown && _pressedSlot != -1)
            {
                if (_pressedSlot == PrimaryAttackPseudoSlot)
                {
                    evt.StopPropagation();
                    return;
                }

                float distance = Vector2.Distance((Vector2)evt.position, _startPointerPos);
                int skillId = GetAssignedSkill(_pressedSlot);
                if (distance > SlotDragCancelThreshold && skillId > 0)
                {
                    SubsystemLog.Info("CombatTouch", $"OnSlotMove: aiming drag started. dist={distance}, slot={_pressedSlot}");
                    _aimingDrag = true;
                    ShowCancelCastZone();
                }
            }
            evt.StopPropagation();
        }

        private void OnSlotUp(PointerUpEvent evt)
        {
            int slot = _pressedSlot;
            SubsystemLog.Info("CombatTouch", $"OnSlotUp: slot={slot}, pointerId={evt.pointerId}, _aimingDrag={_aimingDrag}");
            ReleasePressedSlotCapture(evt.pointerId);
            _slotPointerDown = false;
            _pressedSlot = -1;
            _pressedPointerId = -1;

            if (_aimingDrag && slot != -1)
            {
                bool cancelled = IsInCancelCastZone((Vector2)evt.position);
                if (!cancelled)
                {
                    if (slot == PrimaryAttackPseudoSlot)
                        TriggerPrimaryAttack();
                    else
                        TriggerSkillSlot(slot, GetAssignedSkill(slot));
                }
                else
                {
                    SubsystemLog.Info("Combat", $"Cancel aim deck {ActiveDeckName()} slot {(slot == PrimaryAttackPseudoSlot ? "Primary" : slot.ToString())}");
                }
            }
            else if (slot != -1)
            {
                if (slot == PrimaryAttackPseudoSlot)
                {
                    TriggerPrimaryAttack();
                }
                else
                {
                    int skillId = GetAssignedSkill(slot);
                    if (skillId > 0) TriggerSkillSlot(slot, skillId);
                    else OpenSkillPicker(slot);
                }
            }

            _aimingDrag = false;
            HideCancelCastZone();
            evt.StopPropagation();
        }

        private void OnSlotCancel(PointerCancelEvent evt)
        {
            SubsystemLog.Info("CombatTouch", $"OnSlotCancel: _pressedSlot={_pressedSlot}, _aimingDrag={_aimingDrag}");
            // Mobile devices can fire PointerCancel during capture changes. Keep the
            // press alive for long-press unless we were actually aiming; then cancel aim.
            if (_aimingDrag)
            {
                ReleasePressedSlotCapture(_pressedPointerId);
                ResetPressState();
            }
            evt.StopPropagation();
        }

        private void ReleasePressedSlotCapture(int pointerId)
        {
            int slot = _pressedSlot;
            if (slot == PrimaryAttackPseudoSlot)
                _primaryAttackBtn?.ReleasePointer(pointerId);
            else if (slot >= 0 && slot < MobileSkillSlotCount)
                _skillSlots[slot]?.ReleasePointer(pointerId);
        }

        private void ResetPressState()
        {
            SubsystemLog.Info("CombatTouch", "ResetPressState called");
            _slotPointerDown = false;
            _pressedSlot = -1;
            _pressedPointerId = -1;
            _aimingDrag = false;
            HideCancelCastZone();
        }

        private void ShowCancelCastZone()
        {
            _cancelCastZone?.RemoveFromClassList("hidden");
        }

        private void HideCancelCastZone()
        {
            _cancelCastZone?.AddToClassList("hidden");
        }

        private bool IsInCancelCastZone(Vector2 panelPoint)
            => _cancelCastZone != null
               && !_cancelCastZone.ClassListContains("hidden")
               && _cancelCastZone.worldBound.Contains(panelPoint);

        public void OpenSkillPicker(int slot)
        {
            _activeSlot = Mathf.Clamp(slot, 0, MobileSkillSlotCount - 1);
            BindElements();

            _skillPickerOverlay?.RemoveFromClassList("hidden");
            if (_skillPickerTitle != null)
                _skillPickerTitle.text = $"Chọn kỹ năng ({ActiveDeckName()}-{_activeSlot + 1})";

            PopulateSkillPicker();
            SubsystemLog.Info("Combat", $"Skill picker opened for deck {ActiveDeckName()} slot {_activeSlot}");
        }

        public void CloseSkillPicker()
        {
            _activeSlot = -1;
            _skillPickerOverlay?.AddToClassList("hidden");
        }

        public void TriggerPrimaryAttack()
        {
            int slot = ResolvePrimaryAttackSlot();
            if (slot >= 0)
            {
                TriggerSkillSlot(slot, GetAssignedSkill(slot));
                return;
            }

            if (TryLockNearestTarget())
                SubsystemLog.Info("Combat", $"Primary attack locked target {_lockedTargetName}; assign a skill to cast.");
            else
                OpenSkillPicker(0);
        }

        public int ResolvePrimaryAttackSlot()
        {
            if (GetAssignedSkill(0) > 0) return 0;
            for (int i = 1; i < MobileSkillSlotCount; i++)
                if (GetAssignedSkill(i) > 0) return i;
            return -1;
        }

        public bool TryLockNearestTarget()
        {
            var player = SandboxManager.Instance?.PlayerController;
            if (player == null) return false;
            var enemies = CollectEnemies();
            EnemyRuntimeInfo best = null;
            float bestDist = float.MaxValue;
            Vector2 casterPos = player.transform.position;
            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.alive) continue;
                float dist = Vector2.Distance(casterPos, enemy.position);
                if (dist >= bestDist) continue;
                best = enemy;
                bestDist = dist;
            }

            if (best == null)
            {
                ClearTargetLock();
                return false;
            }

            LockTarget(best.enemyId, string.IsNullOrWhiteSpace(best.displayName) ? $"Mục tiêu {best.enemyId}" : best.displayName);
            return true;
        }

        public void LockTarget(int enemyId, string displayName)
        {
            _lockedTargetId = enemyId;
            _lockedTargetName = displayName ?? string.Empty;
            UpdateTargetLockMarker();
            SubsystemLog.Info("Combat", $"Lock target {_lockedTargetName} ({_lockedTargetId})");
        }

        public void ClearTargetLock()
        {
            _lockedTargetId = -1;
            _lockedTargetName = string.Empty;
            UpdateTargetLockMarker();
        }

        private void UpdateTargetLockMarker()
        {
            if (_targetLockMarker == null) return;
            if (_lockedTargetId >= 0)
            {
                _targetLockMarker.RemoveFromClassList("hidden");
                if (_targetLockName != null)
                    _targetLockName.text = string.IsNullOrWhiteSpace(_lockedTargetName) ? "Đã khóa" : _lockedTargetName;
            }
            else
            {
                _targetLockMarker.AddToClassList("hidden");
                if (_targetLockName != null) _targetLockName.text = string.Empty;
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

            foreach (var skillId in factionSkillOrder)
            {
                if (PcSkillPanelService.IsNpcVariant(skillId)) continue;
                var skill = catalog.Resolve(skillId);
                if (skill == null) continue;
                if (skill.skillStyle == PcSkillStyle.PassivityNpcState) continue;

                int learnedLevel = progression?.GetSkillLevel(skill.skillId) ?? 0;
                if (learnedLevel <= 0) continue;

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
                var levelLabel = new Label($"Cấp {learnedLevel}");
                levelLabel.AddToClassList("skill-picker-level");
                var typeLabel = new Label(skill.skillStyle == PcSkillStyle.PassivityNpcState ? "Bị động" : "Chủ động");
                typeLabel.AddToClassList("skill-picker-type");
                info.Add(nameLabel);
                info.Add(levelLabel);
                info.Add(typeLabel);
                item.Add(info);

                int capturedId = skillId;
                item.userData = capturedId;
                item.RegisterCallback<ClickEvent>(evt =>
                {
                    AssignSkill(_activeSlot, capturedId);
                    CloseSkillPicker();
                    evt.StopPropagation();
                });

                _skillPickerList.Add(item);
            }
        }

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
            for (int i = 0; i < MobileSkillSlotCount; i++)
            {
                int skillId = GetAssignedSkill(i);
                var icon = _skillIcons[i];
                if (icon != null)
                {
                    if (skillId > 0)
                    {
                        GameHudController.LoadIconStatic(this, icon, artPath, $"cai_bang_skill_{skillId}");
                        icon.RemoveFromClassList("empty");
                        icon.style.display = DisplayStyle.Flex;
                    }
                    else
                    {
                        icon.style.backgroundImage = new StyleBackground();
                        icon.AddToClassList("empty");
                        icon.style.display = DisplayStyle.Flex;
                    }
                }
                UpdateSlotLabel(_skillLabels[i], i, skillId);
            }

            if (_deckSwitchLabel != null)
                _deckSwitchLabel.text = ActiveDeckName();
        }

        private void UpdateSlotLabel(Label label, int slot, int skillId)
        {
            if (label == null) return;
            if (skillId <= 0)
            {
                label.text = (slot + 1).ToString();
                return;
            }
            var catalog = _catalog ?? SandboxManager.Instance?.CombatSkillCatalog;
            var skill = catalog?.Resolve(skillId);
            label.text = skill == null ? skillId.ToString() : ShortenSkillName(skill.DisplayName);
        }

        private static string ShortenSkillName(string displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return string.Empty;
            var words = displayName.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 1) return words[0].Length <= 3 ? words[0] : words[0].Substring(0, 3);
            return (words[0][0].ToString() + words[words.Length - 1][0]).ToUpperInvariant();
        }

        /// <summary>Trigger a skill slot — prefer locked target, else auto-target nearest enemy.</summary>
        public void TriggerSkillSlot(int slot, int skillId)
        {
            if (skillId <= 0) return;

            var manager = SandboxManager.Instance;
            if (manager == null) return;

            var catalog = manager.CombatSkillCatalog;
            var skill = catalog?.Resolve(skillId);
            if (skill == null) return;

            var player = manager.PlayerController;
            if (player == null) return;

            Vector2 casterPos = player.transform.position;

            var enemies = CollectEnemies();
            int skillLevel = manager.PlayerProgression?.skillLevels.TryGetValue(skillId, out var lv) == true ? lv : skill.maxLevel;
            var plan = skill.targetEnemy ? ResolveMobileSkillTapPlan(casterPos, skill, enemies, skillLevel) : MobileSkillTapTargetPlan.NoTarget();
            var target = plan.canCastNow ? plan.target : null;

            if (plan.shouldApproach && plan.target != null)
            {
                int facing = CombatAutoTargetService.ComputeFacing8Way(casterPos, plan.target.position);
                player.SetFacing(facing);
                player.MoveTo(plan.approachPosition);
                LockTarget(plan.target.enemyId, plan.target.name);
                SubsystemLog.Info("Combat", $"Move into range for {skill.DisplayName} → {plan.target.name} " +
                                             $"(distance={plan.target.distance:F0}, range={plan.maxRange:F0})");
                return;
            }

            if (target != null || (skill.targetSelf && !skill.targetEnemy))
            {
                player.PlayPcSkillAction(skill.charAnimId, PcCastAnimationDurationSeconds(skill));

                if (target != null)
                {
                    int facing = CombatAutoTargetService.ComputeFacing8Way(casterPos, target.position);
                    player.SetFacing(facing);
                }

                var combatRuntime = manager.CombatRuntime;
                if (combatRuntime != null)
                {
                    var caster = CreateCombatActor(player, skill);
                    var targetActor = target != null ? CreateTargetActor(target) : null;
                    var targetPos = target != null ? target.position : casterPos;
                    var report = combatRuntime.Cast(caster, targetActor, skillId, targetPos, target != null ? CombatRelation.Enemy : CombatRelation.Self);

                    if (report.success)
                    {
                        var persistentPlayer = manager.GameplayLoop?.Player;
                        if (persistentPlayer != null && persistentPlayer.combat != null)
                        {
                            persistentPlayer.combat.currentLife = caster.currentLife;
                            persistentPlayer.combat.currentMana = caster.currentMana;
                            persistentPlayer.combat.states.Clear();
                            foreach (var kvp in caster.states)
                            {
                                persistentPlayer.combat.states[kvp.Key] = kvp.Value;
                            }
                        }

                        if (target != null && targetActor != null)
                        {
                            var persistentTarget = manager.GameplayLoop?.GetActor(target.enemyId);
                            if (persistentTarget != null && persistentTarget.combat != null)
                            {
                                persistentTarget.combat.currentLife = targetActor.currentLife;
                                persistentTarget.combat.currentMana = targetActor.currentMana;
                                persistentTarget.combat.states.Clear();
                                foreach (var kvp in targetActor.states)
                                {
                                    persistentTarget.combat.states[kvp.Key] = kvp.Value;
                                }
                            }
                        }

                        // [SECT-ALL] PC source-based dash (no teleport).
                        // PC source: KNpc::DoRunAttack (0x0809b9c0) close-range lunge, state 0x12.
                        //            KNpc::NewJump (0x08099fd0) long-range, TestMovePos + m_1834 distance.
                        // Mobile port: dùng SandboxPlayerController.BeginDash() lerp từ vị trí hiện tại
                        //              tới caster.position trong skill.dashDurationSeconds.
                        // [SECT-ALL] TODO(PC-runtime): dashDurationSeconds là 0 cho tất cả skill hiện tại
                        //   (PC source chỉ set state + distance, duration thuộc client engine animation).
                        //   Khi duration > 0 → dash mượt. Khi duration <= 0 → skip dash (no source → no fake).
                        //
                        // [SECT-ALL] PC 轻功 (Khinh Công / JumpFly, skill 210): a self-cast leap skill.
                        //   Instead of the no-op dash, leap forward (KNpc::NewJump) while playing the
                        //   Jump (JP01) animation. Distance/duration are PC client-engine values not in
                        //   the server source; use faithful observed defaults (documented in BeginLeap).
                        if (skill.isLeapSkill)
                        {
                            const float KhinhCongLeapDistance = 240f;  // PC NewJump observed leap range (~3-4 tiles).
                            const float KhinhCongLeapDuration = 0.45f; // PC jump animation cycle.
                            player.BeginLeap(player.GetLeapTarget(KhinhCongLeapDistance), KhinhCongLeapDuration);
                        }
                        else if (skill.dashDurationSeconds > 0f)
                            player.BeginDash(caster.position, skill.dashDurationSeconds);
                        else
                            SubsystemLog.Warn("Combat", $"Cast {skill.DisplayName} (id={skillId}): dashDurationSeconds=0 — PC source does not provide duration, dash SKIPPED (TODO: PC runtime observation needed).");

                        var effectService = manager.SkillEffectVisual;
                        BaLangEnemyAi liveTarget = target != null ? target.enemyBehaviour : null;
                        System.Func<Vector2> currentTargetPos = liveTarget != null
                            ? (System.Func<Vector2>)(() => liveTarget != null
                                ? (Vector2)liveTarget.transform.position
                                : targetPos)
                            : null;
                        var collisionMissiles = skill.byMissile && report.projectiles != null
                            ? report.projectiles.FindAll(projectile => projectile.skillId == skill.childSkillId)
                            : null;
                        System.Action<ActiveSkillEffect, int, Vector2> onMissileCollided = null;
                        if (collisionMissiles != null && collisionMissiles.Count > 0)
                        {
                            onMissileCollided = (_, missileIndex, collisionPoint) =>
                            {
                                if (missileIndex < 0 || missileIndex >= collisionMissiles.Count || targetActor == null)
                                    return;

                                int damageCountBefore = report.damageResults?.Count ?? 0;
                                int projectileCountBefore = report.projectiles?.Count ?? 0;
                                int targetLifeBefore = targetActor.currentLife;
                                if (!combatRuntime.TryResolveProjectileCollision(
                                        caster, targetActor, report, collisionMissiles[missileIndex], collisionPoint))
                                    return;

                                ApplyProjectileCollisionResult(
                                    manager, effectService, catalog, combatRuntime, caster, target, targetActor,
                                    report, collisionPoint, damageCountBefore, projectileCountBefore, targetLifeBefore);
                            };
                        }

                        var fx = effectService?.PlaySkillCast(
                            skill, casterPos, targetPos, report.skillLevel, currentTargetPos, onMissileCollided);
                        if (!skill.byMissile && target != null && target.enemyBehaviour != null && targetActor != null)
                            StartCoroutine(ApplyLiveEnemyHpAtImpact(target, targetActor.currentLife, skillId, report.skillLevel, report, fx));

                        string targetName = target != null ? target.name : "Self";
                        float targetDist = target != null ? target.distance : 0f;
                        int targetHp = targetActor != null ? targetActor.currentLife : 0;
                        SubsystemLog.Info("Combat", $"Cast {skill.DisplayName} [{ActiveDeckName()}-{slot + 1}] → {targetName} " +
                                                     $"(dmg={report.damageResults.Count}, pendingHp={targetHp}, range={targetDist:F0})");
                    }
                    else
                    {
                        SubsystemLog.Warn("Combat", $"Cast {skill.DisplayName} FAILED: {report.reason} — {report.detail}");
                    }
                }
            }
            else
            {
                player.PlayPcSkillAction(skill.charAnimId, PcCastAnimationDurationSeconds(skill));
                var effectService = manager.SkillEffectVisual;
                // No target: shoot forward in player's facing direction (PC: KNpc fires toward facing dir)
                int facing = player.visual != null ? player.visual.GetCurrentDirection() : 0;
                Vector2 facingDir = PcDirection8Way.ToVector2(facing);
                float forwardDistance = skill.attackRadius > 0 ? skill.attackRadius : 150f;
                Vector2 forwardTarget = casterPos + facingDir * forwardDistance;
                effectService?.PlaySkillCast(skill, casterPos, forwardTarget, skillLevel);
                SubsystemLog.Info("Combat", $"Cast {skill.DisplayName} — no enemy in range");
            }
        }

        private MobileSkillTapTargetPlan ResolveMobileSkillTapPlan(
            Vector2 casterPos,
            SkillDefinition skill,
            IReadOnlyList<EnemyRuntimeInfo> enemies,
            int skillLevel)
        {
            var locked = FindLockedTarget(casterPos, skill, enemies, skillLevel);
            if (locked != null)
                return MobileSkillTapTargetPlan.Cast(locked, ResolveWorldRange(skill, skillLevel));

            var targetService = new CombatAutoTargetService();
            var plan = targetService.ResolveSkillTapTarget(casterPos, skill, enemies, skillLevel);
            if (plan.hasTarget && plan.target != null)
                LockTarget(plan.target.enemyId, plan.target.name);
            return plan;
        }

        private static float ResolveWorldRange(SkillDefinition skill, int skillLevel)
        {
            int attackRadius;
            if (PcKangLongYouHuiTuning.Applies(skill.skillId) && skillLevel > 0)
                attackRadius = PcKangLongYouHuiTuning.AtLevel(skillLevel).attackRadius;
            else if (PcCaiBangLuaLevelService.Applies(skill.skillId) && skillLevel > 0)
            {
                int luaRadius = PcCaiBangLuaLevelService.GetAttackRadius(skill.skillId, skillLevel);
                attackRadius = luaRadius > 0 ? luaRadius : skill.attackRadius;
            }
            else
                attackRadius = skill.attackRadius;
            return attackRadius > 0 ? attackRadius : 500f;
        }

        private CombatTargetInfo FindLockedTarget(
            Vector2 casterPos,
            SkillDefinition skill,
            IReadOnlyList<EnemyRuntimeInfo> enemies,
            int skillLevel)
        {
            if (_lockedTargetId < 0 || enemies == null || skill == null) return null;
            int attackRadius;
            if (PcKangLongYouHuiTuning.Applies(skill.skillId) && skillLevel > 0)
                attackRadius = PcKangLongYouHuiTuning.AtLevel(skillLevel).attackRadius;
            else if (PcCaiBangLuaLevelService.Applies(skill.skillId) && skillLevel > 0)
            {
                int luaRadius = PcCaiBangLuaLevelService.GetAttackRadius(skill.skillId, skillLevel);
                attackRadius = luaRadius > 0 ? luaRadius : skill.attackRadius;
            }
            else
                attackRadius = skill.attackRadius;
            float maxRange = attackRadius > 0 ? attackRadius : 500f;

            for (int i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy == null || enemy.enemyId != _lockedTargetId || !enemy.alive) continue;
                float dist = Vector2.Distance(casterPos, enemy.position);
                if (dist > maxRange) break;
                return new CombatTargetInfo
                {
                    enemyIndex = i,
                    position = enemy.position,
                    distance = dist,
                    enemyId = enemy.enemyId,
                    name = enemy.displayName,
                    currentLife = enemy.currentLife,
                    maxLife = enemy.maxLife,
                    enemyBehaviour = enemy.enemyBehaviour,
                };
            }

            ClearTargetLock();
            return null;
        }

        private static float PcCastAnimationDurationSeconds(SkillDefinition skill)
        {
            if (skill == null) return 0f;
            if (skill.charAnimId == 14) return 0f; // cdo_none stance/passive (no cast anim)
            // PC parity [2026-06-19]: PC WaitTime (Skills.txt col 25) = ticks, 16 ticks/sec.
            // Skill-specific WaitTime drives cast anim duration. Fallback 20f/16f if WaitTime=0
            // (default m_CastFrame=20 ticks cho damage skills không khai báo WaitTime).
            if (skill.waitTime > 0) return skill.waitTime / 16f;
            return 20f / 16f;
        }

        private static void ApplyProjectileCollisionResult(
            SandboxManager manager,
            SkillEffectVisualService effectService,
            SkillCatalog catalog,
            CombatRuntimeService combatRuntime,
            CombatActorState caster,
            CombatTargetInfo target,
            CombatActorState targetActor,
            CombatCastReport report,
            Vector2 collisionPoint,
            int damageCountBefore,
            int projectileCountBefore,
            int targetLifeBefore)
        {
            if (targetActor == null || report == null)
                return;

            if (target != null)
                target.currentLife = targetActor.currentLife;
            var liveTarget = target?.enemyBehaviour;
            if (liveTarget != null)
            {
                liveTarget.SetLife(targetActor.currentLife, showDamage: true);
                EmitProjectileCollisionFeedback(liveTarget, report.damageResults, damageCountBefore);
            }

            int visualDamage = Mathf.Max(0, targetLifeBefore - targetActor.currentLife);
            var gameplayLoop = manager?.GameplayLoop;
            var glEnemy = target != null ? gameplayLoop?.GetActor(10000 + target.enemyId) : null;
            if (visualDamage > 0 && glEnemy?.combat != null && !glEnemy.isDead)
            {
                int glDamage = targetActor.maxLife > 0
                    ? Mathf.RoundToInt((float)visualDamage / targetActor.maxLife * glEnemy.combat.maxLife)
                    : visualDamage;
                glEnemy.combat.currentLife = Mathf.Max(0, glEnemy.combat.currentLife - glDamage);
                if (glEnemy.combat.currentLife <= 0)
                    gameplayLoop.ProcessActorDeathPublic(glEnemy, gameplayLoop.Player);
            }

            if (report.projectiles == null || report.projectiles.Count <= projectileCountBefore)
                return;

            var nestedMissiles = report.projectiles.GetRange(
                projectileCountBefore, report.projectiles.Count - projectileCountBefore);
            if (nestedMissiles.Count == 0 ||
                !report.projectileImpactSkillIds.TryGetValue(nestedMissiles[0].instanceId, out int nestedSkillId))
                return;

            var nestedVisual = catalog?.Resolve(nestedSkillId);
            if (nestedVisual == null) return;
            int nestedLevel = report.projectileImpactSkillLevels.TryGetValue(nestedMissiles[0].instanceId, out int level)
                ? level
                : report.skillLevel;

            effectService?.PlaySkillCast(
                nestedVisual, collisionPoint, collisionPoint, nestedLevel, null,
                (_, missileIndex, nestedCollisionPoint) =>
                {
                    if (missileIndex < 0 || missileIndex >= nestedMissiles.Count || combatRuntime == null || caster == null)
                        return;
                    int nestedDamageBefore = report.damageResults?.Count ?? 0;
                    int nestedProjectileBefore = report.projectiles.Count;
                    int nestedLifeBefore = targetActor.currentLife;
                    if (!combatRuntime.TryResolveProjectileCollision(
                            caster, targetActor, report, nestedMissiles[missileIndex], nestedCollisionPoint))
                        return;
                    ApplyProjectileCollisionResult(
                        manager, effectService, catalog, combatRuntime, caster, target, targetActor, report,
                        nestedCollisionPoint, nestedDamageBefore, nestedProjectileBefore, nestedLifeBefore);
                });
        }

        private static void EmitProjectileCollisionFeedback(
            BaLangEnemyAi liveTarget,
            List<DamageResult> damageResults,
            int damageCountBefore)
        {
            if (liveTarget == null || damageResults == null || damageCountBefore < 0 || damageCountBefore >= damageResults.Count)
                return;

            int totalDamage = 0;
            bool anyHit = false;
            bool anyCrit = false;
            for (int i = damageCountBefore; i < damageResults.Count; i++)
            {
                var result = damageResults[i];
                totalDamage += result.finalDamage;
                anyHit |= result.hit;
                anyCrit |= result.isCrit;
            }

            CombatFeedbackKind kind = !anyHit ? CombatFeedbackKind.Miss
                                   : anyCrit ? CombatFeedbackKind.Crit
                                   : CombatFeedbackKind.Normal;
            Vector3 worldPos = liveTarget.transform.position + Vector3.up * 2f;
            if (kind == CombatFeedbackKind.Miss)
                CombatFeedbackBus.Raise(new CombatFeedbackEvent(kind, 0, worldPos));
            else if (totalDamage > 0)
                CombatFeedbackBus.Raise(new CombatFeedbackEvent(kind, totalDamage, worldPos));
        }

        private IEnumerator ApplyLiveEnemyHpAtImpact(CombatTargetInfo target, int hp, int skillId, int skillLevel, CombatCastReport report, ActiveSkillEffect fx)
        {
            if (target?.enemyBehaviour == null) yield break;
            if (fx == null)
            {
                target.enemyBehaviour.SetLife(hp, showDamage: true);
                yield break;
            }

            while (fx.phase != SkillEffectPhase.Impact && fx.phase != SkillEffectPhase.Finished)
                yield return null;

            if (target.enemyBehaviour != null)
            {
                target.enemyBehaviour.SetLife(hp, showDamage: true);

                // [DMG-POPUP] Publish CombatFeedbackEvent để spawn số damage ĐỎ tại mục tiêu.
                // PC: số damage client-side render khi NPC bị hit (KNpc::DoHurt KNpc.cpp:1427).
                // Unity: CombatFeedbackView subscribe CombatFeedbackBus.OnFeedback → spawn TextMesh tại evt.Position.
                if (report.damageResults != null && report.damageResults.Count > 0)
                {
                    int totalDamage = 0;
                    bool anyHit = false;
                    bool anyCrit = false;
                    foreach (var r in report.damageResults)
                    {
                        totalDamage += r.finalDamage;
                        if (r.hit) anyHit = true;
                        if (r.isCrit) anyCrit = true;
                    }

                    // Determine kind: Miss nếu tất cả miss, Crit nếu có crit, Normal thường
                    CombatFeedbackKind kind = !anyHit ? CombatFeedbackKind.Miss
                                           : anyCrit ? CombatFeedbackKind.Crit
                                           : CombatFeedbackKind.Normal;

                    // Position: vị trí world của target (để spawn số tại mục tiêu)
                    Vector3 worldPos = target.enemyBehaviour.transform.position;
                    // Offset Y lên chút để số nổi lên trên đầu NPC (PC damage number position)
                    worldPos += Vector3.up * 2f;

                    // Publish event — CombatFeedbackView/HitEffectSpawner sẽ nhận và spawn visual
                    if (kind == CombatFeedbackKind.Miss)
                        CombatFeedbackBus.Raise(new CombatFeedbackEvent(kind, 0, worldPos)); // Miss = 0 value
                    else if (totalDamage > 0)
                        CombatFeedbackBus.Raise(new CombatFeedbackEvent(kind, totalDamage, worldPos));
                }

                // Bridge damage into GameplayLoop so EXP/silver/respawn fire correctly.
                // Mapping: GameplayActor.actorId = 10000 + BaLangNpcEntry.instanceId (enemyId).
                // hp = remaining HP in visual scale; compute damage dealt and apply to GL actor.
                var glEnemy = SandboxManager.Instance?.GameplayLoop?.GetActor(10000 + target.enemyId);
                if (glEnemy != null && !glEnemy.isDead)
                {
                    // Visual damage = (maxLife - hp) as fraction of maxLife, applied to GL maxLife
                    int visualDmg = target.maxLife > 0 ? target.currentLife - hp : 0; // hp is new value
                    if (visualDmg > 0)
                    {
                        int glDmg = target.maxLife > 0
                            ? Mathf.RoundToInt((float)visualDmg / target.maxLife * glEnemy.combat.maxLife)
                            : visualDmg;
                        glEnemy.combat.currentLife = Mathf.Max(0, glEnemy.combat.currentLife - glDmg);
                        if (glEnemy.combat.currentLife <= 0)
                            SandboxManager.Instance?.GameplayLoop?.ProcessActorDeathPublic(glEnemy, SandboxManager.Instance?.GameplayLoop?.Player);
                    }
                }

            }
        }

        private List<EnemyRuntimeInfo> CollectEnemies()
        {
            var enemies = new List<EnemyRuntimeInfo>();
            var runtime = SandboxManager.Instance?.EnemyRuntime;
            if (runtime != null)
            {
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
            }

            var trainingSpawns = SandboxManager.Instance?.TrainingSpawner?.GetActiveEnemies();
            if (trainingSpawns != null)
                enemies.AddRange(trainingSpawns);

            return enemies;
        }

        private CombatActorState CreateCombatActor(SandboxPlayerController player, SkillDefinition skill)
        {
            var manager = SandboxManager.Instance;
            var progression = _progression ?? manager?.PlayerProgression ?? new PlayerProgressionState();

            if (manager != null)
                manager.GrantFactionSkillPanelProgression(progression.faction);

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

            var persistentPlayer = manager?.GameplayLoop?.Player;
            if (persistentPlayer != null && persistentPlayer.combat != null)
            {
                actor.currentLife = persistentPlayer.combat.currentLife;
                actor.maxLife = persistentPlayer.combat.maxLife;
                actor.currentMana = persistentPlayer.combat.currentMana;
                actor.maxMana = persistentPlayer.combat.maxMana;
                if (persistentPlayer.combat.states != null)
                {
                    foreach (var kvp in persistentPlayer.combat.states)
                    {
                        actor.states[kvp.Key] = new SkillMagicAttribute(kvp.Value.kind, kvp.Value.value1, kvp.Value.value2, kvp.Value.value3);
                    }
                }
            }

            foreach (var id in progression.knownSkills)
                actor.knownSkills.Add(id);
            foreach (var kv in progression.skillLevels)
                actor.skillLevels[kv.Key] = kv.Value > 0 ? kv.Value : 1;

            if (skill != null && (!actor.skillLevels.ContainsKey(skill.skillId) || actor.skillLevels[skill.skillId] <= 0))
                actor.skillLevels[skill.skillId] = 1;

            return actor;
        }

        private CombatActorState CreateTargetActor(CombatTargetInfo target)
        {
            var actor = new CombatActorState
            {
                actorId = target.enemyId + 1000,
                faction = CombatFaction.None,
                position = target.position,
                currentLife = target.currentLife,
                maxLife = target.maxLife,
            };

            var manager = SandboxManager.Instance;
            if (manager != null && manager.GameplayLoop != null)
            {
                var persistentTarget = manager.GameplayLoop.GetActor(target.enemyId);
                if (persistentTarget != null && persistentTarget.combat != null)
                {
                    actor.currentLife = persistentTarget.combat.currentLife;
                    actor.maxLife = persistentTarget.combat.maxLife;
                    actor.currentMana = persistentTarget.combat.currentMana;
                    actor.maxMana = persistentTarget.combat.maxMana;
                    if (persistentTarget.combat.states != null)
                    {
                        foreach (var kvp in persistentTarget.combat.states)
                        {
                            actor.states[kvp.Key] = new SkillMagicAttribute(kvp.Value.kind, kvp.Value.value1, kvp.Value.value2, kvp.Value.value3);
                        }
                    }
                }
            }

            return actor;
        }
    }

    /// <summary>
    /// PC 8-way direction to Unity Vector2 converter.
    /// PC direction: 0=S, 1=SW, 2=W, 3=NW, 4=N, 5=NE, 6=E, 7=SE.
    /// Used to compute forward target when no enemy is locked.
    /// </summary>
    public static class PcDirection8Way
    {
        private static readonly Vector2[] Directions = new Vector2[]
        {
            new Vector2(0, -1),   // 0 = S (down)
            new Vector2(-1, -1),  // 1 = SW
            new Vector2(-1, 0),   // 2 = W
            new Vector2(-1, 1),   // 3 = NW
            new Vector2(0, 1),    // 4 = N (up)
            new Vector2(1, 1),    // 5 = NE
            new Vector2(1, 0),    // 6 = E (right)
            new Vector2(1, -1),   // 7 = SE
        };

        /// <summary>Convert PC 8-way direction index to normalized Vector2.</summary>
        public static Vector2 ToVector2(int pcDirection)
        {
            if (pcDirection < 0 || pcDirection >= Directions.Length) pcDirection = 0;
            return Directions[pcDirection].normalized;
        }
    }

}
