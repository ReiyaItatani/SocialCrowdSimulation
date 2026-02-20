using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// L1-2: Perception + Attention layer.
    /// Filters force-calculation targets to "FOV + attention filter passed" agents only.
    /// All GetComponent calls happen here, pre-resolving neighbor data
    /// into PerceivedAgent structs so downstream layers (L3, L4, L5) never call GetComponent.
    /// </summary>
    public class DefaultPerceptionAttentionLayer : MonoBehaviour, IPerceptionAttentionLayer
    {
        // Pooled lists to avoid per-tick allocations.
        // Safe because the pipeline runs synchronously — downstream layers consume
        // these lists before the next tick clears them.
        private readonly List<PerceivedAgent> pooledVisibleAgents = new List<PerceivedAgent>();
        private readonly List<PerceivedAgent> pooledAvoidanceAreaAgents = new List<PerceivedAgent>();
        private readonly List<PerceivedAgent> pooledSharedFOVAgents = new List<PerceivedAgent>();

        public AttentionOutput Tick(AgentFrame frame, SensorInput sensors, GroupContext group)
        {
            // Pre-resolve all agents into PerceivedAgent structs
            string groupName = group.GroupName;
            ResolveAgentsInto(sensors.FOVAgents, groupName, sensors.SelfGameObject, pooledVisibleAgents);
            ResolveAgentsInto(sensors.AvoidanceAreaAgents, groupName, sensors.SelfGameObject, pooledAvoidanceAreaAgents);

            // Determine urgent avoidance target
            PerceivedAgent? urgentTarget = null;
            float urgentWeight = 1f;

            if (pooledVisibleAgents.Count > 0)
            {
                urgentTarget = FindUrgentAvoidanceTarget(
                    frame.Direction, frame.Position, frame.Speed,
                    pooledVisibleAgents, pooledAvoidanceAreaAgents);

                if (urgentTarget.HasValue)
                {
                    urgentWeight = ComputeTagWeight(urgentTarget.Value);
                }
            }

            // Determine potential anticipated collision target
            PerceivedAgent? potentialTarget = FindPotentialAvoidanceTarget(
                frame, pooledVisibleAgents, sensors, group);

            // Resolve environment (wall/obstacle normals)
            Vector3 wallNormal;
            bool hasWall;
            Vector3 closestObstacleNormal;
            bool hasObstacle;
            ResolveEnvironment(sensors, frame, group,
                out wallNormal, out hasWall, out closestObstacleNormal, out hasObstacle);

            return new AttentionOutput(
                pooledVisibleAgents,
                pooledAvoidanceAreaAgents,
                urgentTarget,
                potentialTarget,
                urgentWeight,
                wallNormal, hasWall,
                closestObstacleNormal, hasObstacle,
                sensors.AvoidanceColliderSize, sensors.AgentColliderRadius);
        }

        /// <summary>
        /// Resolves raw GameObjects into PerceivedAgent structs by reading IParameterManager.
        /// This is the ONLY place in the pipeline where GetComponent is called on neighbours.
        /// </summary>
        private void ResolveAgentsInto(List<GameObject> rawAgents, string myGroupName,
            GameObject self, List<PerceivedAgent> result)
        {
            result.Clear();
            if (rawAgents == null) return;

            foreach (GameObject go in rawAgents)
            {
                if (go == null || !go || go == self) continue;

                IParameterManager param = go.GetComponent<IParameterManager>();
                if (param == null) continue;

                CapsuleCollider collider = go.GetComponent<CapsuleCollider>();
                float radius = collider != null ? collider.radius : 0.3f;

                string otherGroup = param.GetGroupName();
                bool isSameGroup = myGroupName != null && myGroupName != "Individual" && myGroupName == otherGroup;

                bool isGroupTag = go.CompareTag("Group");
                int instanceId = go.GetInstanceID();

                result.Add(new PerceivedAgent(
                    go,
                    param.GetCurrentPosition(),
                    param.GetCurrentDirection(),
                    param.GetCurrentSpeed(),
                    param.GetCurrentAvoidanceVector(),
                    otherGroup,
                    isSameGroup,
                    radius,
                    isGroupTag,
                    instanceId));
            }
        }

        /// <summary>
        /// Finds the most urgent avoidance target from visible agents.
        /// </summary>
        private PerceivedAgent? FindUrgentAvoidanceTarget(Vector3 myDirection, Vector3 myPosition,
            float mySpeed, List<PerceivedAgent> visibleAgents, List<PerceivedAgent> avoidanceAreaAgents)
        {
            PerceivedAgent? currentTarget = null;
            float localMinTime = PredictionMath.DefaultMinTimeToCollision;

            foreach (PerceivedAgent agent in visibleAgents)
            {
                float time = PredictionMath.PredictNearestApproachTime(
                    myDirection, myPosition, mySpeed,
                    agent.Direction, agent.Position, agent.Speed);

                if (time >= 0 && time < localMinTime)
                {
                    float distance = PredictionMath.ComputeNearestApproachDistance(
                        time, myPosition, myDirection, mySpeed,
                        agent.Position, agent.Direction, agent.Speed);

                    if (distance < PredictionMath.DefaultCollisionDangerThreshold)
                    {
                        localMinTime = time;
                        currentTarget = agent;
                    }
                }
            }

            // Verify target is also in avoidance area
            if (currentTarget.HasValue && avoidanceAreaAgents != null)
            {
                bool inAvoidanceArea = false;
                foreach (PerceivedAgent areaAgent in avoidanceAreaAgents)
                {
                    if (areaAgent.InstanceId == currentTarget.Value.InstanceId)
                    {
                        inAvoidanceArea = true;
                        break;
                    }
                }
                if (!inAvoidanceArea)
                {
                    currentTarget = null;
                }
            }

            return currentTarget;
        }

        /// <summary>
        /// Finds the potential anticipated collision target (for neighbor avoidance).
        /// Uses group-level shared FOV and parameters when in group with active collider.
        /// </summary>
        private PerceivedAgent? FindPotentialAvoidanceTarget(AgentFrame frame,
            List<PerceivedAgent> agents, SensorInput sensors, GroupContext group)
        {
            List<PerceivedAgent> searchAgents = agents;
            Vector3 searchDir = frame.Direction;
            Vector3 searchPos = frame.Position;
            float searchSpeed = frame.Speed;

            if (group.IsInGroup && group.IsGroupColliderActive)
            {
                if (sensors.SharedFOVAgents != null && sensors.SharedFOVAgents.Count > 0)
                {
                    ResolveAgentsInto(sensors.SharedFOVAgents, group.GroupName, sensors.SelfGameObject, pooledSharedFOVAgents);
                    searchAgents = pooledSharedFOVAgents;
                }

                searchDir = group.GroupFrame.Direction;
                searchPos = group.GroupFrame.Position;
                searchSpeed = group.GroupFrame.Speed;
            }

            PerceivedAgent? currentTarget = null;
            float localMinTime = PredictionMath.DefaultMinTimeToCollision;

            foreach (PerceivedAgent agent in searchAgents)
            {
                float time = PredictionMath.PredictNearestApproachTime(
                    searchDir, searchPos, searchSpeed,
                    agent.Direction, agent.Position, agent.Speed);

                if (time >= 0 && time < localMinTime)
                {
                    float distance = PredictionMath.ComputeNearestApproachDistance(
                        time, searchPos, searchDir, searchSpeed,
                        agent.Position, agent.Direction, agent.Speed);

                    if (distance < PredictionMath.DefaultCollisionDangerThreshold)
                    {
                        localMinTime = time;
                        currentTarget = agent;
                    }
                }
            }

            return currentTarget;
        }

        private float ComputeTagWeight(PerceivedAgent target)
        {
            return PredictionMath.ComputeTagWeight(target);
        }

        /// <summary>
        /// Resolves wall and obstacle normals from NormalVector components.
        /// This is environment perception — GetComponent on environment objects belongs in L1-2.
        /// </summary>
        private void ResolveEnvironment(SensorInput sensors, AgentFrame frame, GroupContext group,
            out Vector3 wallNormal, out bool hasWall,
            out Vector3 closestObstacleNormal, out bool hasObstacle)
        {
            // Resolve wall normal
            wallNormal = Vector3.zero;
            hasWall = false;
            if (sensors.WallTarget != null)
            {
                NormalVector nv = sensors.WallTarget.GetComponent<NormalVector>();
                if (nv != null)
                {
                    wallNormal = nv.CalculateNormalVectorFromWall(frame.Position);
                    hasWall = true;
                }
            }

            // Resolve closest obstacle normal
            closestObstacleNormal = Vector3.zero;
            hasObstacle = false;
            if (sensors.ObstaclesInFOV != null && sensors.ObstaclesInFOV.Count > 0)
            {
                // Determine reference position (agent pos or full group center including self)
                Vector3 referencePos = frame.Position;
                if (group.IsInGroup && group.Members != null && group.Members.Count > 0)
                {
                    Vector3 sum = frame.Position;
                    int count = 1;
                    foreach (GroupMember m in group.Members)
                    {
                        sum += m.Position;
                        count++;
                    }
                    referencePos = sum / count;
                }

                // Find closest obstacle
                GameObject closestObstacle = null;
                float minDist = float.MaxValue;
                foreach (GameObject obs in sensors.ObstaclesInFOV)
                {
                    if (obs == null) continue;
                    float dist = Vector3.Distance(referencePos, obs.transform.position);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closestObstacle = obs;
                    }
                }

                if (closestObstacle != null)
                {
                    NormalVector nv = closestObstacle.GetComponent<NormalVector>();
                    if (nv != null)
                    {
                        closestObstacleNormal = nv.CalculateNormalVectorFromWall(referencePos);
                        hasObstacle = true;
                    }
                }
            }
        }
    }
}
