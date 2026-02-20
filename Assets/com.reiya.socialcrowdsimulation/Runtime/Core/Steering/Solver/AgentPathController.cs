using UnityEngine;
using System.Collections.Generic;

namespace CollisionAvoidance
{
    /// <summary>
    /// Orchestrator component that bridges MotionMatching with the pipeline-based social force model.
    /// Inherits from BasePathController for MotionMatching integration.
    /// Maintains backward compatibility: external code still references AgentPathController
    /// by type and accesses the same public API.
    ///
    /// Pipeline execution order per tick:
    ///   L1-2 (Perception+Attention) -> L3 (Prediction) -> L4 (Decision) -> L5 (Motor) -> Animation
    /// </summary>
    [RequireComponent(typeof(AgentPipelineCoordinator))]
    public class AgentPathController : BasePathController
    {
        // Backward-compat: AgentState is synced from pipeline outputs each frame
        // so AgentDebugGizmos and other code that reads agentState still works.
        [HideInInspector] public AgentState agentState = new AgentState();

        // Pipeline coordinator
        private AgentPipelineCoordinator coordinator;
        private AgentDebugGizmos debugGizmos;

        // Backing fields for properties that may be set before Awake()
        // (e.g. during Editor-time Instantiate in AvatarCreatorQuickGraph)
        [SerializeField, HideInInspector] private GroupManager _groupManager;
        [SerializeField, HideInInspector] private float _initialSpeed = 0.7f;
        [SerializeField, HideInInspector] private float _minSpeed = 0.0f;
        [SerializeField, HideInInspector] private float _maxSpeed = 1.0f;
        [SerializeField, HideInInspector] private float _slowingRadius = 3.0f;

        // Force weights (stored locally, passed to pipeline as ForceWeights struct)
        private float _toGoalWeight = 2.0f;
        private float _avoidanceWeight = 2.0f;
        private float _avoidNeighborWeight = 2.0f;
        private float _groupForceWeight = 0.5f;
        private float _wallRepForceWeight = 0.2f;
        private float _avoidObstacleWeight = 1.0f;
        [SerializeField, HideInInspector] private string _groupName;

        // --- Event (backward-compatible, raised by L4 Decision layer) ---
        public event EventDelegate OnMutualGaze;
        public delegate void EventDelegate(GameObject targetAgent);

        // --- Backward-compatible public properties ---
        // Force weights: set by AgentManager.SetPathControllerParams
        [HideInInspector] public float avoidanceWeight
        {
            get => _avoidanceWeight;
            set { _avoidanceWeight = value; agentState.AvoidanceWeight = value; }
        }

        [HideInInspector] public float toGoalWeight
        {
            get => _toGoalWeight;
            set { _toGoalWeight = value; agentState.ToGoalWeight = value; }
        }

        [HideInInspector] public float avoidNeighborWeight
        {
            get => _avoidNeighborWeight;
            set { _avoidNeighborWeight = value; agentState.AvoidNeighborWeight = value; }
        }

        [HideInInspector] public float groupForceWeight
        {
            get => _groupForceWeight;
            set { _groupForceWeight = value; agentState.GroupForceWeight = value; }
        }

        [HideInInspector] public float wallRepForceWeight
        {
            get => _wallRepForceWeight;
            set { _wallRepForceWeight = value; agentState.WallRepForceWeight = value; }
        }

        [HideInInspector] public float avoidObstacleWeight
        {
            get => _avoidObstacleWeight;
            set { _avoidObstacleWeight = value; agentState.AvoidObstacleWeight = value; }
        }

        // Speed settings (forwarded to coordinator's motor layer)
        public float initialSpeed
        {
            get => _initialSpeed;
            set
            {
                _initialSpeed = value;
                if (coordinator != null) coordinator.UpdateMotorConfig(_initialSpeed, _minSpeed, _maxSpeed, _slowingRadius);
            }
        }

        public float minSpeed
        {
            get => _minSpeed;
            set
            {
                _minSpeed = value;
                if (coordinator != null) coordinator.UpdateMotorConfig(_initialSpeed, _minSpeed, _maxSpeed, _slowingRadius);
            }
        }

        public float maxSpeed
        {
            get => _maxSpeed;
            set
            {
                _maxSpeed = value;
                if (coordinator != null) coordinator.UpdateMotorConfig(_initialSpeed, _minSpeed, _maxSpeed, _slowingRadius);
            }
        }

        public float slowingRadius
        {
            get => _slowingRadius;
            set
            {
                _slowingRadius = value;
                if (coordinator != null) coordinator.UpdateMotorConfig(_initialSpeed, _minSpeed, _maxSpeed, _slowingRadius);
            }
        }

        // Group info
        public string groupName
        {
            get => _groupName;
            set
            {
                _groupName = value;
                agentState.GroupName = value;
                if (coordinator != null) coordinator.SetGroupName(value);
            }
        }

        // GroupManager
        public GroupManager groupManager
        {
            get => _groupManager;
            set
            {
                _groupManager = value;
                if (coordinator != null) coordinator.SetGroupManager(value);
            }
        }

