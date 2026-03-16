using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Post-pipeline social gaze filter.
    /// Runs after all pipeline layers have written to GazeState.
    /// Applies social rules:
    ///   - Custom focal points (slowing area)
    ///   - Group center of mass gaze (when no higher target)
    ///
    /// Called by AgentPipelineCoordinator after L5 Motor tick.
    /// </summary>
    public class SocialGazeFilter : MonoBehaviour
    {
        // Custom focal points (set externally, checked when in slowing area)
        private List<GameObject> customFocalPoints;

        /// <summary>
        /// Set the custom focal points list (called once by coordinator during setup).
        /// </summary>
        public void SetCustomFocalPoints(List<GameObject> focalPoints)
        {
            customFocalPoints = focalPoints;
        }

        /// <summary>
        /// Apply social gaze rules after all pipeline layers have written to GazeState.
        /// </summary>
        public void ProcessGaze(GazeState gaze, AgentFrame frame, GroupContext group,
            bool isIndividual, bool isInSlowingArea)
        {
            // 1. Custom focal point (slowing area only, Default priority)
            if (isInSlowingArea && customFocalPoints != null && customFocalPoints.Count > 0)
            {
                Transform closest = FindClosestFocalPoint(frame.Position);
                if (closest != null)
                {
                    Vector3 dir = (closest.position - frame.Position).normalized;
                    if (dir != Vector3.zero)
                    {
                        gaze.TrySetTarget(GazePriority.Default, dir, closest.position, closest.gameObject);
                    }
                }
            }

            // 2. Group center of mass gaze (Default priority, only when no higher target)
            if (!isIndividual && group.IsInGroup && !gaze.HasExplicitTarget)
            {
                Vector3 toCOM = (group.CenterOfMass - frame.Position).normalized;
                if (toCOM != Vector3.zero)
                {
                    gaze.TrySetTarget(GazePriority.Default, toCOM, group.CenterOfMass);
                }
            }
        }

        private Transform FindClosestFocalPoint(Vector3 position)
        {
            GameObject closest = null;
            float minDist = float.MaxValue;

            foreach (GameObject fp in customFocalPoints)
            {
                if (fp == null) continue;
                float dist = Vector3.Distance(position, fp.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = fp;
                }
            }

            return closest != null ? closest.transform : null;
        }
    }
}
