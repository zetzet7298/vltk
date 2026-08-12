// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: actor state machine + state events (stun bridge).
// Parity dhcd ActorStateID / ActorStateEvent / ActorSM (BattleCore/ActorSM.cs,
// ActorStunState.cs): Enter/Finish_Stun chuyển state Stun ↔ Idle.
// Mobile: SM là event-driver thuần (không timer riêng) — SurvivorBuffMgr bắn
// Enter_Stun khi bit STUN set, Finish_Stun khi clear. Transition map own:
// Finish_Stun→Idle, Die→Die (khớp AddTransition declaration dhcd).
// -----------------------------------------------------------------------------
using System;

namespace VLTK.Survivor
{
    public enum ActorStateID
    {
        Null = 0,
        Idle = 1,
        Move = 2,
        Skill = 3,
        Die = 4,
        Stun = 5,
        Appear = 6,
    }

    public enum ActorStateEvent
    {
        Actor_Enter_Stun = 6,
        Actor_Finish_Stun = 7,
        Actor_Die = 8,
        Actor_Relive = 9,
    }

    public sealed class SurvivorActorSM
    {
        public ActorStateID State { get; private set; } = ActorStateID.Idle;

        /// <summary>Param từ Enter_Stun (duration giây — own, dhcd param1).</summary>
        public float StunDuration { get; private set; }

        /// <summary>(old, new) — parity OnStateChange → actor.CurrState.</summary>
        public event Action<ActorStateID, ActorStateID> StateChanged;

        public void OnStateEvent(ActorStateEvent e, float param = 0f)
        {
            switch (e)
            {
                case ActorStateEvent.Actor_Enter_Stun:
                    SetState(ActorStateID.Stun);
                    StunDuration = param;
                    break;
                case ActorStateEvent.Actor_Finish_Stun:
                    if (State == ActorStateID.Stun) SetState(ActorStateID.Idle);
                    StunDuration = 0f;
                    break;
                case ActorStateEvent.Actor_Die:
                    SetState(ActorStateID.Die);
                    break;
                case ActorStateEvent.Actor_Relive:
                    SetState(ActorStateID.Idle);
                    break;
            }
        }

        private void SetState(ActorStateID next)
        {
            if (State == next) return;
            var old = State;
            State = next;
            StateChanged?.Invoke(old, next);
        }
    }
}
