using System;

namespace VLTK.SkillPort
{
    public enum ReconciliationAction
    {
        None = 0,
        Blend = 1,
        Snap = 2,
    }

    public readonly struct ReconciliationDecision
    {
        public readonly ReconciliationAction action;
        public readonly int blendMilliseconds;

        public ReconciliationDecision(ReconciliationAction action, int blendMilliseconds)
        {
            this.action = action;
            this.blendMilliseconds = blendMilliseconds;
        }
    }

    public static class LocalPoseReconciliationPolicy
    {
        public const int DefaultBlendMilliseconds = 100;

        /// <summary>
        /// Only pose/aim may be predicted. Teleports and errors beyond two
        /// authoritative movement steps snap; bounded drift blends for 100 ms.
        /// </summary>
        public static ReconciliationDecision Evaluate(
            double positionError,
            double authoritativeMaxStep,
            double angularErrorDegrees,
            bool teleport)
        {
            if (positionError < 0 || authoritativeMaxStep < 0 || angularErrorDegrees < 0 ||
                double.IsNaN(positionError) || double.IsNaN(authoritativeMaxStep) ||
                double.IsNaN(angularErrorDegrees))
                return new ReconciliationDecision(ReconciliationAction.Snap, 0);

            if (teleport)
                return new ReconciliationDecision(ReconciliationAction.Snap, 0);
            if (positionError <= double.Epsilon && angularErrorDegrees <= double.Epsilon)
                return new ReconciliationDecision(ReconciliationAction.None, 0);
            if (authoritativeMaxStep <= double.Epsilon ||
                positionError > authoritativeMaxStep * 2.0 ||
                angularErrorDegrees > 45.0)
                return new ReconciliationDecision(ReconciliationAction.Snap, 0);

            return new ReconciliationDecision(
                ReconciliationAction.Blend,
                DefaultBlendMilliseconds);
        }
    }

    /// <summary>
    /// Remote presentation buffer is bounded to 1-3 server ticks. The caller
    /// feeds a rolling p95 jitter estimate; hysteresis prevents oscillation.
    /// </summary>
    public sealed class RemoteInterpolationBufferPolicy
    {
        private const double GrowThresholdTicks = 0.75;
        private const double ShrinkThresholdTicks = 0.25;
        private const long ShrinkStableMilliseconds = 5_000L;

        private long _stableLowJitterMilliseconds;

        public int bufferTicks { get; private set; } = 2;

        public int Observe(double p95JitterTicks, long elapsedMilliseconds)
        {
            if (p95JitterTicks < 0 || double.IsNaN(p95JitterTicks) || elapsedMilliseconds < 0)
            {
                bufferTicks = 3;
                _stableLowJitterMilliseconds = 0;
                return bufferTicks;
            }

            if (p95JitterTicks > GrowThresholdTicks)
            {
                bufferTicks = 3;
                _stableLowJitterMilliseconds = 0;
                return bufferTicks;
            }

            if (p95JitterTicks < ShrinkThresholdTicks)
            {
                _stableLowJitterMilliseconds = checked(_stableLowJitterMilliseconds + elapsedMilliseconds);
                if (_stableLowJitterMilliseconds >= ShrinkStableMilliseconds && bufferTicks > 1)
                {
                    bufferTicks--;
                    _stableLowJitterMilliseconds = 0;
                }
                return bufferTicks;
            }

            _stableLowJitterMilliseconds = 0;
            if (bufferTicks == 3)
                bufferTicks = 2;
            return bufferTicks;
        }
    }
}
