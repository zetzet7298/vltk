// -----------------------------------------------------------------------------
// VLTK Mobile — JX Top Status Bar rendering adapter (UI Toolkit)
//
// Nguồn: KuiTopControlVN.cpp. Vẽ nền rolestate.png (211x71), avatar Nam/Nu
// (70x70 theo gender), level/rank label (số xanh), 4 thanh fill blood/mana/
// stamina/kinhnghiem (scaleX = clamp01(cur/max)), label HP/MP/Stamina "cur/max"
// và EXP "%NN".
//
// Adapter thuần C# (không MonoBehaviour) — EditMode-testable. Bọc một VisualElement
// tree (root) chứa các element theo name. State logic tách riêng trong
// JxTopStatusBarState; adapter chỉ render.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// UI Toolkit adapter for the jx-cocos top status bar. Reads the pure
    /// <see cref="JxTopStatusBarState"/> and pushes derived fractions/labels into
    /// a VisualElement tree. Element names match the convention documented below.
    /// </summary>
    public sealed class JxTopStatusBarAdapter
    {
        private readonly VisualElement _root;
        private readonly JxTopStatusBarState _state;

        // Fill elements (width % driven).
        private VisualElement _hpFill, _manaFill, _staminaFill, _expFill;
        // Labels.
        private Label _hpText, _manaText, _staminaText, _expText;
        private Label _levelText, _rankText, _nameText;
        // Avatar (gender-driven background).
        private VisualElement _avatar;

        /// <summary>Element name contract (UXML/USS must use these names).</summary>
        public static class Names
        {
            public const string HpFill = "jx_hp_fill";
            public const string ManaFill = "jx_mana_fill";
            public const string StaminaFill = "jx_stamina_fill";
            public const string ExpFill = "jx_exp_fill";
            public const string HpText = "jx_hp_text";
            public const string ManaText = "jx_mana_text";
            public const string StaminaText = "jx_stamina_text";
            public const string ExpText = "jx_exp_text";
            public const string LevelText = "jx_level_text";
            public const string RankText = "jx_rank_text";
            public const string NameText = "jx_name_text";
            public const string Avatar = "jx_avatar";
        }

        public int RenderCount { get; private set; }

        public JxTopStatusBarAdapter(VisualElement root, JxTopStatusBarState state)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Cache element references by name from the tree.</summary>
        public void Bind()
        {
            if (_root == null) return;
            _hpFill = Find(Names.HpFill);
            _manaFill = Find(Names.ManaFill);
            _staminaFill = Find(Names.StaminaFill);
            _expFill = Find(Names.ExpFill);
            _hpText = Find(Names.HpText) as Label;
            _manaText = Find(Names.ManaText) as Label;
            _staminaText = Find(Names.StaminaText) as Label;
            _expText = Find(Names.ExpText) as Label;
            _levelText = Find(Names.LevelText) as Label;
            _rankText = Find(Names.RankText) as Label;
            _nameText = Find(Names.NameText) as Label;
            _avatar = Find(Names.Avatar);
            Render();
        }

        /// <summary>Push current state into the tree.</summary>
        public void Render()
        {
            RenderCount++;
            SetFill(_hpFill, _state.HpFraction);
            SetFill(_manaFill, _state.ManaFraction);
            SetFill(_staminaFill, _state.StaminaFraction);
            SetFill(_expFill, _state.ExpFraction);
            SetText(_hpText, _state.HpText);
            SetText(_manaText, _state.ManaText);
            SetText(_staminaText, _state.StaminaText);
            SetText(_expText, _state.ExpText);
            SetText(_levelText, _state.LevelText);
            SetText(_rankText, _state.RankText);
            SetText(_nameText, _state.NameText);
            SetAvatarClass(_state.IsFemale);
        }

        private static void SetFill(VisualElement fill, float fraction)
        {
            if (fill == null) return;
            float pct = Mathf.Clamp01(fraction) * 100f;
            fill.style.width = new Length(pct, LengthUnit.Percent);
        }

        private static void SetText(Label label, string text)
        {
            if (label != null) label.text = text;
        }

        private void SetAvatarClass(bool isFemale)
        {
            if (_avatar == null) return;
            // Toggle USS class for gender sprite (resolved in USS via art path).
            _avatar.EnableInClassList("jx-avatar-female", isFemale);
            _avatar.EnableInClassList("jx-avatar-male", !isFemale);
        }

        /// <summary>BFS element lookup by name (no UQuery alloc required).</summary>
        private VisualElement Find(string name)
        {
            if (_root == null || string.IsNullOrEmpty(name)) return null;
            var queue = new System.Collections.Generic.Queue<VisualElement>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name) return current;
                int n = current.childCount;
                for (int i = 0; i < n; i++) queue.Enqueue(current[i]);
            }
            return null;
        }
    }
}
