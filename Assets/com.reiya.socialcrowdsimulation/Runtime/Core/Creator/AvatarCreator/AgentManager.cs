using System.Collections.Generic;
using UnityEngine;
using MotionMatching;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;

namespace CollisionAvoidance{
// AgentManager is a class that manages various parameters and settings for agents in a simulation.

public class AgentManager : MonoBehaviour
{
    [Header("Goal Parameters")]
    public GoalParameters goalParameters;
    [System.Serializable]
    public class GoalParameters{
    [Tooltip("Radius to consider as the goal.")][Range(0.1f, 5.0f)]public float goalRadius = 2.0f;
    [Tooltip("Radius to start slowing down.")][Range(0.1f, 5.0f)]public float slowingRadius = 3.0f;
    }

    // Weights for various forces influencing agent movement.
    [Space]
    [Header("  ==MOVEMENT==  ")]
    [Header("Weights for Social Forces")]
    public ForceWeights forceWeights;
    [System.Serializable]
    public class ForceWeights{
        [Range(0.0f, 5.0f)]public float toGoalWeight = 1.5f;
        [Range(0.0f, 5.0f)]public float avoidNeighborWeight = 0.5f;
        [Range(0.0f, 5.0f)]public float avoidanceWeight = 2.3f;
        [Range(0.0f, 5.0f)]public float groupForceWeight = 0.5f;
        [Range(0.0f, 5.0f)]public float wallRepForceWeight = 0.3f;
        [Range(0.0f, 5.0f)]public float avoidObstacleWeight = 1.0f;
    }

    // Parameters related to the adjustment of the position of the SimulationBone and SimulationObject.
    [Header("Motion Matching Parameters")]
    public MMParameters mmParameters;
    [System.Serializable]
    public class MMParameters{
        [Range(0.0f, 2.0f), Tooltip("Max distance between SimulationBone and SimulationObject")] 
        public float MaxDistanceMMAndCharacterController = 0.1f;
        [Range(0.0f, 2.0f), Tooltip("Time needed to move half of the distance between SimulationBone and SimulationObject")] 
        public float PositionAdjustmentHalflife = 0.1f;
        [Range(0.0f, 2.0f), Tooltip("Ratio between the adjustment and the character's velocity to clamp the adjustment")] 
        public float PosMaximumAdjustmentRatio = 0.1f;
    }

    // Parameters to control the display of various debug gizmos in the Unity Editor.
    [Header("Path Controller Debug")]
    public GizmosPathController gizmosPC;
    [System.Serializable]
    public class GizmosPathController
    {
        public bool ShowAvoidanceForce = false;
        public bool ShowAnticipatedCollisionAvoidance = false;
        public bool ShowGoalDirection = false;
        public bool ShowCurrentDirection = false;
        public bool ShowGroupForce = false;
        public bool ShowWallForce = false;
        public bool ShowObstacleAvoidanceForce = false;
    }

    // Parameters for debugging the Motion Matching Controller.
    [Header("Motion Matching Controller Debug")]
    public GizmosMotionMatching gizmosMM;
    [System.Serializable]
    public class GizmosMotionMatching
    {
        public bool DebugSkeleton = false;
        public bool DebugCurrent = false;
        public bool DebugPose = false;
        public bool DebugTrajectory = false;
        public bool DebugContacts = false;
    }

    // Lists to store references to various controller game objects.
    private List<GameObject> PathControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingControllers = new List<GameObject>();
    private List<GameObject> CollisionAvoidanceControllers = new List<GameObject>();
    private List<GameObject> Avatars = new List<GameObject>();
    private AvatarCreatorQuickGraph avatarCreator;

    // Awake is called when the script instance is being loaded.
    protected virtual void Awake(){
        // Get a reference to the AvatarCreatorBase component.
        avatarCreator = this.GetComponent<AvatarCreatorQuickGraph>();
        // Get a list of instantiated avatars from the AvatarCreatorBase.
        Avatars = avatarCreator.instantiatedAvatars; 
    }

    // Start is called before the first frame update.
    protected virtual void Start()
    {
        // Loop through all avatars and set their parameters.
        for (int i = 0; i < Avatars.Count; i++)
        {
            // Get and set PathController parameters.
            AgentPathController pathController = Avatars[i].GetComponentInChildren<AgentPathController>();
            if(pathController != null) {
                SetPathControllerParams(pathController);
                PathControllers.Add(pathController.gameObject);
            }

            // Get and set MotionMatchingController parameters.
            MotionMatchingController motionMatchingController = Avatars[i].GetComponentInChildren<MotionMatchingController>();
            if(motionMatchingController != null) {
                SetMotionMatchingControllerParams(motionMatchingController);
                MotionMatchingControllers.Add(motionMatchingController.gameObject);
            }

            // Get and set CollisionAvoidance parameters.
            CollisionAvoidanceController collisionAvoidanceController = Avatars[i].GetComponentInChildren<CollisionAvoidanceController>();
            if(collisionAvoidanceController != null) {
                // SetCollisionAvoidanceControllerParams(collisionAvoidanceController);
                CollisionAvoidanceControllers.Add(collisionAvoidanceController.gameObject);
            }

            // Get and set SocialBehaviour parameters.
            SocialBehaviour socialBehaviour = Avatars[i].GetComponentInChildren<SocialBehaviour>();
            if(socialBehaviour != null) {
                SetSocialBehaviourParams(socialBehaviour);
            }     
        }
    }

