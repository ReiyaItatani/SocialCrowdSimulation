using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// L3: Prediction layer.
    /// Computes predicted future positions for perceived agents using linear extrapolation.
    /// Separates prediction math from force computation so prediction algorithms
    /// can be swapped independently (e.g., linear → polynomial → learned model).
    /// </summary>
    public class DefaultPredictionLayer : MonoBehaviour, IPredictionLayer
    {
        // Pooled list — safe because pipeline runs synchronously per tick.
        private readonly List<PredictedNeighbor> pooledPredictedNeighbors = new List<PredictedNeighbor>();

        public PredictionOutput Tick(AttentionOutput attention, AgentFrame frame, GroupContext group)
        {
            pooledPredictedNeighbors.Clear();
            float nearestCollisionTime = PredictionMath.DefaultMinTimeToCollision;
            PredictedNeighbor? mostUrgent = null;

            // Use group-level parameters if in group with active collider
            Vector3 myDirection = frame.Direction;
            Vector3 myPosition = frame.Position;
            float mySpeed = frame.Speed;

            if (group.IsInGroup && group.IsGroupColliderActive)
            {
                myDirection = group.GroupFrame.Direction;
                myPosition = group.GroupFrame.Position;
                mySpeed = group.GroupFrame.Speed;
            }

            List<PerceivedAgent> agents = attention.VisibleAgents;
            if (agents == null)
            {
                return new PredictionOutput(pooledPredictedNeighbors, PredictionMath.DefaultMinTimeToCollision, null);
            }

            foreach (PerceivedAgent agent in agents)
            {
                float time = PredictionMath.PredictNearestApproachTime(
                    myDirection, myPosition, mySpeed,
                    agent.Direction, agent.Position, agent.Speed);

                if (time < 0) continue;

                Vector3 myPosAtApproach;
                Vector3 otherPosAtApproach;
                float distance = PredictionMath.ComputeNearestApproachPositions(
                    time, myPosition, myDirection, mySpeed,
                    agent.Position, agent.Direction, agent.Speed,
                    out myPosAtApproach, out otherPosAtApproach);

                PredictedNeighbor predicted = new PredictedNeighbor(
                    agent, otherPosAtApproach, myPosAtApproach, time, distance);
                pooledPredictedNeighbors.Add(predicted);

                if (time < nearestCollisionTime && distance < PredictionMath.DefaultCollisionDangerThreshold)
                {
                    nearestCollisionTime = time;
                    mostUrgent = predicted;
                }
            }

            return new PredictionOutput(pooledPredictedNeighbors, nearestCollisionTime, mostUrgent);
        }
    }
}
