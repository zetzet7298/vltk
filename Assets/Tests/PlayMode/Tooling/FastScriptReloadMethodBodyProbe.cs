using UnityEngine;

namespace VLTK.Tests.PlayMode.Tooling
{
    public sealed class FastScriptReloadMethodBodyProbe : MonoBehaviour
    {
        public static int LastInstanceId { get; private set; }
        public static int LastObservedRevision { get; private set; }

        private int _observedRevision;

        private void Awake()
        {
            _observedRevision = Revision();
            LastInstanceId = (int)(EntityId.ToULong(GetEntityId()) & 0xFFFFFFFFu);
            LastObservedRevision = _observedRevision;
            Debug.Log($"[FSR-PILOT] instance={LastInstanceId} revision={LastObservedRevision}");
        }

        private void Update()
        {
            int revision = Revision();
            if (revision == _observedRevision)
                return;

            _observedRevision = revision;
            LastInstanceId = (int)(EntityId.ToULong(GetEntityId()) & 0xFFFFFFFFu);
            LastObservedRevision = revision;
            Debug.Log($"[FSR-PILOT] instance={LastInstanceId} revision={LastObservedRevision}");
        }

        private int Revision() => 7;
    }
}
