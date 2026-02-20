using UnityEngine;
using MotionMatching;

#if UNITY_EDITOR
using UnityEditor;

namespace CollisionAvoidance
{
    public static class DefaultAssetLocator
    {
        private const string KnownBasePath =
            "Assets/com.reiya.socialcrowdsimulation/Sample/QuickStart/ForPrefabCreator/";

        private static readonly string[] AudioClipNames =
        {
            "Excuse me.wav",
            "I'm sorry.wav",
            "Are you okay.wav"
        };

        public static AgentPrefabConfig LoadDefaults()
        {
            AgentPrefabConfig config = AgentPrefabConfig.CreateDefault();

            string folder = ResolveForPrefabCreatorFolder();
            if (string.IsNullOrEmpty(folder))
            {
                Debug.LogWarning("[AutoSetup] Could not locate ForPrefabCreator folder. Use Advanced Settings to assign assets manually.");
                return config;
            }

            // Ensure trailing slash.
            if (!folder.EndsWith("/"))
                folder += "/";

            config.MMData = AssetDatabase.LoadAssetAtPath<MotionMatchingData>(
                folder + "MotionMatchingData.asset");

            config.AvatarMask = AssetDatabase.LoadAssetAtPath<AvatarMaskData>(
                folder + "LowerBody_AvatarMaskData.asset");

            config.AnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                folder + "CollisionAvoidanceAnimatorController.controller");

            config.FOVMeshPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                folder + "FOVMesh.prefab");

            config.PhonePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                folder + "PhoneMesh(New).prefab");

            // Load audio clips.
            string soundsFolder = folder + "Sounds/";
            AudioClip[] clips = new AudioClip[AudioClipNames.Length];
            for (int i = 0; i < AudioClipNames.Length; i++)
            {
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(
                    soundsFolder + AudioClipNames[i]);
            }
            config.AudioClips = clips;

            return config;
        }

        private static string ResolveForPrefabCreatorFolder()
        {
            // Primary: try the known embedded path.
            Object test = AssetDatabase.LoadAssetAtPath<Object>(
                KnownBasePath + "MotionMatchingData.asset");
            if (test != null)
                return KnownBasePath;

            // Fallback: search by type to handle UPM or moved packages.
            string[] guids = AssetDatabase.FindAssets("t:MotionMatchingData MotionMatchingData");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("ForPrefabCreator"))
                {
                    // Return the containing folder.
                    int idx = path.LastIndexOf('/');
                    return idx >= 0 ? path.Substring(0, idx) : null;
                }
            }

            return null;
        }
    }
}
#endif
