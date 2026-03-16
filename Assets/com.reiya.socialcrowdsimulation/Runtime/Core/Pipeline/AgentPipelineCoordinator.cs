using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using MotionMatching;
using TrajectoryFeature = MotionMatching.MotionMatchingData.TrajectoryFeature;

namespace CollisionAvoidance
{
    /// <summary>
    /// Unified agent driver: inherits MotionMatchingCharacterController for
    /// animation integration, contains the full 5-layer pipeline, and exposes
    /// the public API that external code consumes.
    ///
    /// Pipeline execution order per tick:
    ///   L1-2 (Perception+Attention) → L3 (Prediction) → L4 (Decision) → L5 (Motor) → Animation
    /// </summary>
    public class AgentPipelineCoordinator : MotionMatchingCharacterController
    {
        // ══════════════════════════════════════════════════════════════════
        //  Motion Matching Parameters
        // ══════════════════════════════════════════════════════════════════

        [Header("Motion Matching Parameters")]
        public string TrajectoryPositionFeatureName = "FuturePosition";
        public string TrajectoryDirectionFeatureName = "FutureDirection";
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Max distance between SimulationBone and SimulationObject")]
        public float MaxDistanceMMAndCharacterController = 0.1f;
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Time needed to move half of the distance between SimulationBone and SimulationObject")]
        public float PositionAdjustmentHalflife = 0.1f;
        [HideInInspector, Range(0.0f, 2.0f), Tooltip("Ratio between the adjustment and the character's velocity to clamp the adjustment")]
        public float PosMaximumAdjustmentRatio = 0.1f;

        // ══════════════════════════════════════════════════════════════════
        //  Component References
        // ══════════════════════════════════════════════════════════════════

        [Header("Collision Avoidance Parameters")]
        public CollisionAvoidanceController collisionAvoidance;
        public AgentPathManager agentPathManager;

        // ══════════════════════════════════════════════════════════════════
        //  Speed & Group Config (serialized for editor-time assignment)
        // ══════════════════════════════════════════════════════════════════

        [SerializeField, HideInInspector] private GroupManager _groupManager;
        [SerializeField, HideInInspector] private float _initialSpeed = 0.7f;
        [SerializeField, HideInInspector] private float _minSpeed = 0.0f;
        [SerializeField, HideInInspector] private float _maxSpeed = 1.0f;
        [SerializeField, HideInInspector] private float _slowingRadius = 3.0f;
        [SerializeField, HideInInspector] private string _groupName;

        // ══════════════════════════════════════════════════════════════════
        //  Motion Matching Runtime State
        // ══════════════════════════════════════════════════════════════════

        private Transform m_CachedTransform;
        private Vector3[] PredictedPositions;
        private Vector3[] PredictedDirections;
        private int TrajectoryPosFeatureIndex;
        private int TrajectoryRotFeatureIndex;
        private int[] TrajectoryPosPredictionFrames;
        private int[] TrajectoryRotPredictionFrames;
        private int NumberPredictionPos => TrajectoryPosPredictionFrames.Length;

        // Current motion state (local offset from transform for MotionMatching)
        private Vector3 CurrentPosition;
        private Vector3 CurrentDirection;
        private float CurrentSpeed = 1.0f;

        // ══════════════════════════════════════════════════════════════════
        //  Force Weights
        // ══════════════════════════════════════════════════════════════════

        private float _toGoalWeight = 2.0f;
        private float _avoidanceWeight = 2.0f;
        private float _avoidNeighborWeight = 2.0f;
        private float _groupForceWeight = 0.5f;
        private float _wallRepForceWeight = 0.2f;
        private float _avoidObstacleWeight = 1.0f;

        // ══════════════════════════════════════════════════════════════════
        //  Pipeline Layer References
        // ══════════════════════════════════════════════════════════════════

        private IPerceptionAttentionLayer perceptionLayer;
        private IPredictionLayer predictionLayer;
        private IDecisionLayer decisionLayer;
        private IMotorLayer motorLayer;

        private SocialGazeFilter socialGazeFilter;
        private bool isIndividual;

        // Shared gaze channel — mutable, spans the entire pipeline.
        private readonly GazeState gazeState = new GazeState
        {
            SmoothedNeckRotation = Quaternion.identity,
            CurrentLookAtDirection = Vector3.forward,
            DesiredLookAtDirection = Vector3.forward
        };

        private readonly List<GroupMember> pooledGroupMembers = new List<GroupMember>();
        private bool initialized;

        // Debug gizmos
        private AgentDebugGizmos debugGizmos;

