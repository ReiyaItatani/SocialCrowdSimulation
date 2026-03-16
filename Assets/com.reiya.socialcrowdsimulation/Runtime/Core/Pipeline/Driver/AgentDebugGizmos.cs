using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Pure visualization component. Draws force vectors as gizmos in the Scene View.
    /// Reads directly from AgentPipelineCoordinator pipeline outputs.
    /// </summary>
    public class AgentDebugGizmos : MonoBehaviour
    {
        [Header("Force Gizmos")]
        public bool ShowAvoidanceForce;
        public bool ShowCurrentDirection;
        public bool ShowGoalDirection;
        public bool ShowAnticipatedCollisionAvoidance;
        public bool ShowGroupForce;
        public bool ShowWallForce;
        public bool ShowAvoidObstacleForce;

        private AgentPipelineCoordinator coordinator;

        private void Awake()
        {
            coordinator = GetComponent<AgentPipelineCoordinator>();
        }

        private void OnDrawGizmos()
        {
            if (coordinator == null) return;
            Vector3 pos = coordinator.GetCurrentPosition();
            DecisionOutput decision = coordinator.LastDecision;

            if (ShowAvoidanceForce)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.AvoidanceForce, 0.55f, Color.blue);
            }

            if (ShowCurrentDirection)
            {
                DrawUtils.DrawArrowGizmo(pos, coordinator.GetCurrentDirection(), 0.55f, Color.red);
            }

            if (ShowGoalDirection)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.ToGoalForce, 0.55f, Color.white);
            }

            if (ShowAnticipatedCollisionAvoidance)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.AnticipatedCollisionForce, 0.55f, Color.green);
            }

            if (ShowGroupForce)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.GroupForce, 0.55f, Color.cyan);
            }

            if (ShowWallForce)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.WallForce, 0.55f, Color.black);
            }

            if (ShowAvoidObstacleForce)
            {
                DrawUtils.DrawArrowGizmo(pos, decision.ObstacleForce, 0.55f, Color.yellow);
            }

        }
    }
}
