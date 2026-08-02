using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Visual seam. P1 = ProxyActorVisual. P1.5 = bridge Sandbox MalePlayerVisual / PcNpcVisual.</summary>
    public interface IActorVisual
    {
        void SyncPosition(Vector3 worldPos);
        void SetDirection(int dirIndex8);
        void PlayMove(bool moving);
        void SetAlive(bool alive);
    }
}