        // ══════════════════════════════════════════════════════════════════
        //  Public Properties
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Shared gaze channel for external readers (GazeController, CollisionAvoidanceController).</summary>
        public GazeState GazeState => gazeState;

        // Pipeline outputs (read by AgentDebugGizmos)
        public DecisionOutput LastDecision { get; private set; }
        public MotorOutput LastMotor { get; private set; }
        public AttentionOutput LastAttention { get; private set; }

        // Cached transform
        public Transform _cachedTransform => m_CachedTransform ?? (m_CachedTransform = transform);

        // Event
        public event EventDelegate OnMutualGaze;
        public delegate void EventDelegate(GameObject targetAgent);

        // Force weight properties (set by AgentManager)
        [HideInInspector] public float avoidanceWeight { get => _avoidanceWeight; set => _avoidanceWeight = value; }
        [HideInInspector] public float toGoalWeight { get => _toGoalWeight; set => _toGoalWeight = value; }
        [HideInInspector] public float avoidNeighborWeight { get => _avoidNeighborWeight; set => _avoidNeighborWeight = value; }
        [HideInInspector] public float groupForceWeight { get => _groupForceWeight; set => _groupForceWeight = value; }
        [HideInInspector] public float wallRepForceWeight { get => _wallRepForceWeight; set => _wallRepForceWeight = value; }
        [HideInInspector] public float avoidObstacleWeight { get => _avoidObstacleWeight; set => _avoidObstacleWeight = value; }

        // Speed properties
        public float initialSpeed
        {
            get => _initialSpeed;
            set { _initialSpeed = value; if (initialized) PushMotorConfig(); }
        }
        public float minSpeed
        {
            get => _minSpeed;
            set { _minSpeed = value; if (initialized) PushMotorConfig(); }
        }
        public float maxSpeed
        {
            get => _maxSpeed;
            set { _maxSpeed = value; if (initialized) PushMotorConfig(); }
        }
        public float slowingRadius
        {
            get => _slowingRadius;
            set { _slowingRadius = value; if (initialized) PushMotorConfig(); }
        }

        // Group
        public string groupName { get => _groupName; set => _groupName = value; }
        public GroupManager groupManager { get => _groupManager; set => _groupManager = value; }

