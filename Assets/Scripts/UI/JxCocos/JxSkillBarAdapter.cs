// -----------------------------------------------------------------------------
// VLTK Mobile — JX skill bar rendering adapter (UI Toolkit, port KgameWorldVN.cpp)
//
// Nguồn: setattackSprInfo (main/left) + auxiliarySkillData[8] (aux/right).
// Render: 1 main skill slot (circle mask mainskillmix) + 8 auxiliary slots.
// Mỗi slot = icon SPR + cooldown overlay (timeLoopLayer scaleY) + cd label.
// Click slot → cast nếu skill gắn &amp; không cooldown (controller nhận command).
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Click dùng coordinator public
// (UI Toolkit SendEvent cần live panel).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>Adapter UI Toolkit cho JX skill bar (9 combat slots).</summary>
    public sealed class JxSkillBarAdapter
    {
        private readonly VisualElement _root;
        private readonly JxSkillSlotState _state;
        private readonly IJxHudCommandBus _bus;
        private long _nowMs;

        public static class Names
        {
            public const string Bar = "jx_skillbar";
            public const string MainSlot = "jx_skill_main";
            public const string AuxSlotPrefix = "jx_skill_aux_"; // + slotIndex
            public const string Icon = "jx_skill_icon";
            public const string Cooldown = "jx_skill_cooldown";
            public const string CdLabel = "jx_skill_cdlabel";
        }

        public const string EmptyClass = "jx-skill-empty";
        public const string ReadyClass = "jx-skill-ready";
        public const string CooldownClass = "jx-skill-oncooldown";

        public JxSkillBarAdapter(VisualElement root, JxSkillSlotState state, IJxHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Đặt thời gian hiện tại (ms) cho tính cooldown render.</summary>
        public void SetNow(long nowMs) => _nowMs = nowMs;

        /// <summary>Bind bar element. Trả về false nếu thiếu bar.</summary>
        public bool Bind()
        {
            if (Find(_root, Names.Bar) == null) return false;
            Render();
            return true;
        }

        /// <summary>Render state → icon + cooldown overlay cho 9 slot. Empty = mờ.</summary>
        public void Render()
        {
            var bar = Find(_root, Names.Bar);
            if (bar == null) return;

            // Main slot.
            RenderSlot(Find(bar, Names.MainSlot), _state.Main, _state.Main.SkillId);

            // Aux slots 0..7.
            for (int i = 0; i < JxSkillSlotState.AuxiliarySlotCount; i++)
            {
                var slot = Find(bar, Names.AuxSlotPrefix + i);
                RenderSlot(slot, _state.Aux(i), _state.Aux(i).SkillId);
            }
        }

        private void RenderSlot(VisualElement slotEl, JxSkillSlot slot, int skillId)
        {
            if (slotEl == null) return;
            bool empty = JxSkillSlotState.IsEmpty(slot);
            slotEl.EnableInClassList(EmptyClass, empty);

            var icon = Find(slotEl, Names.Icon);
            var cd = Find(slotEl, Names.Cooldown);
            var cdLabel = Find(slotEl, Names.CdLabel) as Label;

            if (empty)
            {
                slotEl.EnableInClassList(ReadyClass, false);
                slotEl.EnableInClassList(CooldownClass, false);
                if (cd != null) cd.style.display = DisplayStyle.None;
                if (cdLabel != null) cdLabel.text = string.Empty;
                return;
            }

            bool onCd = JxSkillSlotState.IsOnCooldown(slot, _nowMs);
            slotEl.EnableInClassList(ReadyClass, !onCd);
            slotEl.EnableInClassList(CooldownClass, onCd);

            if (cd != null)
            {
                cd.style.display = onCd ? DisplayStyle.Flex : DisplayStyle.None;
                if (onCd)
                {
                    // Overlay fill = fraction còn lại (0..1). timeLoopLayer scaleY.
                    long remain = JxSkillSlotState.CooldownRemainingMs(slot, _nowMs);
                    // Không biết totalDuration tại render → dùng giá trị stored an toàn:
                    // hiển thị 1 dòng full khi onCooldown (fraction chính xác cần
                    // controller truyền totalDuration; đây giữ full overlay).
                    cd.style.height = new StyleLength(new Length(100f, LengthUnit.Percent));
                    if (cdLabel != null)
                        cdLabel.text = Math.Max(0, (int)Math.Ceiling(remain / 1000.0)) + "s";
                }
            }
            else if (cdLabel != null)
            {
                cdLabel.text = string.Empty;
            }
        }

        /// <summary>Coordinator click main skill → cast nếu ready. Trả về false nếu rỗng/cooldown.</summary>
        public bool ClickMain()
        {
            var slot = _state.Main;
            if (JxSkillSlotState.IsEmpty(slot)) return false;
            if (JxSkillSlotState.IsOnCooldown(slot, _nowMs)) return false;
            _bus.PublishActionRequested(JxHudAction.None); // cast đi qua controller (hook riêng)
            return true;
        }

        /// <summary>Coordinator click auxiliary slot → cast nếu ready.</summary>
        public bool ClickAux(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= JxSkillSlotState.AuxiliarySlotCount) return false;
            var slot = _state.Aux(slotIndex);
            if (JxSkillSlotState.IsEmpty(slot)) return false;
            if (JxSkillSlotState.IsOnCooldown(slot, _nowMs)) return false;
            _bus.PublishActionRequested(JxHudAction.None); // cast đi qua controller (hook riêng)
            return true;
        }

        private static VisualElement Find(VisualElement root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            var q = new Queue<VisualElement>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.name == name) return cur;
                int n = cur.childCount;
                for (int i = 0; i < n; i++) q.Enqueue(cur[i]);
            }
            return null;
        }
    }
}
