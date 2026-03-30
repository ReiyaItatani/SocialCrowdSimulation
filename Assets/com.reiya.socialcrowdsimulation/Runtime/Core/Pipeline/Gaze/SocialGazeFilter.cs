using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Post-pipeline social gaze filter.
    /// Runs after all pipeline layers have written to GazeState.
    /// Applies social rules:
    ///   - Custom focal points (slowing area)
    ///   - Individual group member gaze (cycling between members)
    ///   - Conversation gaze boost during Talk animation
    ///
    /// Called by AgentPipelineCoordinator after L5 Motor tick.
    /// </summary>
    public class SocialGazeFilter : MonoBehaviour
    {
        [Header("Member Gaze")]
        [SerializeField] private float memberGazeSwitchMinTime = 2.0f;
        [SerializeField] private float memberGazeSwitchMaxTime = 5.0f;
        [SerializeField, Range(0f, 1f), Tooltip("Probability of looking at a group member (vs. looking forward)")]
        private float memberGazeProbability = 0.4f;

        [Header("Talk Gaze")]
        [SerializeField] private float talkGazeSwitchMinTime = 4.0f;
        [SerializeField] private float talkGazeSwitchMaxTime = 8.0f;
        [SerializeField, Range(0f, 1f), Tooltip("Probability of looking at conversation partner during Talk")]
        private float talkGazeProbability = 0.8f;

        [Header("Eye Height")]
        [SerializeField] private float eyeHeightOffset = 1.6f;

        // Custom focal points (set externally, checked when in slowing area)
        private List<GameObject> customFocalPoints;

        // Member gaze cycling state — track by GameObject to survive reordering
        private GameObject currentGazeTarget;
        private float gazeTargetTimer;
        private bool wasTalking;

        /// <summary>
        /// Set the custom focal points list (called once by coordinator during setup).
        /// </summary>
        public void SetCustomFocalPoints(List<GameObject> focalPoints)
        {
            customFocalPoints = focalPoints;
        }

        private void Awake()
        {
            // Stagger initial timer per-agent to avoid synchronized switching
            gazeTargetTimer = Random.Range(0f, memberGazeSwitchMaxTime);
        }

        /// <summary>
        /// Apply social gaze rules after all pipeline layers have written to GazeState.
        /// </summary>
        public void ProcessGaze(GazeState gaze, AgentFrame frame, GroupContext group,
            bool isIndividual, bool isInSlowingArea, float deltaTime = 0f,
            UpperBodyAnimationState animState = UpperBodyAnimationState.Walk)
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

            // 2. Group member gaze (individual members instead of center of mass)
            if (!isIndividual && group.IsInGroup && group.Members != null && group.Members.Count > 0)
            {
                bool isTalking = animState == UpperBodyAnimationState.Talk;

                // Reset timer on Talk state transition so gaze shifts immediately
                if (isTalking != wasTalking)
                {
                    gazeTargetTimer = 0f;
                    wasTalking = isTalking;
                }

                // Update gaze target timer; re-select when expired or target left
                gazeTargetTimer -= deltaTime;
                GroupMember? foundTarget = FindCurrentTarget(group.Members);
                if (gazeTargetTimer <= 0f || (currentGazeTarget != null && foundTarget == null))
                {
                    SelectNewGazeTarget(group.Members, isTalking);
                    foundTarget = FindCurrentTarget(group.Members);
                }

                // currentGazeTarget == null means "look forward" (skip this tick)
                if (foundTarget.HasValue)
                {
                    GroupMember target = foundTarget.Value;
                    Vector3 targetEyePos = target.Position + Vector3.up * eyeHeightOffset;
                    Vector3 myEyePos = frame.Position + Vector3.up * eyeHeightOffset;
                    Vector3 toMember = (targetEyePos - myEyePos).normalized;

                    if (toMember != Vector3.zero)
                    {
                        bool set;
                        if (isTalking)
                        {
                            set = gaze.TrySetTarget(GazePriority.Conversation, toMember, targetEyePos, target.GameObject);
                        }
                        else
                        {
                            set = !gaze.HasExplicitTarget &&
                                  gaze.TrySetTarget(GazePriority.Default, toMember, targetEyePos, target.GameObject);
                        }
                        if (set) gaze.IsGroupMemberTarget = true;
                    }
                }
            }
        }

        private GroupMember? FindCurrentTarget(List<GroupMember> members)
        {
            if (currentGazeTarget == null) return null;
            foreach (GroupMember m in members)
            {
                if (m.GameObject == currentGazeTarget) return m;
            }
            return null;
        }

        private void SelectNewGazeTarget(List<GroupMember> members, bool isTalking)
        {
            float prob = isTalking ? talkGazeProbability : memberGazeProbability;

            if (Random.value < prob)
            {
                // Look at a group member
                if (members.Count == 1)
                {
                    currentGazeTarget = members[0].GameObject;
                }
                else
                {
                    int newIndex;
                    do
                    {
                        newIndex = Random.Range(0, members.Count);
                    } while (members[newIndex].GameObject == currentGazeTarget && members.Count > 1);
                    currentGazeTarget = members[newIndex].GameObject;
                }
            }
            else
            {
                // Look forward (no target)
                currentGazeTarget = null;
            }

            gazeTargetTimer = isTalking
                ? Random.Range(talkGazeSwitchMinTime, talkGazeSwitchMaxTime)
                : Random.Range(memberGazeSwitchMinTime, memberGazeSwitchMaxTime);
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
