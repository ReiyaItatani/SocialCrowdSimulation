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
    public class DefaultPredictionLayer : MonoBehaviour, IPredictionLayer, IGazeAwareLayer
    {
        // Pooled list — safe because pipeline runs synchronously per tick.
        private readonly List<PredictedNeighbor> pooledPredictedNeighbors = new List<PredictedNeighbor>();

        // Cached output for ProcessGaze (available after Tick completes)
        private PredictionOutput lastOutput;

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

            lastOutput = new PredictionOutput(pooledPredictedNeighbors, nearestCollisionTime, mostUrgent);
            return lastOutput;
        }

        /// <summary>
        /// Writes gaze toward the predicted collision point (Prediction priority).
        /// Literature: Matthis et al. 2018 — humans look 1-2 seconds ahead.
        /// Only activates when a collision is predicted within 2 seconds.
        /// </summary>
        public void ProcessGaze(GazeState gaze, AgentFrame frame, GroupContext group)
        {
            if (!lastOutput.MostUrgentNeighbor.HasValue) return;

            PredictedNeighbor urgent = lastOutput.MostUrgentNeighbor.Value;
            if (urgent.TimeToApproach > 2f) return;

            Vector3 targetDir = (urgent.PredictedPosition - frame.Position).normalized;
            if (targetDir != Vector3.zero)
            {
                gaze.TrySetTarget(GazePriority.Prediction, targetDir,
                    urgent.PredictedPosition,
                    urgent.Agent.GameObject);
            }
        }
    }
}
