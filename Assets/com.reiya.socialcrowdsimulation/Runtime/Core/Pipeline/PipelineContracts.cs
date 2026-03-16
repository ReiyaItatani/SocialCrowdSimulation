using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Represents a neighboring agent as perceived by L1-2 (Perception + Attention).
    /// All IParameterManager.GetComponent() calls are resolved here so downstream
    /// layers never need to call GetComponent.
    /// </summary>
    public readonly struct PerceivedAgent
    {
        public readonly GameObject GameObject;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly float Speed;
        public readonly Vector3 AvoidanceVector;
        public readonly string GroupName;
        public readonly bool IsSameGroup;
        public readonly float ColliderRadius;
        public readonly bool IsGroupTag;
        public readonly int InstanceId;

        public PerceivedAgent(GameObject gameObject, Vector3 position, Vector3 direction,
            float speed, Vector3 avoidanceVector, string groupName, bool isSameGroup,
            float colliderRadius, bool isGroupTag, int instanceId)
        {
            GameObject = gameObject;
            Position = position;
            Direction = direction;
            Speed = speed;
            AvoidanceVector = avoidanceVector;
            GroupName = groupName;
            IsSameGroup = isSameGroup;
            ColliderRadius = colliderRadius;
            IsGroupTag = isGroupTag;
            InstanceId = instanceId;
        }
    }

    /// <summary>
    /// Output of L1-2: Perception + Attention layer.
    /// Filters force-calculation targets to "FOV + attention filter passed" agents only.
    /// Wall/obstacle data bypasses L1-2 and is resolved by the Coordinator into DecisionInput.
    /// </summary>
    public readonly struct AttentionOutput
    {
        /// <summary>Agents visible in FOV that passed attention filter.</summary>
        public readonly List<PerceivedAgent> VisibleAgents;

        /// <summary>Agents within the immediate avoidance area.</summary>
        public readonly List<PerceivedAgent> AvoidanceAreaAgents;

        /// <summary>Most urgent avoidance target from DecideUrgentAvoidanceTarget.</summary>
        public readonly PerceivedAgent? UrgentAvoidanceTarget;

        /// <summary>Potential anticipated collision target from SteerToAvoidNeighbors.</summary>
        public readonly PerceivedAgent? PotentialAvoidanceTarget;

        /// <summary>Weight multiplier for the urgent avoidance target (from TagChecker).</summary>
        public readonly float UrgentTargetWeight;

        // Environment perception (resolved from NormalVector components in L1-2)
        public readonly Vector3 WallNormal;
        public readonly bool HasWall;
        public readonly Vector3 ClosestObstacleNormal;
        public readonly bool HasObstacle;
        public readonly Vector3 AvoidanceColliderSize;
        public readonly float AgentColliderRadius;

        public AttentionOutput(List<PerceivedAgent> visibleAgents, List<PerceivedAgent> avoidanceAreaAgents,
            PerceivedAgent? urgentAvoidanceTarget, PerceivedAgent? potentialAvoidanceTarget,
            float urgentTargetWeight,
            Vector3 wallNormal, bool hasWall,
            Vector3 closestObstacleNormal, bool hasObstacle,
            Vector3 avoidanceColliderSize, float agentColliderRadius)
        {
            VisibleAgents = visibleAgents;
            AvoidanceAreaAgents = avoidanceAreaAgents;
            UrgentAvoidanceTarget = urgentAvoidanceTarget;
            PotentialAvoidanceTarget = potentialAvoidanceTarget;
            UrgentTargetWeight = urgentTargetWeight;
            WallNormal = wallNormal;
            HasWall = hasWall;
            ClosestObstacleNormal = closestObstacleNormal;
            HasObstacle = hasObstacle;
            AvoidanceColliderSize = avoidanceColliderSize;
            AgentColliderRadius = agentColliderRadius;
        }
    }

    /// <summary>
    /// A neighbor with predicted future position, output by L3 (Prediction).
    /// </summary>
    public readonly struct PredictedNeighbor
    {
        public readonly PerceivedAgent Agent;
        public readonly Vector3 PredictedPosition;
        public readonly Vector3 MyPositionAtApproach;
        public readonly float TimeToApproach;
        public readonly float DistanceAtApproach;

        public PredictedNeighbor(PerceivedAgent agent, Vector3 predictedPosition,
            Vector3 myPositionAtApproach, float timeToApproach, float distanceAtApproach)
        {
            Agent = agent;
            PredictedPosition = predictedPosition;
            MyPositionAtApproach = myPositionAtApproach;
            TimeToApproach = timeToApproach;
            DistanceAtApproach = distanceAtApproach;
        }
    }

    /// <summary>
    /// Output of L3: Prediction layer.
    /// Computes forces against predicted positions, not current positions.
    /// </summary>
    public readonly struct PredictionOutput
    {
        /// <summary>Neighbors with predicted future positions and approach data.</summary>
        public readonly List<PredictedNeighbor> PredictedNeighbors;

        /// <summary>Time to the nearest predicted collision.</summary>
        public readonly float TimeToNearestCollision;

        /// <summary>The most urgent predicted collision target (if any).</summary>
        public readonly PredictedNeighbor? MostUrgentNeighbor;

        public PredictionOutput(List<PredictedNeighbor> predictedNeighbors,
            float timeToNearestCollision, PredictedNeighbor? mostUrgentNeighbor)
        {
            PredictedNeighbors = predictedNeighbors;
            TimeToNearestCollision = timeToNearestCollision;
            MostUrgentNeighbor = mostUrgentNeighbor;
        }
    }

    /// <summary>
    /// Force weights for the L4 Decision layer (the "sliders").
    /// Set by AgentManager via AgentPipelineCoordinator properties.
    /// </summary>
    public readonly struct ForceWeights
    {
        public readonly float ToGoal;
        public readonly float Avoidance;
        public readonly float AnticipatedCollision;
        public readonly float Group;
        public readonly float Wall;
        public readonly float Obstacle;

        public ForceWeights(float toGoal, float avoidance, float anticipatedCollision,
            float group, float wall, float obstacle)
        {
            ToGoal = toGoal;
            Avoidance = avoidance;
            AnticipatedCollision = anticipatedCollision;
            Group = group;
            Wall = wall;
            Obstacle = obstacle;
        }
    }

    /// <summary>
    /// Output of L4: Decision layer.
    /// Weighted combination of forces — this is where the sliders live.
    /// </summary>
    public readonly struct DecisionOutput
    {
        /// <summary>Weighted combined direction (normalized).</summary>
        public readonly Vector3 DesiredDirection;

        /// <summary>Desired speed from decision logic.</summary>
        public readonly float DesiredSpeed;

        /// <summary>Whether mutual avoidance (parallel approach) was detected.</summary>
        public readonly bool MutualAvoidanceDetected;

        /// <summary>The target agent involved in mutual avoidance (for OnMutualGaze event).</summary>
        public readonly GameObject MutualAvoidanceTarget;

        // Individual force vectors for debug visualization (AgentDebugGizmos)
        public readonly Vector3 ToGoalForce;
        public readonly Vector3 AvoidanceForce;
        public readonly Vector3 AnticipatedCollisionForce;
        public readonly Vector3 GroupForce;
        public readonly Vector3 WallForce;
        public readonly Vector3 ObstacleForce;

        public DecisionOutput(Vector3 desiredDirection, float desiredSpeed,
            bool mutualAvoidanceDetected, GameObject mutualAvoidanceTarget,
            Vector3 toGoalForce, Vector3 avoidanceForce, Vector3 anticipatedCollisionForce,
            Vector3 groupForce, Vector3 wallForce, Vector3 obstacleForce)
        {
            DesiredDirection = desiredDirection;
            DesiredSpeed = desiredSpeed;
            MutualAvoidanceDetected = mutualAvoidanceDetected;
            MutualAvoidanceTarget = mutualAvoidanceTarget;
            ToGoalForce = toGoalForce;
            AvoidanceForce = avoidanceForce;
            AnticipatedCollisionForce = anticipatedCollisionForce;
            GroupForce = groupForce;
            WallForce = wallForce;
            ObstacleForce = obstacleForce;
        }
    }

    /// <summary>
    /// Output of L5: Motor Constraints layer.
    /// Final position/direction/speed after applying max speed, acceleration limits,
    /// and goal slowing.
    /// </summary>
    public readonly struct MotorOutput
    {
        /// <summary>Final world position after motor constraints.</summary>
        public readonly Vector3 NextPosition;

        /// <summary>Final movement direction after motor constraints.</summary>
        public readonly Vector3 NextDirection;

        /// <summary>Actual speed after constraints (may differ from desired).</summary>
        public readonly float ActualSpeed;

        /// <summary>Whether agent is in the goal slowing area.</summary>
        public readonly bool IsInSlowingArea;

        public MotorOutput(Vector3 nextPosition, Vector3 nextDirection,
            float actualSpeed, bool isInSlowingArea)
        {
            NextPosition = nextPosition;
            NextDirection = nextDirection;
            ActualSpeed = actualSpeed;
            IsInSlowingArea = isInSlowingArea;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Pure-input types: each layer receives only these + previous layer output
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Common per-agent state shared by all pipeline layers.
    /// Replaces the repeated (Vector3 currentPosition, Vector3 currentDirection, float currentSpeed) triple.
    /// </summary>
    public readonly struct AgentFrame
    {
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly float Speed;

        public AgentFrame(Vector3 position, Vector3 direction, float speed)
        {
            Position = position;
            Direction = direction;
            Speed = speed;
        }
    }

    /// <summary>
    /// Pre-resolved group member data. Replaces GetComponent&lt;IParameterManager&gt; calls
    /// inside L3, L4, and L5.
    /// </summary>
    public readonly struct GroupMember
    {
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly float Speed;

        public GroupMember(Vector3 position, Vector3 direction, float speed)
        {
            Position = position;
            Direction = direction;
            Speed = speed;
        }
    }

    /// <summary>
    /// Pre-resolved group context. Built by the Coordinator from GroupManager
    /// so that no layer needs IGroupDataProvider or GetComponent calls for group data.
    /// </summary>
    public readonly struct GroupContext
    {
        public readonly bool IsInGroup;
        public readonly bool IsGroupColliderActive;
        public readonly string GroupName;

        /// <summary>Group members excluding self. Null when IsInGroup is false.</summary>
        public readonly List<GroupMember> Members;

        /// <summary>Group-averaged frame from GroupParameterManager.</summary>
        public readonly AgentFrame GroupFrame;

        /// <summary>Center of mass of group members excluding self.</summary>
        public readonly Vector3 CenterOfMass;

        public GroupContext(bool isInGroup, bool isGroupColliderActive, string groupName,
            List<GroupMember> members, AgentFrame groupFrame, Vector3 centerOfMass)
        {
            IsInGroup = isInGroup;
            IsGroupColliderActive = isGroupColliderActive;
            GroupName = groupName;
            Members = members;
            GroupFrame = groupFrame;
            CenterOfMass = centerOfMass;
        }

        /// <summary>Default context for non-group (individual) agents.</summary>
        public static GroupContext None => new GroupContext(false, false, null, null, default, Vector3.zero);
    }

    /// <summary>
    /// Raw sensor data fed into L1-2, built by the Coordinator from CollisionAvoidanceController.
    /// L1-2 is the only layer that calls GetComponent on neighbouring agents.
    /// </summary>
    public readonly struct SensorInput
    {
        public readonly List<GameObject> FOVAgents;
        public readonly List<GameObject> AvoidanceAreaAgents;
        public readonly GameObject SelfGameObject;
        /// <summary>From GroupManager shared FOV (used when group collider is active).</summary>
        public readonly List<GameObject> SharedFOVAgents;
        /// <summary>Current wall target (has NormalVector component).</summary>
        public readonly GameObject WallTarget;
        /// <summary>Obstacles in FOV (each has NormalVector component).</summary>
        public readonly List<GameObject> ObstaclesInFOV;
        /// <summary>Size of the avoidance collider (for distance scaling).</summary>
        public readonly Vector3 AvoidanceColliderSize;
        /// <summary>Radius of the agent's CapsuleCollider.</summary>
        public readonly float AgentColliderRadius;

        public SensorInput(List<GameObject> fovAgents, List<GameObject> avoidanceAreaAgents,
            GameObject selfGameObject, List<GameObject> sharedFOVAgents,
            GameObject wallTarget, List<GameObject> obstaclesInFOV,
            Vector3 avoidanceColliderSize, float agentColliderRadius)
        {
            FOVAgents = fovAgents;
            AvoidanceAreaAgents = avoidanceAreaAgents;
            SelfGameObject = selfGameObject;
            SharedFOVAgents = sharedFOVAgents;
            WallTarget = wallTarget;
            ObstaclesInFOV = obstaclesInFOV;
            AvoidanceColliderSize = avoidanceColliderSize;
            AgentColliderRadius = agentColliderRadius;
        }
    }

    /// <summary>
    /// Fully-resolved input for L4 Decision layer.
    /// Contains L3 output + attention targets + pre-resolved wall/obstacle normals.
    /// No GetComponent calls needed inside L4.
    /// </summary>
    public readonly struct DecisionInput
    {
        public readonly PredictionOutput Prediction;
        public readonly PerceivedAgent? UrgentAvoidanceTarget;
        public readonly float UrgentTargetWeight;
        public readonly PerceivedAgent? PotentialAvoidanceTarget;
        public readonly Vector3 WallNormal;
        public readonly bool HasWall;
        public readonly Vector3 ClosestObstacleNormal;
        public readonly bool HasObstacle;
        public readonly Vector3 GoalPosition;
        public readonly Vector3 AvoidanceColliderSize;
        public readonly float AgentColliderRadius;

        public DecisionInput(PredictionOutput prediction,
            PerceivedAgent? urgentAvoidanceTarget, float urgentTargetWeight,
            PerceivedAgent? potentialAvoidanceTarget,
            Vector3 wallNormal, bool hasWall,
            Vector3 closestObstacleNormal, bool hasObstacle,
            Vector3 goalPosition, Vector3 avoidanceColliderSize, float agentColliderRadius)
        {
            Prediction = prediction;
            UrgentAvoidanceTarget = urgentAvoidanceTarget;
            UrgentTargetWeight = urgentTargetWeight;
            PotentialAvoidanceTarget = potentialAvoidanceTarget;
            WallNormal = wallNormal;
            HasWall = hasWall;
            ClosestObstacleNormal = closestObstacleNormal;
            HasObstacle = hasObstacle;
            GoalPosition = goalPosition;
            AvoidanceColliderSize = avoidanceColliderSize;
            AgentColliderRadius = agentColliderRadius;
        }
    }

    // ────────────────────────────────────────────────────────────────
    //  Gaze channel: shared mutable state spanning the entire pipeline
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Priority levels for gaze target selection.
    /// Higher values override lower values within a single tick.
    /// </summary>
    public enum GazePriority
    {
        /// <summary>Look forward (individual) or at center of mass (group).</summary>
        Default = 0,
        /// <summary>L3: predictive look-ahead toward anticipated collision (Matthis et al. 2018).</summary>
        Prediction = 10,
        /// <summary>L4: avoidance target, gaze as intention signal (Ducourant et al. 2022).</summary>
        Decision = 20,
        /// <summary>Physical collision response (look at collided agent).</summary>
        Collision = 30,
        /// <summary>Social filter override (close-range eye contact avoidance, Foulsham et al. 2022).</summary>
        Social = 40
    }

    /// <summary>
    /// Shared mutable gaze channel that spans the entire pipeline.
    /// Unlike other pipeline contracts (readonly structs), GazeState is a mutable class
    /// because multiple layers read and write it within a single tick.
    ///
    /// Owned by AgentPipelineCoordinator. Created once, persisted across ticks.
    /// The coordinator resets per-tick fields (TargetPriority) at tick start
    /// while preserving accumulated state (CurrentLookAtDirection, SmoothedNeckRotation).
    ///
    /// Thread safety: Not needed — pipeline runs synchronously per agent per tick.
    /// </summary>
    public class GazeState
    {
        // ── Per-tick fields (reset by coordinator at tick start) ──

        /// <summary>Current gaze priority for this tick. Reset to Default each tick.</summary>
        public GazePriority TargetPriority;

        /// <summary>World-space position of the gaze target (if any). Zero if direction-only.</summary>
        public Vector3 TargetPosition;

        /// <summary>The GameObject being looked at (if any). Null for direction-based targets.</summary>
        public GameObject TargetObject;

        /// <summary>Whether a specific target was set this tick (vs. default forward gaze).</summary>
        public bool HasExplicitTarget;

        /// <summary>Whether mutual gaze was detected this tick (set by social filter).</summary>
        public bool MutualGazeDetected;

        /// <summary>Whether gaze aversion is active (close-range eye contact avoidance).</summary>
        public bool GazeAversionActive;

        // ── Accumulated state (persists across ticks, updated by Neck Driver) ──

        /// <summary>
        /// Current actual look-at direction (world-space, normalized).
        /// Updated by the Neck Driver after applying neck rotation.
        /// Read by L1-2 to determine FOV center.
        /// </summary>
        public Vector3 CurrentLookAtDirection;

        /// <summary>
        /// Desired look-at direction for this tick (world-space, normalized).
        /// Written by the highest-priority layer this tick.
        /// </summary>
        public Vector3 DesiredLookAtDirection;

        /// <summary>
        /// Accumulated neck rotation (local-space Quaternion, applied by Neck Driver).
        /// Persists across ticks for smooth interpolation.
        /// </summary>
        public Quaternion SmoothedNeckRotation;

        /// <summary>
        /// Tries to set the gaze target. Succeeds only if the given priority
        /// is >= the current TargetPriority.
        /// </summary>
        public bool TrySetTarget(GazePriority priority, Vector3 direction,
            Vector3 targetPosition = default, GameObject targetObject = null)
        {
            if (priority < TargetPriority) return false;

            TargetPriority = priority;
            DesiredLookAtDirection = direction.normalized;
            TargetPosition = targetPosition;
            TargetObject = targetObject;
            HasExplicitTarget = true;
            return true;
        }

        /// <summary>
        /// Called by the coordinator at the start of each tick.
        /// Resets per-tick fields while preserving accumulated state.
        /// </summary>
        public void ResetForTick()
        {
            TargetPriority = GazePriority.Default;
            TargetPosition = Vector3.zero;
            TargetObject = null;
            HasExplicitTarget = false;
            MutualGazeDetected = false;
            GazeAversionActive = false;
            // Ensure DesiredLookAtDirection never stays as zero from a previous failed tick.
            if (DesiredLookAtDirection == Vector3.zero)
            {
                DesiredLookAtDirection = Vector3.forward;
            }
            // NOTE: CurrentLookAtDirection, DesiredLookAtDirection, and SmoothedNeckRotation
            // are NOT reset — they carry forward for temporal continuity.
        }
    }

    /// <summary>
    /// Per-tick context for L5 Motor layer.
    /// Replaces hidden dependencies that were set via Initialize().
    /// </summary>
    public readonly struct MotorContext
    {
        public readonly Vector3 GoalPosition;
        public readonly UpperBodyAnimationState AnimationState;
        public readonly float InitialSpeed;
        public readonly float MinSpeed;
        public readonly float MaxSpeed;
        public readonly float SlowingRadius;

        public MotorContext(Vector3 goalPosition, UpperBodyAnimationState animationState,
            float initialSpeed, float minSpeed, float maxSpeed, float slowingRadius)
        {
            GoalPosition = goalPosition;
            AnimationState = animationState;
            InitialSpeed = initialSpeed;
            MinSpeed = minSpeed;
            MaxSpeed = maxSpeed;
            SlowingRadius = slowingRadius;
        }
    }
}
