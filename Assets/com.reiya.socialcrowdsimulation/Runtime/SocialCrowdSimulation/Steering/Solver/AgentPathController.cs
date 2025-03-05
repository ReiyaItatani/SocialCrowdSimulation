using UnityEngine;
using System.Collections;

namespace CollisionAvoidance
{
    public class AgentPathController : ReactSolver
    {
        [Header("Gizmos")]
        private bool showAvoidanceForce = true;
        public bool ShowAvoidanceForce { get => showAvoidanceForce; set => showAvoidanceForce = value; }
        private bool showCurrentDirection = true;
        public bool ShowCurrentDirection { get => showCurrentDirection; set => showCurrentDirection = value; }
        private bool showGoalDirection = true;
        public bool ShowGoalDirection { get => showGoalDirection; set => showGoalDirection = value; }
        private bool showAnticipatedCollisionAvoidance = true;
        public bool ShowAnticipatedCollisionAvoidance { get => showAnticipatedCollisionAvoidance; set => showAnticipatedCollisionAvoidance = value; }
        private bool showGroupForce = true;
        public bool ShowGroupForce { get => showGroupForce; set => showGroupForce = value; }
        private bool showWallForce = true;
        public bool ShowWallForce { get => showWallForce; set => showWallForce = value; }
        private bool showAvoidObstacleForce = true;
        public bool ShowAvoidObstacleForce { get => showAvoidObstacleForce; set => showAvoidObstacleForce = value; }

        protected virtual void Start()
        {
            InitForceSolver();
            InitSpeedSolver();
            StartCoroutine(DelayedStart(1.0f));
            InitMotionMathing();

            StartUpdateForce();
            StartUpdateSpeed();
        }

        // Wait for a certain delay before actually activating collision avoidance
        protected virtual IEnumerator DelayedStart(float delay)
        {
            yield return new WaitForSeconds(delay);
            InitReactSolver();
        }

        // Your update logic goes here
        protected override void OnUpdate()
        {
            base.OnUpdate();
            // Gizmos can't be called here; only in OnDrawGizmos or OnDrawGizmosSelected
        }

        // This method is automatically called by Unity to draw Gizmos in the Scene View
        private void OnDrawGizmos()
        {
            DrawInfo();
        }

        protected virtual void DrawInfo()
        {
            if (showAvoidanceForce)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), avoidanceVector, 0.55f, Color.blue);
            }

            if (showCurrentDirection)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), CurrentDirection, 0.55f, Color.red);
            }

            if (showGoalDirection)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), toGoalVector, 0.55f, Color.white);
            }

            if (showAnticipatedCollisionAvoidance)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), avoidNeighborsVector, 0.55f, Color.green);
            }

            if (showGroupForce)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), groupForce, 0.55f, Color.cyan);
            }

            if (showWallForce)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), wallRepForce, 0.55f, Color.black);
            }

            if (showAvoidObstacleForce)
            {
                DrawUtils.DrawArrowGizmo(GetCurrentPosition(), avoidObstacleVector, 0.55f, Color.yellow);
            }
        }
    }
}
