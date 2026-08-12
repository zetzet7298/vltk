// -----------------------------------------------------------------------------
// VLTK Mobile — GM access gate for PC GM-token actions.
// PC source: CheckGameMaster() in script/global/gm/lenhbaiadmintestserver.lua.
// Mobile keeps this locked to Editor/Development Build unless an explicit test or
// account-level override is injected.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class GmAccessService
    {
        public bool? overrideAllowed;

        public bool IsAllowed => overrideAllowed ?? (Application.isEditor || Debug.isDebugBuild);

        public string DenialMessage => "Chỉ GM/dev mới được sử dụng Lệnh bài GM Test Server.";

        public static GmAccessService AllowForTests()
            => new GmAccessService { overrideAllowed = true };

        public static GmAccessService DenyForTests()
            => new GmAccessService { overrideAllowed = false };
    }
}
