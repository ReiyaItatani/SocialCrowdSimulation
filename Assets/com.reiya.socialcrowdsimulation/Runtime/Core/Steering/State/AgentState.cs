using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Backward-compatibility state for one agent.
    /// Synced from pipeline outputs (DecisionOutput, MotorOutput, AttentionOutput) each frame
    /// by AgentPathController.OnUpdate(). Read by AgentDebugGizmos for force visualization
    /// and by external code via AgentPathController facade methods.
    /// </summary>
    [System.Serializable]
    public class AgentState
    {
        // --- Motion state ---
        public Vector3 CurrentPosition;
        public Vector3 CurrentDirection;
        public float CurrentSpeed = 1.0f;

        // --- Force vectors (synced from DecisionOutput) ---
        public Vector3 AvoidanceVector;
        public Vector3 ToGoalVector;
        public Vector3 AvoidNeighborsVector;
        public Vector3 GroupForce;
        public Vector3 WallRepForce;
        public Vector3 AvoidObstacleVector;

        // --- Force weights (set by AgentManager via AgentPathController facade) ---
        public float ToGoalWeight = 2.0f;
        public float AvoidanceWeight = 2.0f;
        public float AvoidNeighborWeight = 2.0f;
        public float GroupForceWeight = 0.5f;
        public float WallRepForceWeight = 0.2f;
        public float AvoidObstacleWeight = 1.0f;

        // --- Avoidance targets (synced from AttentionOutput) ---
        public GameObject CurrentAvoidanceTarget;
        public GameObject PotentialAvoidanceTarget;

        // --- Speed management state (synced from MotorOutput) ---
        public bool OnInSlowingArea;

        // --- Group info ---
        public string GroupName;
    }
}