        // Gizmo forwarding properties
        public bool ShowAvoidanceForce
        {
            get => debugGizmos != null ? debugGizmos.ShowAvoidanceForce : true;
            set { if (debugGizmos != null) debugGizmos.ShowAvoidanceForce = value; }
        }
        public bool ShowCurrentDirection
        {
            get => debugGizmos != null ? debugGizmos.ShowCurrentDirection : true;
            set { if (debugGizmos != null) debugGizmos.ShowCurrentDirection = value; }
        }
        public bool ShowGoalDirection
        {
            get => debugGizmos != null ? debugGizmos.ShowGoalDirection : true;
            set { if (debugGizmos != null) debugGizmos.ShowGoalDirection = value; }
        }
        public bool ShowAnticipatedCollisionAvoidance
        {
            get => debugGizmos != null ? debugGizmos.ShowAnticipatedCollisionAvoidance : true;
            set { if (debugGizmos != null) debugGizmos.ShowAnticipatedCollisionAvoidance = value; }
        }
        public bool ShowGroupForce
        {
            get => debugGizmos != null ? debugGizmos.ShowGroupForce : true;
            set { if (debugGizmos != null) debugGizmos.ShowGroupForce = value; }
        }
        public bool ShowWallForce
        {
            get => debugGizmos != null ? debugGizmos.ShowWallForce : true;
            set { if (debugGizmos != null) debugGizmos.ShowWallForce = value; }
        }
        public bool ShowAvoidObstacleForce
        {
            get => debugGizmos != null ? debugGizmos.ShowAvoidObstacleForce : true;
            set { if (debugGizmos != null) debugGizmos.ShowAvoidObstacleForce = value; }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Unity Lifecycle
        // ══════════════════════════════════════════════════════════════════

        protected virtual void Awake()
        {
            debugGizmos = GetComponent<AgentDebugGizmos>();

            // Ensure collision avoidance is initialized
            if (collisionAvoidance != null && !collisionAvoidance.IsInitialized)
            {
                collisionAvoidance.InitCollisionAvoidanceController();
            }

            // Resolve pipeline layers (live on child GameObjects)
            perceptionLayer = GetComponentInChildren<IPerceptionAttentionLayer>();
            predictionLayer = GetComponentInChildren<IPredictionLayer>();
            decisionLayer = GetComponentInChildren<IDecisionLayer>();
            motorLayer = GetComponentInChildren<IMotorLayer>();

            // Resolve social gaze filter (optional)
            socialGazeFilter = GetComponentInChildren<SocialGazeFilter>();
            isIndividual = _groupName == null || _groupName == "Individual";

            // Wire CustomFocalPoints from SocialBehaviour to gaze filter
            if (socialGazeFilter != null)
            {
                SocialBehaviour sb = GetComponentInChildren<SocialBehaviour>();
                if (sb != null)
                {
                    socialGazeFilter.SetCustomFocalPoints(sb.CustomFocalPoints);
                }
            }

            // Wire GazeState to CollisionAvoidanceController for FOV direction
            if (collisionAvoidance != null)
            {
                collisionAvoidance.SetGazeState(gazeState);
            }

            // Initialize motor layer
            float effectiveInitialSpeed = _initialSpeed < _minSpeed ? _minSpeed : _initialSpeed;
            motorLayer.Initialize(effectiveInitialSpeed);

            // Subscribe to target reached for speed transition
            if (agentPathManager != null)
            {
                agentPathManager.OnTargetReached += OnTargetReached;
            }

            // Initialize motion matching
            InitMotionMatching();

            // Initialize shared state
            CurrentDirection = Vector3.forward;

            initialized = true;
        }

        protected virtual void OnDisable()
        {
            if (agentPathManager != null)
            {
                agentPathManager.OnTargetReached -= OnTargetReached;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  MotionMatchingCharacterController Overrides
        // ══════════════════════════════════════════════════════════════════

        protected override void OnUpdate()
        {
            if (!initialized) return;

            // 1. Build frame + weights
            ForceWeights weights = new ForceWeights(
                _toGoalWeight, _avoidanceWeight, _avoidNeighborWeight,
                _groupForceWeight, _wallRepForceWeight, _avoidObstacleWeight);

            Vector3 worldPosition = (Vector3)GetCurrentPosition();
            AgentFrame frame = new AgentFrame(worldPosition, CurrentDirection, GetMotorSpeed());

            // 2. Run the full pipeline
            MotorOutput motor = RunPipeline(frame, Time.deltaTime, weights);

            // 3. Convert motor output (world space) back to local offset for MotionMatching
            CurrentPosition = motor.NextPosition - (Vector3)transform.position;
            CurrentDirection = motor.NextDirection;
            CurrentSpeed = motor.ActualSpeed;

            // 4. Predict future positions for MotionMatching trajectory features
            for (int i = 0; i < NumberPredictionPos; i++)
            {
                PredictPosition(
                    DatabaseDeltaTime * TrajectoryPosPredictionFrames[i],
                    CurrentPosition,
                    GetMotorSpeed(),
                    out PredictedPositions[i],
                    out PredictedDirections[i],
                    CurrentDirection);
            }

            // 5. Fire mutual gaze event
            if (gazeState.MutualGazeDetected && gazeState.TargetObject != null)
            {
                OnMutualGaze?.Invoke(gazeState.TargetObject);
            }
            else if (LastDecision.MutualAvoidanceDetected && LastDecision.MutualAvoidanceTarget != null)
            {
                OnMutualGaze?.Invoke(LastDecision.MutualAvoidanceTarget);
            }

            // 6. Adjust character position (from MotionMatching integration)
            AdjustCharacterPosition();
            ClampSimulationBone();
        }

        public override float3 GetWorldInitPosition()
        {
            return agentPathManager.CurrentTargetNodePosition + transform.position;
        }

        public override float3 GetWorldInitDirection()
        {
            Vector3 dir = agentPathManager.CurrentTargetNodePosition - agentPathManager.PrevTargetNodePosition;
            return dir.normalized;
        }

        public override void GetTrajectoryFeature(TrajectoryFeature feature, int index, Transform character, NativeArray<float> output)
        {
            if (!feature.SimulationBone) Debug.Assert(false, "Trajectory should be computed using the SimulationBone");
            switch (feature.FeatureType)
            {
                case TrajectoryFeature.Type.Position:
                    Vector3 world = GetWorldPredictedPos(index);
                    Vector3 local = character.InverseTransformPoint(new Vector3(world.x, 0.0f, world.z));
                    output[0] = local.x;
                    output[1] = local.z;
                    break;
                case TrajectoryFeature.Type.Direction:
                    Vector3 worldDir = GetWorldPredictedDir(index);
                    Vector3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.z));
                    output[0] = localDir.x;
                    output[1] = localDir.z;
                    break;
                default:
                    Debug.Assert(false, "Unknown feature type: " + feature.FeatureType);
                    break;
            }
        }

        // ══════════════════════════════════════════════════════════════════
        //  Pipeline Execution
        // ══════════════════════════════════════════════════════════════════

        private MotorOutput RunPipeline(AgentFrame frame, float deltaTime, ForceWeights weights)
        {
            // ── 0. Reset gaze channel for this tick ──
            gazeState.ResetForTick();
            gazeState.DesiredLookAtDirection = frame.Direction != Vector3.zero
                ? frame.Direction
                : gazeState.CurrentLookAtDirection != Vector3.zero
                    ? gazeState.CurrentLookAtDirection
                    : Vector3.forward;

            // ── 1. Gather raw data from concrete components ──
            SensorInput sensors = BuildSensorInput();
            GroupContext group = BuildGroupContext(frame);

            // ── 2. L1-2: Perception + Attention ──
            AttentionOutput attention = perceptionLayer.Tick(frame, sensors, group);
            LastAttention = attention;
            (perceptionLayer as IGazeAwareLayer)?.ProcessGaze(gazeState, frame, group);

            // ── 3. L3: Prediction ──
            PredictionOutput prediction = predictionLayer.Tick(attention, frame, group);
            (predictionLayer as IGazeAwareLayer)?.ProcessGaze(gazeState, frame, group);

            // ── 4. Build DecisionInput (pre-resolve wall/obstacle normals) ──
            DecisionInput decisionInput = BuildDecisionInput(prediction, attention, frame);

            // ── 5. L4: Decision (force combination) ──
            DecisionOutput decision = decisionLayer.Tick(decisionInput, frame, weights, group, deltaTime);
            LastDecision = decision;
            (decisionLayer as IGazeAwareLayer)?.ProcessGaze(gazeState, frame, group);

            // ── 6. L5: Motor Constraints ──
            MotorContext motorCtx = BuildMotorContext();
            MotorOutput motor = motorLayer.Tick(decision, frame, motorCtx, group, deltaTime);
            LastMotor = motor;

            // ── 7. Social Gaze Filter (post-pipeline) ──
            if (socialGazeFilter != null)
            {
                socialGazeFilter.ProcessGaze(gazeState, frame, group, isIndividual, motor.IsInSlowingArea);
            }

            return motor;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Input Struct Builders
        // ══════════════════════════════════════════════════════════════════

        private SensorInput BuildSensorInput()
        {
            List<GameObject> fovAgents = collisionAvoidance != null ? collisionAvoidance.GetOthersInFOV() : null;
            List<GameObject> avoidanceAgents = collisionAvoidance != null ? collisionAvoidance.GetOthersInAvoidanceArea() : null;
            GameObject selfGO = collisionAvoidance != null ? collisionAvoidance.GetAgentGameObject() : null;
            List<GameObject> sharedFOVAgents = _groupManager != null ? _groupManager.GetAgentsInSharedFOV() : null;
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
            if (_groupName == null || _groupName == "Individual" || _groupManager == null)
                return GroupContext.None;

            List<GameObject> groupAgents = _groupManager.GetGroupAgents();
            if (groupAgents == null || groupAgents.Count == 0)
                return GroupContext.None;

            bool isGroupColliderActive = _groupManager.GetOnGroupCollider();
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
            IParameterManager groupParam = _groupManager.GetGroupParameterManager();
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
                groupName: _groupName,
                members: pooledGroupMembers,
                groupFrame: groupFrame,
                centerOfMass: centerOfMass);
        }

        private DecisionInput BuildDecisionInput(PredictionOutput prediction,
            AttentionOutput attention, AgentFrame frame)
        {
            Vector3 goalPosition = agentPathManager != null ? agentPathManager.CurrentTargetNodePosition : frame.Position;

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
            Vector3 goalPosition = agentPathManager != null ? agentPathManager.CurrentTargetNodePosition : Vector3.zero;
            UpperBodyAnimationState animState = collisionAvoidance != null
                ? collisionAvoidance.GetUpperBodyAnimationState()
                : UpperBodyAnimationState.Walk;

            return new MotorContext(goalPosition, animState, _initialSpeed, _minSpeed, _maxSpeed, _slowingRadius);
        }

        // ══════════════════════════════════════════════════════════════════
        //  MotionMatching Integration
        // ══════════════════════════════════════════════════════════════════

        private void InitMotionMatching()
        {
            TrajectoryPosFeatureIndex = -1;
            TrajectoryRotFeatureIndex = -1;
            for (int i = 0; i < MotionMatching.MMData.TrajectoryFeatures.Count; ++i)
            {
                if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryPositionFeatureName) TrajectoryPosFeatureIndex = i;
                if (MotionMatching.MMData.TrajectoryFeatures[i].Name == TrajectoryDirectionFeatureName) TrajectoryRotFeatureIndex = i;
            }
            Debug.Assert(TrajectoryPosFeatureIndex != -1, "Trajectory Position Feature not found");
            Debug.Assert(TrajectoryRotFeatureIndex != -1, "Trajectory Direction Feature not found");

            TrajectoryPosPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryPosFeatureIndex].FramesPrediction;
            TrajectoryRotPredictionFrames = MotionMatching.MMData.TrajectoryFeatures[TrajectoryRotFeatureIndex].FramesPrediction;
            Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length,
                "Trajectory Position and Trajectory Direction Prediction Frames must be the same for AgentPipelineCoordinator");
            for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
            {
                Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i],
                    "Trajectory Position and Trajectory Direction Prediction Frames must be the same for AgentPipelineCoordinator");
            }

            PredictedPositions = new Vector3[NumberPredictionPos];
            PredictedDirections = new Vector3[NumberPredictionPos];
        }

        private void AdjustCharacterPosition()
        {
            float3 characterController = GetCurrentPosition();
            float3 motionMatching = MotionMatching.transform.position;
            float3 differencePosition = characterController - motionMatching;
            float3 adjustmentPosition = Spring.DampAdjustmentImplicit(differencePosition, PositionAdjustmentHalflife, Time.deltaTime);
            float maxLength = PosMaximumAdjustmentRatio * math.length(MotionMatching.Velocity) * Time.deltaTime;
            if (math.length(adjustmentPosition) > maxLength)
            {
                adjustmentPosition = maxLength * math.normalize(adjustmentPosition);
            }
            MotionMatching.SetPosAdjustment(adjustmentPosition);
        }

        private void ClampSimulationBone()
        {
            float3 characterController = GetCurrentPosition();
            float3 motionMatching = MotionMatching.transform.position;
            if (math.distance(characterController, motionMatching) > MaxDistanceMMAndCharacterController)
            {
                float3 newMotionMatchingPos = MaxDistanceMMAndCharacterController * math.normalize(motionMatching - characterController) + characterController;
                MotionMatching.SetPosAdjustment(newMotionMatchingPos - motionMatching);
            }
        }

        private Vector3 GetWorldPredictedPos(int index)
        {
            return PredictedPositions[index] + transform.position;
        }

        private Vector3 GetWorldPredictedDir(int index)
        {
            return PredictedDirections[index];
        }

        // ══════════════════════════════════════════════════════════════════
        //  Public Query API
        // ══════════════════════════════════════════════════════════════════

        public float3 GetCurrentPosition()
        {
            return (float3)_cachedTransform.position + (float3)CurrentPosition;
        }

        public Vector3 GetCurrentDirection()
        {
            return CurrentDirection;
        }

        public float GetCurrentSpeed()
        {
            return CurrentSpeed;
        }

        public float GetMotorSpeed()
        {
            var motor = motorLayer as DefaultMotorLayer;
            return motor != null ? motor.GetCurrentSpeed() : 1.0f;
        }

        public Vector3 GetCurrentGoal()
        {
            return agentPathManager.CurrentTargetNodePosition;
        }

        public string GetGroupName()
        {
            return _groupName;
        }

        public List<GameObject> GetGroupAgents()
        {
            if (_groupManager == null) return null;
            return _groupManager.GetGroupAgents();
        }

        public GameObject GetPotentialAvoidanceTarget()
        {
            return LastAttention.PotentialAvoidanceTarget.HasValue
                ? LastAttention.PotentialAvoidanceTarget.Value.GameObject
                : null;
        }

        public Vector3 GetCurrentAvoidanceVector()
        {
            return LastDecision.AvoidanceForce;
        }

        public CollisionAvoidanceController GetCollisionAvoidanceController()
        {
            return collisionAvoidance;
        }

        public bool GetOnInSlowingArea()
        {
            return LastMotor.IsInSlowingArea;
        }

        // ══════════════════════════════════════════════════════════════════
        //  Pipeline Config Helpers
        // ══════════════════════════════════════════════════════════════════

        private void PushMotorConfig()
        {
            var motor = motorLayer as DefaultMotorLayer;
            if (motor != null)
            {
                motor.UpdateInitialSpeed(_initialSpeed, _minSpeed);
            }
        }

        public void OnTargetReached()
        {
            var motor = motorLayer as DefaultMotorLayer;
            if (motor != null)
            {
                motor.OnTargetReached(_initialSpeed);
            }
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
    }
}
