using System;

namespace VLTK.SkillPort
{
    public enum AuthoritativePresentationDispatchResult
    {
        Presented = 0,
        ResyncApplied = 1,
        SequenceObserved = 2,
        EnvelopeRejected = 3,
        ReducerRejected = 4,
        PolicyBlocked = 5,
        ShadowOnly = 6,
        PreloadBlocked = 7,
        ContextMissing = 8,
        SinkRejected = 9,
    }

    public interface IAuthoritativeCombatPresentationContext
    {
        bool TryResolve(
            string sourceEntityId,
            out string factionKey,
            out PlayerVisualTuple visualTuple);
    }

    public interface IAuthoritativeCombatPresentationPreloadGate
    {
        bool CanReveal(int skillId, CombatLifecycleEvent evt);
    }

    public interface IAuthoritativeCombatPresentationSink
    {
        bool TryPresent(CombatLifecycleEvent evt, PlayerVisualTuple visualTuple);
    }

    /// <summary>
    /// Receive-only authoritative presentation boundary. It never calculates
    /// damage, invents lifecycle events, or falls back to local combat authority.
    /// </summary>
    public sealed class AuthoritativeCombatPresentationBridge
    {
        private readonly RealtimeSessionCursor _cursor;
        private readonly CombatPresentationReducer _reducer;
        private readonly IAuthoritativeCombatPresentationContext _context;
        private readonly IAuthoritativeCombatPresentationPreloadGate _preload;
        private readonly IAuthoritativeCombatPresentationSink _sink;

        private RuntimePolicySnapshot _policy;

        public ServerEnvelopeAcceptance lastEnvelopeAcceptance { get; private set; }
        public PresentationApplyResult lastReducerResult { get; private set; }
        public CombatPresentationReducer reducer => _reducer;

        public AuthoritativeCombatPresentationBridge(
            IAuthoritativeCombatPresentationContext context,
            IAuthoritativeCombatPresentationPreloadGate preload,
            IAuthoritativeCombatPresentationSink sink,
            RealtimeSessionCursor cursor = null,
            CombatPresentationReducer reducer = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _preload = preload ?? throw new ArgumentNullException(nameof(preload));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _cursor = cursor ?? new RealtimeSessionCursor();
            _reducer = reducer ?? new CombatPresentationReducer();
        }

        public void BeginSession(ulong sessionEpoch, ulong initialServerSequence, ulong initialServerTick)
        {
            _cursor.Begin(sessionEpoch, initialServerSequence, initialServerTick);
        }

        public void SetRuntimePolicy(RuntimePolicySnapshot policy)
        {
            _policy = policy;
        }

        public void ClearTransientPresentationState()
        {
            _reducer.ClearTransientState();
        }

        public bool RequiresAuthoritativeInput(int skillId, string factionKey)
        {
            if (_policy == null)
                return false;
            SkillRuntimeMode mode = _policy.Resolve(skillId, factionKey);
            return mode.exposed &&
                   (mode.authorityMode == SkillAuthorityMode.GoActiveLegacyShadow ||
                    mode.authorityMode == SkillAuthorityMode.GoOnly);
        }

        public AuthoritativePresentationDispatchResult ApplyServerEnvelope(
            global::Game.V1.ServerEnvelope envelope)
        {
            lastEnvelopeAcceptance = GameV1SkillPortAdapters.AcceptServerEnvelope(_cursor, envelope);
            if (lastEnvelopeAcceptance != ServerEnvelopeAcceptance.Accepted)
                return AuthoritativePresentationDispatchResult.EnvelopeRejected;

            if (envelope.ActiveCombatResync != null)
                return ApplyResync(envelope.ActiveCombatResync, envelope.ServerSeq);

            if (envelope.Combat != null && envelope.Combat.ResyncState != null)
                return ApplyResync(envelope.Combat.ResyncState, envelope.ServerSeq);

            if (envelope.Combat == null)
            {
                return _reducer.ObserveAcceptedServerEnvelope(envelope.ServerSeq, envelope.ServerTick)
                    ? AuthoritativePresentationDispatchResult.SequenceObserved
                    : AuthoritativePresentationDispatchResult.ReducerRejected;
            }

            CombatLifecycleEvent evt = GameV1SkillPortAdapters.ToLifecycleEvent(
                envelope.Combat,
                envelope.ServerSeq);
            lastReducerResult = _reducer.Apply(evt);
            if (lastReducerResult != PresentationApplyResult.Applied)
                return AuthoritativePresentationDispatchResult.ReducerRejected;

            if (!_context.TryResolve(evt.sourceEntityId, out string factionKey, out PlayerVisualTuple tuple) ||
                tuple == null || !tuple.IsCanonical())
                return AuthoritativePresentationDispatchResult.ContextMissing;

            if (_policy == null)
                return AuthoritativePresentationDispatchResult.PolicyBlocked;

            SkillRuntimeMode mode = _policy.Resolve(evt.skillId, factionKey);
            if (!mode.exposed || mode.presentationMode == SkillPresentationMode.Disabled ||
                mode.authorityMode == SkillAuthorityMode.Disabled)
                return AuthoritativePresentationDispatchResult.PolicyBlocked;

            bool goAuthoritative =
                mode.authorityMode == SkillAuthorityMode.GoActiveLegacyShadow ||
                mode.authorityMode == SkillAuthorityMode.GoOnly;
            if (!goAuthoritative || mode.presentationMode == SkillPresentationMode.GraphV2Shadow ||
                mode.presentationMode == SkillPresentationMode.Legacy)
                return AuthoritativePresentationDispatchResult.ShadowOnly;

            if (mode.presentationMode != SkillPresentationMode.GraphV2)
                return AuthoritativePresentationDispatchResult.PolicyBlocked;
            if (!_preload.CanReveal(evt.skillId, evt))
                return AuthoritativePresentationDispatchResult.PreloadBlocked;

            return _sink.TryPresent(evt, tuple)
                ? AuthoritativePresentationDispatchResult.Presented
                : AuthoritativePresentationDispatchResult.SinkRejected;
        }

        private AuthoritativePresentationDispatchResult ApplyResync(
            global::Game.V1.ActiveCombatResyncState state,
            ulong serverSequence)
        {
            CombatPresentationSnapshot snapshot =
                GameV1SkillPortAdapters.ToPresentationSnapshot(state, serverSequence);
            lastReducerResult = _reducer.ApplySnapshot(snapshot)
                ? PresentationApplyResult.Applied
                : PresentationApplyResult.InvalidEvent;
            return lastReducerResult == PresentationApplyResult.Applied
                ? AuthoritativePresentationDispatchResult.ResyncApplied
                : AuthoritativePresentationDispatchResult.ReducerRejected;
        }
    }
}
