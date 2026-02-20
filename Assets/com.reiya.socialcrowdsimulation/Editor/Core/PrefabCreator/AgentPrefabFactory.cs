using UnityEngine;
using System.IO;
using MotionMatching;

#if UNITY_EDITOR
using UnityEditor;

namespace CollisionAvoidance
{
    public static class AgentPrefabFactory
    {
        /// <summary>
        /// Validates the humanoid and config before creating a prefab.
        /// Returns null if valid, or a human-readable error string.
        /// </summary>
        public static string Validate(GameObject humanoid, AgentPrefabConfig config)
        {
            if (humanoid == null)
                return "No humanoid GameObject provided.";

            Animator anim = humanoid.GetComponent<Animator>();
            if (anim == null)
                return $"'{humanoid.name}' has no Animator component.";

            if (!anim.isHuman)
                return $"'{humanoid.name}' is not configured as a Humanoid rig. Set the rig type to Humanoid in the model's import settings.";

            if (config.MMData == null)
                return "MotionMatchingData asset is not assigned.";

            if (config.FOVMeshPrefab == null)
                return "FOVMesh prefab is not assigned.";

            if (config.AnimatorController == null)
                return "AnimatorController is not assigned.";

            if (config.AvatarMask == null)
                return "AvatarMaskData is not assigned.";

            if (config.PhonePrefab == null)
                return "PhoneMesh prefab is not assigned.";

            return null;
        }

        /// <summary>
        /// Creates an agent prefab from the given humanoid and config.
        /// Hierarchy:
        ///   Agent (root)
        ///   ├── Avatar (humanoid model + physics + social behavior)
        ///   ├── Pipeline (AgentPathController + Coordinator)
        ///   │   ├── Navigation              L0: AgentPathManager
        ///   │   ├── PerceptionAttention      L1-2: Perception + CollisionAvoidanceController
        ///   │   ├── Prediction               L3: PredictionLayer
        ///   │   ├── Decision                 L4: DecisionLayer
        ///   │   └── Motor                    L5: MotorLayer
        ///   └── Animation (MotionMatchingController)
        /// Returns the saved prefab path on success, or null on failure.
        /// </summary>
        public static string CreatePrefab(GameObject humanoid, AgentPrefabConfig config)
        {
            string error = Validate(humanoid, config);
            if (error != null)
            {
                Debug.LogError(error);
                return null;
            }

            // Clone the humanoid instance to avoid modifying the original.
            GameObject humanoidInstance = Object.Instantiate(humanoid);
            humanoidInstance.transform.position = Vector3.zero;

            // Verify RightHand bone exists.
            Transform handTransform = humanoidInstance.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand);
            if (handTransform == null)
            {
                Debug.LogError($"'{humanoid.name}' has no RightHand bone mapped in its Humanoid rig.");
                Object.DestroyImmediate(humanoidInstance);
                return null;
            }

            // ── Agent root ──
            GameObject agent = new GameObject("Agent");
            agent.tag = "Agent";

            // ── Avatar (humanoid model + physics + social behavior) ──
            humanoidInstance.name = "Avatar";
            humanoidInstance.transform.SetParent(agent.transform);
            humanoidInstance.tag = "Agent";

            // ── Pipeline (driver + coordinator) ──
            GameObject pipelineGO = new GameObject("Pipeline");
            pipelineGO.transform.SetParent(agent.transform);
            AgentPathController pathController = pipelineGO.AddComponent<AgentPathController>();
            pipelineGO.AddComponent<AgentDebugGizmos>();
            // AgentPipelineCoordinator is auto-added by [RequireComponent] on AgentPathController
            AddLayerInfo(pipelineGO,
                "Pipeline",
                "Agent pipeline driver. AgentPathController bridges MotionMatching with the pipeline. " +
                "AgentPipelineCoordinator executes L0-L5 each tick.");

            // ── L0: Navigation ──
            GameObject navigationGO = CreateLayerChild(pipelineGO, "Navigation",
                "L0: Navigation",
                "Goal selection and waypoint management. " +
                "Determines which graph node the agent walks toward. " +
                "Provides goalPosition to the downstream layers.");
            AgentPathManager agentPathManager = navigationGO.AddComponent<AgentPathManager>();

            // ── L1-2: PerceptionAttention ──
            GameObject perceptionGO = CreateLayerChild(pipelineGO, "PerceptionAttention",
                "L1-2: Perception + Attention",
                "Detects neighbors via FOV and avoidance area triggers. " +
                "Pre-resolves all GetComponent calls into PerceivedAgent structs. " +
                "Resolves wall/obstacle normals. Downstream layers receive pure data only.");
            perceptionGO.AddComponent<DefaultPerceptionAttentionLayer>();
            CollisionAvoidanceController collisionAvoidanceController =
                perceptionGO.AddComponent<CollisionAvoidanceController>();

            // ── L3: Prediction ──
            GameObject predictionGO = CreateLayerChild(pipelineGO, "Prediction",
                "L3: Prediction",
                "Computes predicted future positions for perceived agents using linear extrapolation. " +
                "Identifies the most urgent predicted collision. " +
                "Pure math — no GetComponent calls.");
            predictionGO.AddComponent<DefaultPredictionLayer>();

            // ── L4: Decision ──
            GameObject decisionGO = CreateLayerChild(pipelineGO, "Decision",
                "L4: Decision (Social Force Model)",
                "Computes 6 weighted forces: ToGoal, Avoidance, AnticipatedCollision, " +
                "Group (cohesion/repulsion/alignment), Wall, Obstacle. " +
                "Combines them into a single desired direction. Pure math.");
            decisionGO.AddComponent<DefaultDecisionLayer>();

