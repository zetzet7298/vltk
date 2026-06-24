// -----------------------------------------------------------------------------
// VLTK Mobile — JX Buff row rendering adapter (UI Toolkit, port KuiStateSkillControlVN.cpp)
//
// Nguồn: KuiStateSkillControlVN.cpp draw(). Mỗi buff active → 1 icon (SPR buffPath)
// + label đếm ngược xanh lá stroke đen. Lưới 10 cột. Ẩn khi không có buff.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Mỗi icon là VisualElement
// tên "jx_buff_icon"; label là Label tên "jx_buff_time".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>Adapter UI Toolkit cho JX buff icon row.</summary>
    public sealed class JxBuffRowAdapter
    {
        private readonly VisualElement _root;
        private readonly JxBuffRowState _state;

        public static class Names
        {
            public const string Layer = "jx_buff_layer";
            public const string Icon = "jx_buff_icon";
            public const string Time = "jx_buff_time";
        }

        /// <summary>USS class cho countdown label (xanh lá). Màu chính xác qua USS.</summary>
        public const string TimeClass = "jx-buff-time";

        public JxBuffRowAdapter(VisualElement root, JxBuffRowState state)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>Bind layer element. Trả về false nếu thiếu layer.</summary>
        public bool Bind()
        {
            if (Find(_root, Names.Layer) == null) return false;
            Render();
            return true;
        }

        /// <summary>
        /// Render state → rebuild icon grid. Ẩn layer khi không buff. Mỗi icon =
        /// VisualElement + countdown label. Vị trí theo grid (10 cột, offX 26, offY -36)
        /// tính từ gốc (13, height-87) — ở đây dùng absolute style trong layer.
        /// </summary>
        public void Render()
        {
            var layer = Find(_root, Names.Layer);
            if (layer == null) return;

            // Visible state (ẩn khi không buff).
            layer.style.display = _state.IsVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (!_state.IsVisible) return;

            layer.Clear();
            for (int i = 0; i < _state.Buffs.Count; i++)
            {
                var buff = _state.Buffs[i];
                var (col, row) = JxBuffRowState.GridCell(i);

                var icon = new VisualElement { name = Names.Icon };
                // Vị trí: x = StartOffsetX + col*IconSpacingX;
                //         y = StartOffsetYFromTop + |row*IconSpacingY| (đi xuống).
                icon.style.left = JxBuffRowState.StartOffsetX + col * JxBuffRowState.IconSpacingX;
                icon.style.top = JxBuffRowState.StartOffsetYFromTop + row * (-JxBuffRowState.IconSpacingY);

                // Countdown label.
                var time = new Label(JxBuffRowState.CountdownText(buff)) { name = Names.Time };
                time.AddToClassList(TimeClass);
                icon.Add(time);

                layer.Add(icon);
            }
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