        // Gizmo properties (forwarded to AgentDebugGizmos)
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

        protected virtual void Awake()
        {
            // Resolve components
            coordinator = GetComponent<AgentPipelineCoordinator>();
            debugGizmos = GetComponent<AgentDebugGizmos>();

            // Ensure collision avoidance is initialized
            if (!collisionAvoidance.IsInitialized)
            {
                collisionAvoidance.InitCollisionAvoidanceController();
            }

            // Initialize pipeline coordinator
            coordinator.Initialize(
                collisionAvoidance,
                _groupManager,
                _groupName,
                _initialSpeed,
                _minSpeed,
                _maxSpeed,
                _slowingRadius,
                () => agentPathManager.CurrentTargetNodePosition,
                () => collisionAvoidance.GetUpperBodyAnimationState());

            // Subscribe to target reached for speed transition
            agentPathManager.OnTargetReached += coordinator.OnTargetReached;

            // Initialize shared state for backward compat
            agentState.CurrentPosition = CurrentPosition;
            agentState.CurrentDirection = CurrentDirection;
            agentState.CurrentSpeed = CurrentSpeed;
            agentState.GroupName = _groupName;

            // Initialize motion matching (from BasePathController)
            InitMotionMathing();
        }

        protected virtual void OnDisable()
        {
            if (agentPathManager != null)
            {
                agentPathManager.OnTargetReached -= coordinator.OnTargetReached;
            }
        }

        protected override void OnUpdate()
        {
            // Build force weights from current property values
            ForceWeights weights = new ForceWeights(
                _toGoalWeight, _avoidanceWeight, _avoidNeighborWeight,
                _groupForceWeight, _wallRepForceWeight, _avoidObstacleWeight);

            // Pipeline works in world space; CurrentPosition is a local offset
            Vector3 worldPosition = (Vector3)GetCurrentPosition();
            AgentFrame frame = new AgentFrame(worldPosition, agentState.CurrentDirection, coordinator.GetMotorSpeed());

            // Run the full pipeline once for actual movement
            MotorOutput motor = coordinator.Tick(frame, Time.deltaTime, weights);

            // Convert motor output (world space) back to local offset for MotionMatching
            CurrentPosition = motor.NextPosition - (Vector3)transform.position;
            agentState.CurrentPosition = CurrentPosition;
            agentState.CurrentDirection = motor.NextDirection;
            agentState.CurrentSpeed = motor.ActualSpeed;
            agentState.OnInSlowingArea = motor.IsInSlowingArea;

            // Predict future positions for MotionMatching trajectory features
            // PredictPosition uses local offset since GetWorldPredictedPos adds transform.position
            for (int i = 0; i < NumberPredictionPos; i++)
            {
                coordinator.PredictPosition(
                    DatabaseDeltaTime * TrajectoryPosPredictionFrames[i],
                    CurrentPosition,
                    coordinator.GetMotorSpeed(),
                    out PredictedPositions[i],
                    out PredictedDirections[i],
                    agentState.CurrentDirection);
            }

            // Sync force vectors for backward compat (AgentDebugGizmos reads these)
            DecisionOutput decision = coordinator.LastDecision;
            agentState.ToGoalVector = decision.ToGoalForce;
            agentState.AvoidanceVector = decision.AvoidanceForce;
            agentState.AvoidNeighborsVector = decision.AnticipatedCollisionForce;
            agentState.GroupForce = decision.GroupForce;
            agentState.WallRepForce = decision.WallForce;
            agentState.AvoidObstacleVector = decision.ObstacleForce;

            // Sync avoidance targets for backward compat
            AttentionOutput attention = coordinator.LastAttention;
            agentState.PotentialAvoidanceTarget = attention.PotentialAvoidanceTarget.HasValue
                ? attention.PotentialAvoidanceTarget.Value.GameObject
                : null;
            agentState.CurrentAvoidanceTarget = attention.UrgentAvoidanceTarget.HasValue
                ? attention.UrgentAvoidanceTarget.Value.GameObject
                : null;

            // Fire mutual gaze event if detected by L4
            if (decision.MutualAvoidanceDetected && decision.MutualAvoidanceTarget != null)
            {
                OnMutualGaze?.Invoke(decision.MutualAvoidanceTarget);
            }

            // Sync to base class fields (CurrentPosition already set above)
            CurrentDirection = agentState.CurrentDirection;
            CurrentSpeed = agentState.CurrentSpeed;

            // Adjust character position (from BasePathController)
            AdjustCharacterPosition();
            ClampSimulationBone();
        }

        #region BACKWARD-COMPATIBLE PUBLIC API
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
            return agentState.PotentialAvoidanceTarget;
        }

        public Vector3 GetCurrentAvoidanceVector()
        {
            return agentState.AvoidanceVector;
        }

        public CollisionAvoidanceController GetCollisionAvoidanceController()
        {
            return collisionAvoidance;
        }

        public bool GetOnInSlowingArea()
        {
            return agentState.OnInSlowingArea;
        }
        #endregion
    }
}
