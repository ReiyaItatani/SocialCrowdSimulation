using UnityEngine;
using MotionMatching;
using System;

namespace CollisionAvoidance
{
    public class AnimationUpdateFlow : MonoBehaviour
    {
        [Serializable]
        public enum FullBodyAnimation
        {
            MotionMatching,
            UnityAnimator
        }

        [Header("Settings")]
        public FullBodyAnimation fullBodyAnimation = FullBodyAnimation.MotionMatching;
        public bool useGaze = true;
        public bool useFacialExpression = false;

        [Header("Motion Matching")]
        public MotionMatchingController MotionMatching;

        [Header("Unity Animator")]
        public UnityAnimatorController UnityAnimatorController;

        private MotionMatchingSkinnedMeshRenderer motionMatchingSkinnedMeshRenderer;
        private GazeController gazeController;
        private ConversationalAgentFramework conversationalAgentFramework;


        private FullBodyAnimation currentFullBodyAnimation;

        private void Awake()
        {
            InitDependencies();
            currentFullBodyAnimation = fullBodyAnimation;
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void Update()
        {
            if (fullBodyAnimation != currentFullBodyAnimation)
            {
                UnsubscribeEvents();
                SubscribeEvents();
                currentFullBodyAnimation = fullBodyAnimation;
            }
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void InitDependencies()
        {
            if (motionMatchingSkinnedMeshRenderer == null)
                motionMatchingSkinnedMeshRenderer = GetComponent<MotionMatchingSkinnedMeshRenderer>();
            if (gazeController == null)
                gazeController = GetComponent<GazeController>();
            if (conversationalAgentFramework == null)
                conversationalAgentFramework = GetComponent<ConversationalAgentFramework>();
        }

        private void SubscribeEvents()
        {
            if (fullBodyAnimation == FullBodyAnimation.MotionMatching)
            {
                if (MotionMatching == null || motionMatchingSkinnedMeshRenderer == null) return;
                MotionMatching.OnSkeletonTransformUpdated += motionMatchingSkinnedMeshRenderer.OnSkeletonTransformUpdated;
                if (useGaze && gazeController != null)
                    MotionMatching.OnSkeletonTransformUpdated += gazeController.UpdateGaze;
                if (useFacialExpression && conversationalAgentFramework != null)
                    MotionMatching.OnSkeletonTransformUpdated += conversationalAgentFramework.UpdateOCEAN;
            }
            else if (fullBodyAnimation == FullBodyAnimation.UnityAnimator)
            {
                if (UnityAnimatorController == null) return;

                UnityAnimatorController.UnityAnimatorControllerUpdated += UnityAnimatorController.UpdateAnimation;
                if (useGaze && gazeController != null)
                    UnityAnimatorController.UnityAnimatorControllerUpdated += gazeController.UpdateGaze;
                if (useFacialExpression && conversationalAgentFramework != null)
                    UnityAnimatorController.UnityAnimatorControllerUpdated += conversationalAgentFramework.UpdateOCEAN;
            }
        }

        private void UnsubscribeEvents()
        {
            if (MotionMatching != null)
            {
                if (motionMatchingSkinnedMeshRenderer != null)
                    MotionMatching.OnSkeletonTransformUpdated -= motionMatchingSkinnedMeshRenderer.OnSkeletonTransformUpdated;
                if (useGaze && gazeController != null)
                    MotionMatching.OnSkeletonTransformUpdated -= gazeController.UpdateGaze;
                if (useFacialExpression && conversationalAgentFramework != null)
                    MotionMatching.OnSkeletonTransformUpdated -= conversationalAgentFramework.UpdateOCEAN;
            }

            if (UnityAnimatorController != null)
            {
                UnityAnimatorController.UnsubscribePlayableGraph();
                UnityAnimatorController.UnityAnimatorControllerUpdated -= UnityAnimatorController.UpdateAnimation;

                if (useGaze && gazeController != null)
                    UnityAnimatorController.UnityAnimatorControllerUpdated -= gazeController.UpdateGaze;
                if (useFacialExpression && conversationalAgentFramework != null)
                    UnityAnimatorController.UnityAnimatorControllerUpdated -= conversationalAgentFramework.UpdateOCEAN;

            }
        }
    }
}
