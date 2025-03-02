using Drawing;
using UnityEngine;

namespace CollisionAvoidance{
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

        protected virtual void Start(){
            InitForceSolver();
            InitSpeedSolver();
            InitReactSolver();
            InitMotionMathing();
            
            StartUpdateForce();
            StartUpdateSpeed();
        }

        protected override void OnUpdate(){
            base.OnUpdate();
            DrawInfo();
        }

        protected virtual void DrawInfo(){
            Color gizmoColor;
            if(showAvoidanceForce){
                gizmoColor = Color.blue;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidanceVector, 0.55f, gizmoColor);
            }

            if(showCurrentDirection){
                gizmoColor = Color.red;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), CurrentDirection, 0.55f, gizmoColor);
            }
            
            if(showGoalDirection){
                gizmoColor = Color.white;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), toGoalVector, 0.55f, gizmoColor);
            }

            if(showAnticipatedCollisionAvoidance){
                gizmoColor = Color.green;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), avoidNeighborsVector, 0.55f, gizmoColor);
            }

            if(showGroupForce){
                gizmoColor = Color.cyan;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), groupForce, 0.55f, gizmoColor);
            }

            if(showWallForce){
                gizmoColor = Color.black;
                Draw.ArrowheadArc((Vector3)GetCurrentPosition(), wallRepForce, 0.55f, gizmoColor);
            }
        }
    }
}
