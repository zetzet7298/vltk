using UnityEngine;
using VLTK.Backend;

namespace VLTK.Production.App
{
    [DisallowMultipleComponent]
    public sealed class ProductionBootstrapper : MonoBehaviour
    {
        public bool composeOnAwake = true;
        public ProductionBootStateMachine StateMachine { get; private set; }
        public ProductionComposition Composition { get; private set; }
        public BackendClientRunner BackendRunner { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Idempotent composition entry used by Unity lifecycle and focused
        /// Editor tests.
        /// </summary>
        public void Initialize()
        {
            StateMachine ??= new ProductionBootStateMachine();
            if (composeOnAwake && Composition == null)
                Composition = ProductionCompositionRoot.Create(transform);

            BackendRunner ??= GetComponent<BackendClientRunner>();
            if (BackendRunner == null)
                BackendRunner = gameObject.AddComponent<BackendClientRunner>();

            BackendRunner.runOnStart = true;
            BackendRunner.runCombatDemoOnComplete = false;
            BackendRunner.createAccountOnLoginFailure = true;
            BackendRunner.createRoleWhenMissing = true;
            BackendRunner.enterMapId = 53;
            // Positive PC/server coordinate pinned by revivepos.ini. Unity world
            // projection uses a separate signed coordinate system.
            BackendRunner.enterPosX = 48032;
            BackendRunner.enterPosY = 117504;
            BackendRunner.playerObject = Composition?.avatarController?.gameObject;
        }
    }
}
