using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Shared prediction math and utilities used across pipeline layers.
    /// Extracted to eliminate code duplication between layers.
    /// </summary>
    public static class PredictionMath
    {
        // Shared constants for collision prediction (used by L1-2 and L3)
        public const float DefaultMinTimeToCollision = 5.0f;
        public const float DefaultCollisionDangerThreshold = 4.0f;

        /// <summary>
        /// Compute weight multiplier for a perceived agent based on its tag.
        /// Group-tagged agents get a larger weight to account for group collider size.
        /// </summary>
        public static float ComputeTagWeight(PerceivedAgent target)
        {
            if (target.IsGroupTag)
            {
                return target.ColliderRadius + 2f;
            }
            return 1f;
        }

        /// <summary>
        /// Computes the reflection of targetVector about baseVector.
        /// Used for mutual avoidance when agents approach in parallel.
        /// </summary>
        public static Vector3 GetReflectionVector(Vector3 targetVector, Vector3 baseVector)
        {
            targetVector = targetVector.normalized;
            baseVector = baseVector.normalized;
            float cosTheta = Vector3.Dot(targetVector, baseVector);
            return 2 * cosTheta * baseVector - targetVector;
        }

        /// <summary>
        /// Predicts the time of nearest approach between two agents using linear extrapolation.
        /// Returns negative if agents are diverging.
        /// </summary>
        public static float PredictNearestApproachTime(
            Vector3 myDirection, Vector3 myPosition, float mySpeed,
            Vector3 otherDirection, Vector3 otherPosition, float otherSpeed)
        {
            Vector3 relVelocity = otherDirection * otherSpeed - myDirection * mySpeed;
            float relSpeed = relVelocity.magnitude;

            if (relSpeed == 0) return 0;

            Vector3 relTangent = relVelocity / relSpeed;
            Vector3 relPosition = myPosition - otherPosition;
            float projection = Vector3.Dot(relTangent, relPosition);

            return projection / relSpeed;
        }

        /// <summary>
        /// Computes distance between two agents at a predicted time of nearest approach.
        /// </summary>
        public static float ComputeNearestApproachDistance(
            float time, Vector3 myPosition, Vector3 myDirection, float mySpeed,
            Vector3 otherPosition, Vector3 otherDirection, float otherSpeed)
        {
            Vector3 myFinal = myPosition + myDirection * mySpeed * time;
            Vector3 otherFinal = otherPosition + otherDirection * otherSpeed * time;
            return Vector3.Distance(myFinal, otherFinal);
        }

        /// <summary>
        /// Computes the predicted positions of both agents at time of nearest approach.
        /// Returns the distance between them at that time.
        /// </summary>
        public static float ComputeNearestApproachPositions(
            float time, Vector3 myPosition, Vector3 myDirection, float mySpeed,
            Vector3 otherPosition, Vector3 otherDirection, float otherSpeed,
            out Vector3 myPositionAtApproach, out Vector3 otherPositionAtApproach)
        {
            myPositionAtApproach = myPosition + myDirection * mySpeed * time;
            otherPositionAtApproach = otherPosition + otherDirection * otherSpeed * time;
            return Vector3.Distance(myPositionAtApproach, otherPositionAtApproach);
        }
    }
}
