// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Sandbox
{
    /// <summary>
    /// HUD nút lên/xuống ngựa. Bấm gọi <see cref="SandboxPlayerController.ToggleMount"/>;
    /// label tự đổi "Lên Ngựa" / "Xuống Ngựa" theo trạng thái cưỡi. PC: phím cưỡi ngựa
    /// trên client toggle giữa đi bộ và cưỡi.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MountToggleButton : MonoBehaviour
    {
        public SandboxPlayerController controller;
        public Text label;

        private bool _lastMounted;
        private bool _init;

        public void Bind(SandboxPlayerController ctrl, Text labelText)
        {
            controller = ctrl;
            label = labelText;
            _init = false;
            RefreshLabel(true);
        }

        public void OnClick()
        {
            if (controller == null)
                return;
            controller.ToggleMount();
            RefreshLabel(true);
        }

        private void Update()
        {
            RefreshLabel(false);
        }

        private void RefreshLabel(bool force)
        {
            if (controller == null || label == null)
                return;
            bool mounted = controller.Mount != null && controller.Mount.IsMounted;
            if (!force && _init && mounted == _lastMounted)
                return;
            _lastMounted = mounted;
            _init = true;
            label.text = mounted ? "Xuống Ngựa" : "Lên Ngựa";
        }
    }
}
