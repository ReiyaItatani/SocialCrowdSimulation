using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Pure visualization component. Draws force vectors as gizmos in the Scene View.
    /// Extracted from AgentPathController.DrawInfo.
    /// </summary>
    public class AgentDebugGizmos : MonoBehaviour
    {
        [Header("Gizmos")]
        public bool ShowAvoidanceForce = true;
        public bool ShowCurrentDirection = true;
        public bool ShowGoalDirection = true;
        public bool ShowAnticipatedCollisionAvoidance = true;
        public bool ShowGroupForce = true;
        public bool ShowWallForce = true;
        public bool ShowAvoidObstacleForce = true;

        private AgentPathController controller;

        private void Awake()
        {
            controller = GetComponent<AgentPathController>();
        }

        private void OnDrawGizmos()
        {
            if (controller == null || controller.agentState == null) return;
            var state = controller.agentState;
            Vector3 pos = controller.GetCurrentPosition();

            if (ShowAvoidanceForce)
            {
                DrawUtils.DrawArrowGizmo(pos, state.AvoidanceVector, 0.55f, Color.blue);
            }

            if (ShowCurrentDirection)
            {
                DrawUtils.DrawArrowGizmo(pos, state.CurrentDirection, 0.55f, Color.red);
            }

            if (ShowGoalDirection)
            {
                DrawUtils.DrawArrowGizmo(pos, state.ToGoalVector, 0.55f, Color.white);
            }

            if (ShowAnticipatedCollisionAvoidance)
            {
                DrawUtils.DrawArrowGizmo(pos, state.AvoidNeighborsVector, 0.55f, Color.green);
            }

            if (ShowGroupForce)
            {
                DrawUtils.DrawArrowGizmo(pos, state.GroupForce, 0.55f, Color.cyan);
            }

            if (ShowWallForce)
            {
                DrawUtils.DrawArrowGizmo(pos, state.WallRepForce, 0.55f, Color.black);
            }

            if (ShowAvoidObstacleForce)
            {
                DrawUtils.DrawArrowGizmo(pos, state.AvoidObstacleVector, 0.55f, Color.yellow);
            }
        }
    }
}
