using UnityEngine;
using MotionMatching;

#if UNITY_EDITOR
namespace CollisionAvoidance
{
    public class AgentPrefabConfig
    {
        public MotionMatchingData MMData;
        public GameObject FOVMeshPrefab;
        public RuntimeAnimatorController AnimatorController;
        public AvatarMaskData AvatarMask;
        public GameObject PhonePrefab;
        public Vector3 PositionOffset;
        public Vector3 RotationOffset;
        public AudioClip[] AudioClips;

        public static AgentPrefabConfig CreateDefault()
        {
            return new AgentPrefabConfig
            {
                PositionOffset = new Vector3(0.1f, -0.03f, 0.03f),
                RotationOffset = new Vector3(0, 0, -20)
            };
        }
    }
}
#endif
