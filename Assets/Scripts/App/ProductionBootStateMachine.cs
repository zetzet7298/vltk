using VLTK.Production.Networking;

namespace VLTK.Production.App
{
    public enum ProductionBootState
    {
        Created,
        Bootstrap,
        RealmReady,
        Authenticated,
        CharacterSelected,
        ContentVerified,
        RealtimeAdmitted,
        MapLoaded,
        AvatarPresented,
        Ready,
        Failed
    }

    public sealed class ProductionBootStateMachine
    {
        public ProductionBootState State { get; private set; } = ProductionBootState.Created;
        public string FailureCode { get; private set; }

        public bool BeginBootstrap()
        {
            return Move(ProductionBootState.Created, ProductionBootState.Bootstrap, null);
        }

        public bool ApplyBootstrap(RealmBootstrapResponse response)
        {
            if (State != ProductionBootState.Bootstrap)
                return Fail("bad_bootstrap_order");
            if (response == null || !response.IsValid())
                return Fail("bootstrap_invalid");
            State = ProductionBootState.RealmReady;
            return true;
        }

        public bool ApplyAuth(AuthSessionResponse response)
        {
            if (State != ProductionBootState.RealmReady)
                return Fail("bad_auth_order");
            if (response == null || !response.IsValid())
                return Fail("auth_invalid");
            State = ProductionBootState.Authenticated;
            return true;
        }

        public bool ApplyCharacter(CharacterSelectionResponse response)
        {
            if (State != ProductionBootState.Authenticated)
                return Fail("bad_character_order");
            if (response == null || !response.IsValid())
                return Fail("character_invalid");
            State = ProductionBootState.CharacterSelected;
            return true;
        }

        public bool ApplyContent(VerifiedContentResponse response)
        {
            if (State != ProductionBootState.CharacterSelected)
                return Fail("bad_content_order");
            if (response == null || !response.IsValid())
                return Fail("content_invalid");
            State = ProductionBootState.ContentVerified;
            return true;
        }

        public bool ApplyRealtimeAdmission(RealtimeAdmissionResult result)
        {
            if (State != ProductionBootState.ContentVerified)
                return Fail("bad_realtime_order");
            if (!result.admitted)
                return Fail(string.IsNullOrEmpty(result.failureCode) ? "realtime_rejected" : result.failureCode);
            State = ProductionBootState.RealtimeAdmitted;
            return true;
        }

        public bool ApplyMapLoaded(int mapId)
        {
            if (State != ProductionBootState.RealtimeAdmitted)
                return Fail("bad_map_order");
            if (mapId != ProductionMapIds.CanonicalBootMapId)
                return Fail("map_invalid");
            State = ProductionBootState.MapLoaded;
            return true;
        }

        public bool ApplyAvatarPresented()
        {
            if (State != ProductionBootState.MapLoaded)
                return Fail("bad_avatar_order");
            State = ProductionBootState.AvatarPresented;
            return true;
        }

        public bool ApplyJoystickReady()
        {
            if (State != ProductionBootState.AvatarPresented)
                return Fail("bad_joystick_order");
            State = ProductionBootState.Ready;
            return true;
        }

        private bool Move(ProductionBootState from, ProductionBootState to, string failureCode)
        {
            if (State != from)
                return Fail(failureCode ?? "bad_order");
            State = to;
            return true;
        }

        public bool FailExternal(string code)
        {
            return Fail(code);
        }

        private bool Fail(string code)
        {
            FailureCode = SecretRedactor.RedactMessage(code ?? "failed");
            State = ProductionBootState.Failed;
            return false;
        }
    }
}
