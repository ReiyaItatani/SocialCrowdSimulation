using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// L4: Decision layer — Social Force Model implementation.
    /// Computes 6 weighted forces and combines them into a desired direction.
    /// This is where the "sliders" (force weights) are applied.
    ///
    /// Pure computation: receives DecisionInput + AgentFrame + GroupContext.
    /// No GetComponent calls. No concrete Unity component references.
    /// To replace the force model (e.g., with ORCA), implement IDecisionLayer.
    /// </summary>
    public class DefaultDecisionLayer : MonoBehaviour, IDecisionLayer
    {
        // Group force sub-weights
        private float cohesionWeight = 2.0f;
        private float repulsionForceWeight = 1.5f;
        private float alignmentForceWeight = 1.5f;

        // ToGoal uses a raw timer (no TimedForce) because it snaps instantly
        // to the new direction — no smooth transition needed.
        private float toGoalTimer;
        private Vector3 cachedToGoalForce;
        // Other forces use TimedForce for timer + transition encapsulation
        private TimedForce avoidanceForce = new TimedForce(0.1f, 0.3f, TransitionMode.Lerp);
        private TimedForce anticipatedForce = new TimedForce(0.1f, 0.3f, TransitionMode.Slerp);
        private TimedForce groupForce = new TimedForce(0.1f, 0.1f, TransitionMode.Slerp);
        private TimedForce wallForce = new TimedForce(0.2f, 0.5f, TransitionMode.Slerp);
        private TimedForce obstacleForce = new TimedForce(0.1f, 0.3f, TransitionMode.Slerp);

        // Mutual avoidance detection state
        private bool mutualAvoidanceDetected;
        private GameObject mutualAvoidanceTarget;

        public DecisionOutput Tick(DecisionInput input, AgentFrame frame, ForceWeights weights,
            GroupContext group, float deltaTime)
        {
            float dt = deltaTime;

            // Update each force on its own timer
            UpdateToGoalForce(dt, frame.Position, input.GoalPosition);
            UpdateAvoidanceForce(dt, input, frame);
            UpdateAnticipatedCollisionForce(dt, input, frame, group);
            UpdateGroupForce(dt, frame, group, input.AgentColliderRadius);
            UpdateWallForce(dt, input);
            UpdateObstacleForce(dt, input);

            // Weighted combination
            Vector3 combinedDirection = (
                weights.ToGoal * cachedToGoalForce +
                weights.Avoidance * avoidanceForce.Current +
                weights.AnticipatedCollision * anticipatedForce.Current +
                weights.Group * groupForce.Current +
                weights.Wall * wallForce.Current +
                weights.Obstacle * obstacleForce.Current
            ).normalized;

            combinedDirection = new Vector3(combinedDirection.x, 0f, combinedDirection.z);

            return new DecisionOutput(
                combinedDirection,
                frame.Speed,
                mutualAvoidanceDetected,
                mutualAvoidanceTarget,
                cachedToGoalForce,
                avoidanceForce.Current,
                anticipatedForce.Current,
                groupForce.Current,
                wallForce.Current,
                obstacleForce.Current
            );
        }

        #region ToGoal Force

        private void UpdateToGoalForce(float dt, Vector3 currentPosition, Vector3 goalPosition)
        {
            toGoalTimer += dt;
            if (toGoalTimer < 0.1f) return;
            toGoalTimer = 0f;

            cachedToGoalForce = (goalPosition - currentPosition).normalized;
        }

        #endregion

        #region Avoidance Force

        private void UpdateAvoidanceForce(float dt, DecisionInput input, AgentFrame frame)
        {
            if (!avoidanceForce.ShouldUpdate(dt)) return;

            mutualAvoidanceDetected = false;
            mutualAvoidanceTarget = null;

            if (input.UrgentAvoidanceTarget.HasValue)
            {
                PerceivedAgent target = input.UrgentAvoidanceTarget.Value;

                Vector3 avoidanceVector = ComputeAvoidanceVector(
                    target.Position, frame.Direction, frame.Position);

                // Check opponent direction for mutual avoidance
                if (target.AvoidanceVector != Vector3.zero &&
                    Vector3.Dot(frame.Direction, avoidanceVector) < 0.5f)
                {
                    bool isParallel;
                    avoidanceVector = CheckOpponentDir(
                        avoidanceVector, frame.Position,
                        target.AvoidanceVector, target.Position,
                        out isParallel);
                    if (isParallel)
                    {
                        mutualAvoidanceDetected = true;
                        mutualAvoidanceTarget = target.GameObject;
                    }
                }

                // Scale by distance using pre-resolved collider data
                float maxDist = Mathf.Sqrt(
                    input.AvoidanceColliderSize.x / 2 * input.AvoidanceColliderSize.x / 2 +
                    input.AvoidanceColliderSize.z * input.AvoidanceColliderSize.z) +
                    input.AgentColliderRadius * 2;
                float dist = Vector3.Distance(target.Position, frame.Position);
                avoidanceVector *= (1.0f - dist / maxDist);

                // Apply tag weight
                avoidanceVector *= input.UrgentTargetWeight;

                avoidanceForce.SetImmediate(avoidanceVector);
            }
            else
            {
                // Fade out to zero when no target
                if (avoidanceForce.Current != Vector3.zero)
                {
                    avoidanceForce.SetTarget(Vector3.zero);
                }
            }
        }

        private Vector3 ComputeAvoidanceVector(Vector3 targetPosition, Vector3 currentDirection, Vector3 currentPosition)
        {
            Vector3 directionToTarget = (targetPosition - currentPosition).normalized;
            Vector3 upVector;

            if (Vector3.Dot(directionToTarget, currentDirection) >= 0.9748f)
            {
                upVector = Vector3.up;
            }
            else
            {
                upVector = Vector3.Cross(directionToTarget, currentDirection);
            }
            return Vector3.Cross(upVector, directionToTarget).normalized;
        }

        private Vector3 CheckOpponentDir(Vector3 myDirection, Vector3 myPosition,
            Vector3 otherDirection, Vector3 otherPosition, out bool isParallel)
        {
            Vector3 offset = (otherPosition - myPosition).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, offset).normalized;
            if ((Vector3.Dot(right, myDirection) > 0 && Vector3.Dot(right, otherDirection) > 0) ||
                (Vector3.Dot(right, myDirection) < 0 && Vector3.Dot(right, otherDirection) < 0))
            {
                isParallel = true;
                return PredictionMath.GetReflectionVector(myDirection, offset);
            }
            isParallel = false;
            return myDirection;
        }

        #endregion

        #region Anticipated Collision Avoidance Force

        private void UpdateAnticipatedCollisionForce(float dt, DecisionInput input,
            AgentFrame frame, GroupContext group)
        {
            if (!anticipatedForce.ShouldUpdate(dt)) return;

            if (input.UrgentAvoidanceTarget.HasValue)
            {
                anticipatedForce.SetImmediate(Vector3.zero);
                return;
            }

            Vector3 newForce;

            if (group.IsInGroup && group.IsGroupColliderActive)
            {
                newForce = ComputeAnticipatedForce(
                    group.GroupFrame.Direction,
                    group.GroupFrame.Position,
                    group.GroupFrame.Speed,
                    input.Prediction, frame.Position);
            }
            else
            {
                newForce = ComputeAnticipatedForce(
                    frame.Direction, frame.Position, frame.Speed,
                    input.Prediction, frame.Position);
            }

            if (input.PotentialAvoidanceTarget.HasValue)
            {
                newForce *= PredictionMath.ComputeTagWeight(input.PotentialAvoidanceTarget.Value);
            }

            anticipatedForce.SetTarget(newForce);
        }

        private Vector3 ComputeAnticipatedForce(Vector3 myDirection, Vector3 myPosition, float mySpeed,
            PredictionOutput prediction, Vector3 currentPosition)
        {
            if (!prediction.MostUrgentNeighbor.HasValue) return Vector3.zero;

            PredictedNeighbor target = prediction.MostUrgentNeighbor.Value;
            PerceivedAgent agent = target.Agent;

            float steer = 0f;
            float parallelness = Vector3.Dot(myDirection, agent.Direction);
            float angle = 0.707f;

            if (parallelness < -angle)
            {
                Vector3 offset = target.PredictedPosition - myPosition;
                Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                float sideDot = Vector3.Dot(offset, rightVector);
                steer = (sideDot > 0) ? -1.0f : 1.0f;
            }
            else if (parallelness > angle)
            {
                Vector3 offset = agent.Position - myPosition;
                Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                float sideDot = Vector3.Dot(offset, rightVector);
                steer = (sideDot > 0) ? -1.0f : 1.0f;
            }
            else
            {
                if (mySpeed <= agent.Speed)
                {
                    Vector3 rightVector = Vector3.Cross(myDirection, Vector3.up);
                    float sideDot = Vector3.Dot(rightVector, agent.Direction);
                    steer = (sideDot > 0) ? -1.0f : 1.0f;
                }
            }

            return Vector3.Cross(myDirection, Vector3.up) * steer;
        }

        #endregion

        #region Group Force

        private void UpdateGroupForce(float dt, AgentFrame frame, GroupContext group, float agentRadius)
        {
            if (!group.IsInGroup)
            {
                groupForce.SetImmediate(Vector3.zero);
                return;
            }

            if (!groupForce.ShouldUpdate(dt)) return;

            if (group.Members == null || group.Members.Count == 0)
            {
                groupForce.SetImmediate(Vector3.zero);
                return;
            }

            Vector3 cohesion = CalculateCohesionForce(group, cohesionWeight, frame.Position);
            Vector3 repulsion = CalculateRepulsionForce(group.Members, repulsionForceWeight, frame.Position, agentRadius);
            Vector3 alignment = CalculateAlignment(group.Members, alignmentForceWeight);
            Vector3 newGroupForce = (cohesion + repulsion + alignment).normalized;

            groupForce.SetTarget(newGroupForce);
        }

        private Vector3 CalculateCohesionForce(GroupContext group, float weight, Vector3 currentPos)
        {
            float safetyDistance = 0.05f;
            float threshold = group.Members.Count * 0.3f + safetyDistance;
            Vector3 centerOfMass = group.CenterOfMass;
            float dist = Vector3.Distance(currentPos, centerOfMass);
            float judgeWithinThreshold = dist > threshold ? 1f : 0f;
            Vector3 toCenterOfMassDir = (centerOfMass - currentPos).normalized;
            return judgeWithinThreshold * weight * toCenterOfMassDir;
        }

        private Vector3 CalculateRepulsionForce(List<GroupMember> members, float weight, Vector3 currentPos, float agentRadius)
        {
            Vector3 repulsionForceDir = Vector3.zero;
            float minDist = 2 * agentRadius + 0.05f;

            foreach (GroupMember member in members)
            {
                Vector3 offset = member.Position - currentPos;
                float sqrDist = offset.sqrMagnitude;

                if (sqrDist < minDist * minDist)
                {
                    float invDist = 1.0f / Mathf.Sqrt(sqrDist);
                    Vector3 dir = offset.normalized;
                    repulsionForceDir += weight * invDist * dir;
                }
            }

            return -repulsionForceDir;
        }

        private Vector3 CalculateAlignment(List<GroupMember> members, float weight)
        {
            Vector3 steering = Vector3.zero;

            foreach (GroupMember member in members)
            {
                steering += member.Direction;
            }

            return steering * weight;
        }

        #endregion

        #region Wall Force

        private void UpdateWallForce(float dt, DecisionInput input)
        {
            if (!wallForce.ShouldUpdate(dt)) return;

            if (input.HasWall)
            {
                wallForce.SetImmediate(input.WallNormal);
            }
            else
            {
                wallForce.SetTarget(Vector3.zero);
            }
        }

        #endregion

        #region Obstacle Force

        private void UpdateObstacleForce(float dt, DecisionInput input)
        {
            if (!obstacleForce.ShouldUpdate(dt)) return;

            Vector3 newObstacleForce = input.HasObstacle
                ? input.ClosestObstacleNormal.normalized
                : Vector3.zero;

            obstacleForce.SetTarget(newObstacleForce);
        }

        #endregion
    }
}