            // ── L5: Motor ──
            GameObject motorGO = CreateLayerChild(pipelineGO, "Motor",
                "L5: Motor Constraints",
                "Speed management: max speed, acceleration, goal slowing, group speed sync. " +
                "Computes final nextPosition from desired direction and constrained speed.");
            motorGO.AddComponent<DefaultMotorLayer>();

            // ── Animation ──
            GameObject animationGO = new GameObject("Animation");
            animationGO.transform.SetParent(agent.transform);
            MotionMatchingController motionMatchingController =
                animationGO.AddComponent<MotionMatchingController>();
            AddLayerInfo(animationGO,
                "Animation",
                "MotionMatching animation system. Samples motion database based on " +
                "pipeline output (position, direction, speed) to produce character animation.");

            // ── Wire cross-references ──

            // Pipeline ↔ Animation
            pathController.MotionMatching = motionMatchingController;
            pathController.collisionAvoidance = collisionAvoidanceController;
            pathController.agentPathManager = agentPathManager;

            // Navigation
            agentPathManager.pathController = pathController;

            // MotionMatching
            motionMatchingController.CharacterController = pathController;
            motionMatchingController.MMData = config.MMData;
            motionMatchingController.SearchTime = 0.01f;

            // ── Avatar setup ──

            // Instantiate phone prefab under right hand bone.
            GameObject phoneInstance = Object.Instantiate(
                config.PhonePrefab,
                handTransform.position + config.PositionOffset,
                handTransform.rotation
            );
            phoneInstance.transform.localEulerAngles = config.RotationOffset;
            phoneInstance.transform.SetParent(handTransform);

            // Configure Animator.
            Animator humanoidAnimator = humanoidInstance.GetComponent<Animator>();
            humanoidAnimator.runtimeAnimatorController = config.AnimatorController;
            humanoidAnimator.applyRootMotion = false;

            // Add Rigidbody.
            Rigidbody rigidBody = humanoidInstance.AddComponent<Rigidbody>();
            rigidBody.mass = 60f;
            rigidBody.useGravity = false;

            // Add CapsuleCollider.
            CapsuleCollider capsuleCollider = humanoidInstance.AddComponent<CapsuleCollider>();
            capsuleCollider.isTrigger = true;
            capsuleCollider.center = new Vector3(0, 0.9f, 0);
            capsuleCollider.radius = 0.3f;
            capsuleCollider.height = 1.8f;

            // Add ParameterManager.
            ParameterManager parameterManager = humanoidInstance.AddComponent<ParameterManager>();
            parameterManager.pathController = pathController;

            // Create Sound child with AudioSource.
            GameObject soundObject = new GameObject("Sound");
            soundObject.transform.localPosition = Vector3.zero;
            soundObject.transform.SetParent(humanoidInstance.transform);
            AudioSource audioSource = soundObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Add SocialBehaviour.
            SocialBehaviour socialBehaviour = humanoidInstance.AddComponent<SocialBehaviour>();
            socialBehaviour.smartPhone = phoneInstance;
            socialBehaviour.audioSource = audioSource;
            socialBehaviour.audioClips = config.AudioClips;

            // Add AgentCollisionDetection.
            AgentCollisionDetection agentCollisionDetection = humanoidInstance.AddComponent<AgentCollisionDetection>();

            // Add GazeController.
            humanoidInstance.AddComponent<GazeController>();

            // Add MotionMatchingSkinnedMeshRenderer.
            CollisionAvoidance.MotionMatchingSkinnedMeshRenderer motionMatchingSkinnedMeshRenderer =
                humanoidInstance.AddComponent<CollisionAvoidance.MotionMatchingSkinnedMeshRenderer>();
            motionMatchingSkinnedMeshRenderer.MotionMatching = motionMatchingController;
            motionMatchingSkinnedMeshRenderer.AvatarMask = config.AvatarMask;
            motionMatchingSkinnedMeshRenderer.AvoidToesFloorPenetration = true;
            motionMatchingSkinnedMeshRenderer.ToesSoleOffset = new Vector3(0, 0, -0.02f);

            // Add AnimationModifier and RightHandRotModifier.
            humanoidInstance.AddComponent<AnimationModifier>();
            humanoidInstance.AddComponent<RightHandRotModifier>();

            // Wire CollisionAvoidanceController references.
            collisionAvoidanceController.pathController = pathController;
            collisionAvoidanceController.FOVMeshPrefab = config.FOVMeshPrefab;
            collisionAvoidanceController.socialBehaviour = socialBehaviour;
            collisionAvoidanceController.agentCollisionDetection = agentCollisionDetection;
            collisionAvoidanceController.agentCollider = capsuleCollider;

            // ── Save as prefab ──

            string resourcesPath = "Assets/Resources";
            string agentPath = Path.Combine(resourcesPath, humanoid.name);
            if (!Directory.Exists(agentPath))
            {
                Directory.CreateDirectory(agentPath);
            }

            string prefabPath = Path.Combine(agentPath, agent.name + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(agent, prefabPath);

            // Clean up temporary scene objects.
            Object.DestroyImmediate(agent);

            Debug.Log($"Prefab created at: {prefabPath}");
            return prefabPath;
        }

        /// <summary>
        /// Creates a child GameObject with a LayerInfo component for Inspector documentation.
        /// </summary>
        private static GameObject CreateLayerChild(GameObject parent, string name,
            string layerName, string description)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform);
            AddLayerInfo(child, layerName, description);
            return child;
        }

        /// <summary>
        /// Adds a LayerInfo component with the given name and description.
        /// </summary>
        private static void AddLayerInfo(GameObject go, string layerName, string description)
        {
            LayerInfo info = go.AddComponent<LayerInfo>();
            info.layerName = layerName;
            info.description = description;
        }
    }
}
#endif
