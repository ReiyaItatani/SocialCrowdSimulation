using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// On Play, calls InstantiateAvatars() on the AvatarCreatorQuickGraph.
    /// Uses the existing AgentsList as-is — individuals, groups, speeds, everything.
    /// </summary>
    public class EnvironmentBootstrapper : MonoBehaviour
    {
        private void Start()
        {
            var avatarCreator = GetComponentInChildren<AvatarCreatorQuickGraph>();
            if (avatarCreator == null)
            {
                Debug.LogWarning("[EnvironmentBootstrapper] No AvatarCreatorQuickGraph found.");
                return;
            }

            if (avatarCreator.instantiatedAvatars.Count > 0)
                return;

            avatarCreator.InstantiateAvatars();
            Debug.Log($"[EnvironmentBootstrapper] Spawned {avatarCreator.instantiatedAvatars.Count} agents.");
        }
    }
}