    // OnValidate is called when the script is loaded or a value is changed in the Inspector.
    protected virtual void OnValidate() {
        // Loop through all PathControllers and set their parameters.
        foreach(GameObject controllerObject in PathControllers) 
        {
            AgentPathController pathController = controllerObject.GetComponent<AgentPathController>();
            if(pathController != null) 
            {
                SetPathControllerParams(pathController);
            }
            AgentPathManager pathManager = controllerObject.GetComponentInChildren<AgentPathManager>();
            if(pathController != null) 
            {
                SetPathManagerrParams(pathManager);
            }
        }

        // Loop through all MotionMatchingControllers and set their parameters.
        foreach(GameObject controllerObject in MotionMatchingControllers) 
        {
            MotionMatchingController motionMatchingController = controllerObject.GetComponent<MotionMatchingController>();
            if(motionMatchingController != null) 
            {
                SetMotionMatchingControllerParams(motionMatchingController);
            }
        }

        // Loop through all CollisionAvoidanceControllers and set their parameters.
        // foreach(GameObject controllerObject in CollisionAvoidanceControllers) 
        // {
        //     CollisionAvoidanceController collisionAvoidanceController = controllerObject.GetComponent<CollisionAvoidanceController>();
        //     if(collisionAvoidanceController != null) 
        //     {
        //         SetCollisionAvoidanceControllerParams(collisionAvoidanceController);
        //     }
        // }

    }

    // Method to set parameters for PathController.
    protected virtual void SetPathControllerParams(AgentPathController pathController){
        pathController.slowingRadius = goalParameters.slowingRadius;

        pathController.toGoalWeight               = forceWeights.toGoalWeight;
        pathController.avoidanceWeight            = forceWeights.avoidanceWeight;
        pathController.avoidNeighborWeight        = forceWeights.avoidNeighborWeight;
        pathController.groupForceWeight           = forceWeights.groupForceWeight;
        pathController.wallRepForceWeight         = forceWeights.wallRepForceWeight;
        pathController.avoidObstacleWeight        = forceWeights.avoidObstacleWeight;

        pathController.MaxDistanceMMAndCharacterController = mmParameters.MaxDistanceMMAndCharacterController;
        pathController.PositionAdjustmentHalflife          = mmParameters.PositionAdjustmentHalflife;
        pathController.PosMaximumAdjustmentRatio           = mmParameters.PosMaximumAdjustmentRatio;

        pathController.ShowAvoidanceForce                = gizmosPC.ShowAvoidanceForce;
        pathController.ShowAnticipatedCollisionAvoidance = gizmosPC.ShowAnticipatedCollisionAvoidance;
        pathController.ShowGoalDirection                 = gizmosPC.ShowGoalDirection;
        pathController.ShowCurrentDirection              = gizmosPC.ShowCurrentDirection;
        pathController.ShowGroupForce                    = gizmosPC.ShowGroupForce;
        pathController.ShowWallForce                     = gizmosPC.ShowWallForce;
        pathController.ShowAvoidObstacleForce            = gizmosPC.ShowObstacleAvoidanceForce;
    }

    protected virtual void SetPathManagerrParams(AgentPathManager pathManager){
        pathManager.goalRadius = goalParameters.goalRadius;
    }

    protected virtual void SetMotionMatchingControllerParams(MotionMatchingController motionMatchingController){
        // motionMatchingController.SpheresRadius = SphereRadius;
        motionMatchingController.DebugSkeleton   = gizmosMM.DebugSkeleton;
        motionMatchingController.DebugCurrent    = gizmosMM.DebugCurrent;
        motionMatchingController.DebugPose       = gizmosMM.DebugPose;
        motionMatchingController.DebugTrajectory = gizmosMM.DebugTrajectory;
        motionMatchingController.DebugContacts   = gizmosMM.DebugContacts;
    }

    // private void SetCollisionAvoidanceControllerParams(CollisionAvoidanceController collisionAvoidanceController){
    //     collisionAvoidanceController.agentCollider.radius           = CapsuleColliderRadius;
    // }

    protected virtual void SetSocialBehaviourParams(SocialBehaviour socialBehaviour){

    }

#if UNITY_EDITOR
    public virtual void SaveToFile(string path)
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, json);
    }

    public virtual void LoadFromFile(string path)
    {
        string json = File.ReadAllText(path);
        JsonUtility.FromJsonOverwrite(json, this);
    }
    
    public virtual void SaveSettings()
    {
        string path = EditorUtility.SaveFilePanel("Save Agent Settings", "", "AgentSettings", "json");
        if (!string.IsNullOrEmpty(path))
        {
            SaveToFile(path);
        }
    }

    public virtual void LoadSettings()
    {
        string path = EditorUtility.OpenFilePanel("Load Agent Settings", "", "json");
        if (!string.IsNullOrEmpty(path))
        {
            LoadFromFile(path);
        }
    }
#endif

}
}