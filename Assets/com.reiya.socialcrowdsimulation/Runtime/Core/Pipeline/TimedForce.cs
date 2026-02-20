using UnityEngine;

namespace CollisionAvoidance
{
    public enum TransitionMode
    {
        Lerp,
        Slerp
    }

    /// <summary>
    /// Manages a timed force update with smooth transitions.
    /// Encapsulates the timer+interval+transition pattern used by all forces in DefaultDecisionLayer.
    /// </summary>
    public struct TimedForce
    {
        public Vector3 Current;

        private float updateTimer;
        private float transitionTimer;
        private readonly float updateInterval;
        private readonly float transitionDuration;
        private readonly TransitionMode mode;
        private Vector3 transitionFrom;
        private Vector3 transitionTarget;

        public TimedForce(float updateInterval, float transitionDuration, TransitionMode mode)
        {
            this.updateInterval = updateInterval;
            this.transitionDuration = transitionDuration;
            this.mode = mode;
            Current = Vector3.zero;
            updateTimer = 0f;
            transitionTimer = 0f;
            transitionFrom = Vector3.zero;
            transitionTarget = Vector3.zero;
        }

        /// <summary>
        /// Returns true when a new force should be computed (timer expired).
        /// Otherwise, advances the transition interpolation.
        /// </summary>
        public bool ShouldUpdate(float dt)
        {
            updateTimer += dt;
            if (updateTimer < updateInterval)
            {
                AdvanceTransition(dt);
                return false;
            }
            updateTimer = 0f;
            return true;
        }

        /// <summary>
        /// Set a new target force and begin transitioning from the current value.
        /// </summary>
        public void SetTarget(Vector3 target)
        {
            transitionFrom = Current;
            transitionTarget = target;
            transitionTimer = 0f;
        }

        /// <summary>
        /// Immediately set the force value (no transition).
        /// </summary>
        public void SetImmediate(Vector3 value)
        {
            Current = value;
            transitionFrom = value;
            transitionTarget = value;
            transitionTimer = transitionDuration;
        }

        private void AdvanceTransition(float dt)
        {
            if (transitionTimer >= transitionDuration) return;
            transitionTimer += dt;
            float t = Mathf.Clamp01(transitionTimer / transitionDuration);
            Current = mode == TransitionMode.Lerp
                ? Vector3.Lerp(transitionFrom, transitionTarget, t)
                : Vector3.Slerp(transitionFrom, transitionTarget, t);
        }
    }
}
