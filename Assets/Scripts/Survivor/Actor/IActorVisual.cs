using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Visual seam. P1 = ProxyActorVisual. P1.5 = bridge Sandbox MalePlayerVisual / PcNpcVisual.</summary>
    public interface IActorVisual
    {
        void SyncPosition(Vector3 worldPos);
        /// <summary>Ticket 46 — cập nhật depth theo world Y (Y cao = render trước). Gọi sau SyncPosition.</summary>
        void SyncDepth(float worldY);
        void SetDirection(int dirIndex8);
        void PlayMove(bool moving);
        void SetAlive(bool alive);
    }
}
