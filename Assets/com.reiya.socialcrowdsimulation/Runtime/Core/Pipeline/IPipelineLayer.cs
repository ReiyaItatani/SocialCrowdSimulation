namespace CollisionAvoidance
{
    /// <summary>
    /// L1-2: Perception + Attention layer.
    /// Filters force-calculation targets to "FOV + attention filter passed" agents only.
    /// This is the ONLY layer that calls GetComponent on neighbouring agents,
    /// producing PerceivedAgent structs so downstream layers are pure.
    /// </summary>
    public interface IPerceptionAttentionLayer
    {
        AttentionOutput Tick(AgentFrame frame, SensorInput sensors, GroupContext group);
    }

    /// <summary>
    /// L3: Prediction layer.
    /// Computes predicted future positions for perceived agents.
    /// Separates prediction math from force computation.
    /// </summary>
    public interface IPredictionLayer
    {
        PredictionOutput Tick(AttentionOutput attention, AgentFrame frame, GroupContext group);
    }

    /// <summary>
    /// L4: Decision layer.
    /// Weighted combination of all social forces — the "sliders" live here.
    /// Swapping this layer replaces the force model (e.g., Social Force → ORCA).
    /// </summary>
    public interface IDecisionLayer
    {
        DecisionOutput Tick(DecisionInput input, AgentFrame frame, ForceWeights weights,
            GroupContext group, float deltaTime);
    }

    /// <summary>
    /// L5: Motor Constraints layer.
    /// Applies max speed, acceleration limits, and goal slowing.
    /// Produces final position/direction/speed for the animation system.
    /// </summary>
    public interface IMotorLayer
    {
        MotorOutput Tick(DecisionOutput decision, AgentFrame frame, MotorContext motor,
            GroupContext group, float deltaTime);

        /// <summary>
        /// Set the initial speed value. Called once during agent setup.
        /// Runtime configuration (min/max speed, slowing radius) is passed per-tick via MotorContext.
        /// </summary>
        void Initialize(float initialSpeed);
    }
}
