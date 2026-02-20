using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Pipeline driver that orchestrates the 5-layer Perception-to-Action pipeline.
    /// This is the ONLY component that touches concrete Unity components
    /// (CollisionAvoidanceController, GroupManager, NormalVector).
    /// Each layer receives pure data structs — no GetComponent calls beyond L1-2.
    ///
    /// Execution order per tick:
    ///   1. Gather raw data from concrete components
    ///   2. L1-2 (Perception+Attention)
    ///   3. L3 (Prediction)
    ///   4. Pre-resolve wall/obstacle normals
    ///   5. L4 (Decision)
    ///   6. L5 (Motor)
    /// </summary>
    /// <remarks>
    /// Layers are resolved via GetComponentInChildren so each layer can live
    /// on its own child GameObject (Navigation / PerceptionAttention / Prediction / Decision / Motor).
    /// </remarks>
    public class AgentPipelineCoordinator : MonoBehaviour
    {
        // Layer references (resolved in Initialize)
        private IPerceptionAttentionLayer perceptionLayer;
        private IPredictionLayer predictionLayer;
        private IDecisionLayer decisionLayer;
        private IMotorLayer motorLayer;

        // Concrete dependencies (used ONLY for building input structs)
        private CollisionAvoidanceController collisionAvoidance;
        private GroupManager groupManager;
        private string groupName;
        private System.Func<Vector3> getGoalPosition;
        private System.Func<UpperBodyAnimationState> getUpperBodyAnimationState;

        // Speed configuration (passed through to MotorContext each tick)
        private float initialSpeed = 0.7f;
        private float minSpeed;
        private float maxSpeed = 1.0f;
        private float slowingRadius = 3.0f;

        // Latest pipeline outputs (readable by AgentPathController and debug tools).
        // WARNING: These contain references to pooled lists that are cleared on the next tick.
        // Only read these within the same frame they were produced.
        public DecisionOutput LastDecision { get; private set; }
        public MotorOutput LastMotor { get; private set; }
        public AttentionOutput LastAttention { get; private set; }

        // Pooled list — safe because pipeline runs synchronously per tick.
        private readonly List<GroupMember> pooledGroupMembers = new List<GroupMember>();

        private bool initialized;

        /// <summary>
        /// Initialize the pipeline coordinator with all dependencies.
        /// Called by AgentPathController.Awake().
        /// </summary>
        public void Initialize(
            CollisionAvoidanceController collisionAvoidance,
            GroupManager groupManager,
            string groupName,
            float initialSpeed,
            float minSpeed,
            float maxSpeed,
            float slowingRadius,
            System.Func<Vector3> getGoalPosition,
            System.Func<UpperBodyAnimationState> getUpperBodyAnimationState)
        {
            this.collisionAvoidance = collisionAvoidance;
            this.groupManager = groupManager;
            this.groupName = groupName;
            this.initialSpeed = initialSpeed;
            this.minSpeed = minSpeed;
            this.maxSpeed = maxSpeed;
            this.slowingRadius = slowingRadius;
            this.getGoalPosition = getGoalPosition;
            this.getUpperBodyAnimationState = getUpperBodyAnimationState;

            // Resolve layer components via interface (layers live on child GameObjects)
            perceptionLayer = GetComponentInChildren<IPerceptionAttentionLayer>();
            predictionLayer = GetComponentInChildren<IPredictionLayer>();
            decisionLayer = GetComponentInChildren<IDecisionLayer>();
            motorLayer = GetComponentInChildren<IMotorLayer>();

            // Initialize motor layer with initial speed
            float effectiveInitialSpeed = initialSpeed < minSpeed ? minSpeed : initialSpeed;
            motorLayer.Initialize(effectiveInitialSpeed);

            initialized = true;
        }

        public void SetGroupManager(GroupManager gm) { groupManager = gm; }
        public void SetGroupName(string name) { groupName = name; }

        public void UpdateMotorConfig(float newInitialSpeed, float newMinSpeed, float newMaxSpeed, float newSlowingRadius)
        {
            initialSpeed = newInitialSpeed;
            minSpeed = newMinSpeed;
            maxSpeed = newMaxSpeed;
            slowingRadius = newSlowingRadius;

            var motor = motorLayer as DefaultMotorLayer;
            if (motor != null)
            {
                motor.UpdateInitialSpeed(newInitialSpeed, newMinSpeed);
            }
        }

        /// <summary>
        /// Execute the full pipeline for one frame.
        /// </summary>
        public MotorOutput Tick(AgentFrame frame, float deltaTime, ForceWeights weights)
        {
            if (!initialized)
            {
                return new MotorOutput(frame.Position, frame.Direction, frame.Speed, false);
            }

            // ── 1. Gather raw data from concrete components ──

            SensorInput sensors = BuildSensorInput();
            GroupContext group = BuildGroupContext(frame);

            // ── 2. L1-2: Perception + Attention ──

            AttentionOutput attention = perceptionLayer.Tick(frame, sensors, group);
            LastAttention = attention;

            // ── 3. L3: Prediction ──

            PredictionOutput prediction = predictionLayer.Tick(attention, frame, group);

            // ── 4. Build DecisionInput (pre-resolve wall/obstacle normals) ──

            DecisionInput decisionInput = BuildDecisionInput(prediction, attention, frame);

            // ── 5. L4: Decision (force combination) ──

            DecisionOutput decision = decisionLayer.Tick(decisionInput, frame, weights, group, deltaTime);
            LastDecision = decision;

            // ── 6. L5: Motor Constraints ──

            MotorContext motorCtx = BuildMotorContext();
            MotorOutput motor = motorLayer.Tick(decision, frame, motorCtx, group, deltaTime);
            LastMotor = motor;

            return motor;
        }

        /// <summary>
        /// Get the current speed from the motor layer.
        /// </summary>
        public float GetMotorSpeed()
        {
            var motor = motorLayer as DefaultMotorLayer;
            return motor != null ? motor.GetCurrentSpeed() : 1.0f;
        }

        /// <summary>
        /// Predict a future position without running the full pipeline.
        /// </summary>
        public void PredictPosition(float time, Vector3 currentPosition, float currentSpeed,
            out Vector3 predictedPosition, out Vector3 predictedDirection, Vector3 currentDirection = default)
        {
            Vector3 direction = LastDecision.DesiredDirection;
            if (direction == Vector3.zero)
            {
                direction = currentDirection != Vector3.zero ? currentDirection : Vector3.forward;
            }
            predictedDirection = direction;
            predictedPosition = currentPosition + direction * currentSpeed * time;
        }

        /// <summary>
        /// Notify motor layer that a path target was reached.
        /// </summary>
        public void OnTargetReached()
        {
            var motor = motorLayer as DefaultMotorLayer;
            if (motor != null)
            {
                motor.OnTargetReached(initialSpeed);
            }
        }

        // ────────────────────────────────────────────────────────────────
        //  Input struct builders — all concrete component access happens here
        // ────────────────────────────────────────────────────────────────

        private SensorInput BuildSensorInput()
        {
            List<GameObject> fovAgents = collisionAvoidance != null ? collisionAvoidance.GetOthersInFOV() : null;
            List<GameObject> avoidanceAgents = collisionAvoidance != null ? collisionAvoidance.GetOthersInAvoidanceArea() : null;
            GameObject selfGO = collisionAvoidance != null ? collisionAvoidance.GetAgentGameObject() : null;
            List<GameObject> sharedFOVAgents = groupManager != null ? groupManager.GetAgentsInSharedFOV() : null;
            GameObject wallTarget = collisionAvoidance != null ? collisionAvoidance.GetCurrentWallTarget() : null;
            List<GameObject> obstacles = collisionAvoidance != null ? collisionAvoidance.GetObstaclesInFOV() : null;
            Vector3 avoidanceColliderSize = collisionAvoidance != null ? collisionAvoidance.GetAvoidanceColliderSize() : Vector3.one;
            CapsuleCollider selfCollider = collisionAvoidance != null ? collisionAvoidance.GetAgentCollider() : null;
            float agentColliderRadius = selfCollider != null ? selfCollider.radius : 0.3f;

            return new SensorInput(fovAgents, avoidanceAgents, selfGO, sharedFOVAgents,
                wallTarget, obstacles, avoidanceColliderSize, agentColliderRadius);
        }

        private GroupContext BuildGroupContext(AgentFrame frame)
        {
            if (groupName == null || groupName == "Individual" || groupManager == null)
                return GroupContext.None;

            List<GameObject> groupAgents = groupManager.GetGroupAgents();
            if (groupAgents == null || groupAgents.Count == 0)
                return GroupContext.None;

            bool isGroupColliderActive = groupManager.GetOnGroupCollider();
            GameObject selfGO = collisionAvoidance != null ? collisionAvoidance.GetAgentGameObject() : null;

            pooledGroupMembers.Clear();
            Vector3 centerOfMassSum = Vector3.zero;

            foreach (GameObject go in groupAgents)
            {
                if (go == null || !go || go == selfGO) continue;
                IParameterManager param = go.GetComponent<IParameterManager>();
                if (param == null) continue;

                Vector3 pos = param.GetCurrentPosition();
                pooledGroupMembers.Add(new GroupMember(pos, param.GetCurrentDirection(), param.GetCurrentSpeed()));
                centerOfMassSum += pos;
            }

            Vector3 centerOfMass = pooledGroupMembers.Count > 0 ? centerOfMassSum / pooledGroupMembers.Count : frame.Position;

            // Group-level frame (from GroupParameterManager)
            AgentFrame groupFrame = frame;
            IParameterManager groupParam = groupManager.GetGroupParameterManager();
            if (groupParam != null)
            {
                groupFrame = new AgentFrame(
                    groupParam.GetCurrentPosition(),
                    groupParam.GetCurrentDirection(),
                    groupParam.GetCurrentSpeed());
            }

            return new GroupContext(
                isInGroup: true,
                isGroupColliderActive: isGroupColliderActive,
                groupName: groupName,
                members: pooledGroupMembers,
                groupFrame: groupFrame,
                centerOfMass: centerOfMass);
        }

        private DecisionInput BuildDecisionInput(PredictionOutput prediction,
            AttentionOutput attention, AgentFrame frame)
        {
            Vector3 goalPosition = getGoalPosition != null ? getGoalPosition() : frame.Position;

            return new DecisionInput(
                prediction,
                attention.UrgentAvoidanceTarget,
                attention.UrgentTargetWeight,
                attention.PotentialAvoidanceTarget,
                attention.WallNormal, attention.HasWall,
                attention.ClosestObstacleNormal, attention.HasObstacle,
                goalPosition, attention.AvoidanceColliderSize, attention.AgentColliderRadius);
        }

        private MotorContext BuildMotorContext()
        {
            Vector3 goalPosition = getGoalPosition != null ? getGoalPosition() : Vector3.zero;
            UpperBodyAnimationState animState = getUpperBodyAnimationState != null
                ? getUpperBodyAnimationState()
                : UpperBodyAnimationState.Walk;

            return new MotorContext(goalPosition, animState, initialSpeed, minSpeed, maxSpeed, slowingRadius);
        }
    }
}
