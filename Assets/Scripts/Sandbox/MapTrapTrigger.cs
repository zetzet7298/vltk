// -----------------------------------------------------------------------------
// VLTK Mobile — invisible trigger component for PC Region_S Trap.dat cells.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [DisallowMultipleComponent]
    public sealed class MapTrapTrigger : MonoBehaviour
    {
        private TrapDefinition _trap;
        private TrapTriggerService _service;
        private bool _fired;

        public void Configure(TrapDefinition trap, TrapTriggerService service)
        {
            _trap = trap;
            _service = service;
            _fired = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_fired || _trap == null || _service == null) return;
            if (other != null && other.GetComponentInParent<SandboxPlayerController>() == null) return;
            _fired = true;
            _service.OnPlayerEnter(_trap);
        }
    }
}
