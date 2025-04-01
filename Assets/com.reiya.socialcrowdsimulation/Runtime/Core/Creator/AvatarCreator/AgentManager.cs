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

    // OCEAN Personality Model Parameters: Parameters that define the personality of the agent according to the OCEAN model.
    [Space]
    [Header("  ==FACIAL EXPRESSION==  ")]
    //Facial Expression might be expensive.
    public bool useFacialExpression = false;
    [Header("Conversational Agent Framework Parameters")]
    public OCEAN ocean;
    [System.Serializable]
    public class OCEAN
    {
        [Range(-1f, 1f)]public float openness = 0f;
        [Range(-1f, 1f)]public float conscientiousness = 0f;
        [Range(-1f, 1f)]public float extraversion = 0f;
        [Range(-1f, 1f)]public float agreeableness = 0f;
        [Range(-1f, 1f)]public float neuroticism = 0f;
    }

    // Emotion Parameters: Parameters that define the emotional state of the agent.
    [Header("Emotion Parameters")]
    public Emotion emotion;

    [System.Serializable]
    public class Emotion
    {
        [Range(0f, 1f)]public float happy = 0f;
        [Range(0f, 1f)]public float sad = 0f;
        [Range(0f, 1f)]public float angry = 0f;
        [Range(0f, 1f)]public float disgust = 0f;
        [Range(0f, 1f)]public float fear = 0f;
        [Range(0f, 1f)]public float shock = 0f;
    }

    // Lists to store references to various controller game objects.
    private List<GameObject> PathControllers = new List<GameObject>();
    private List<GameObject> MotionMatchingControllers = new List<GameObject>();
    private List<GameObject> CollisionAvoidanceControllers = new List<GameObject>();
    private List<GameObject> ConversationalAgentFrameworks = new List<GameObject>();
    private List<GameObject> Avatars = new List<GameObject>();
    private AvatarCreatorQuickGraph avatarCreator;

    // Awake is called when the script instance is being loaded.
    void Awake(){
        // Get a reference to the AvatarCreatorBase component.
        avatarCreator = this.GetComponent<AvatarCreatorQuickGraph>();
        // Get a list of instantiated avatars from the AvatarCreatorBase.
        Avatars = avatarCreator.instantiatedAvatars; 
    }

    // Start is called before the first frame update.
    void Start()
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

            // Get and set ConversationalAgentFramework parameters.
            ConversationalAgentFramework conversationalAgentFramework = Avatars[i].GetComponentInChildren<ConversationalAgentFramework>();
            if(conversationalAgentFramework != null) {
                SetConversationalAgentFrameworkParams(conversationalAgentFramework);
                ConversationalAgentFrameworks.Add(conversationalAgentFramework.gameObject);
            }

            // Get and set SocialBehaviour parameters.
            SocialBehaviour socialBehaviour = Avatars[i].GetComponentInChildren<SocialBehaviour>();
            if(socialBehaviour != null) {
                SetSocialBehaviourParams(socialBehaviour);
            }     
        }
    }

    // OnValidate is called when the script is loaded or a value is changed in the Inspector.
    private void OnValidate() {
        // Loop through all PathControllers and set their parameters.
        foreach(GameObject controllerObject in PathControllers) 
        {
            AgentPathController pathController = controllerObject.GetComponent<AgentPathController>();
            if(pathController != null) 
            {
                SetPathControllerParams(pathController);
            }
            AgentPathManager pathManager = controllerObject.GetComponent<AgentPathManager>();
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

        // Loop through all ConversationalAgentFrameworks and set their parameters.
        foreach(GameObject controllerObject in ConversationalAgentFrameworks) 
        {
            ConversationalAgentFramework conversationalAgentFramework = controllerObject.GetComponent<ConversationalAgentFramework>();
            if(conversationalAgentFramework != null) 
            {
                SetConversationalAgentFrameworkParams(conversationalAgentFramework);
            }
            SocialBehaviour socialBehaviour = controllerObject.GetComponent<SocialBehaviour>();
            if(socialBehaviour != null) {
                SetSocialBehaviourParams(socialBehaviour);
            }    
        }
    }

    // Method to set parameters for PathController.
    private void SetPathControllerParams(AgentPathController pathController){
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

    private void SetPathManagerrParams(AgentPathManager pathManager){
        pathManager.goalRadius = goalParameters.goalRadius;
    }

    private void SetMotionMatchingControllerParams(MotionMatchingController motionMatchingController){
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

    private void SetConversationalAgentFrameworkParams(ConversationalAgentFramework conversationalAgentFramework){
        if(useFacialExpression == true) {
            conversationalAgentFramework.enabled = true;
        } else {
            conversationalAgentFramework.enabled = false;
        }

        conversationalAgentFramework.openness          = ocean.openness;
        conversationalAgentFramework.conscientiousness = ocean.conscientiousness;
        conversationalAgentFramework.extraversion      = ocean.extraversion;
        conversationalAgentFramework.agreeableness     = ocean.agreeableness;
        conversationalAgentFramework.neuroticism       = ocean.neuroticism;

        conversationalAgentFramework.e_happy           = emotion.happy;
        conversationalAgentFramework.e_sad             = emotion.sad;
        conversationalAgentFramework.e_angry           = emotion.angry;
        conversationalAgentFramework.e_disgust         = emotion.disgust;
        conversationalAgentFramework.e_fear            = emotion.fear;
        conversationalAgentFramework.e_shock           = emotion.shock;      
    }

    private void SetSocialBehaviourParams(SocialBehaviour socialBehaviour){

    }

#if UNITY_EDITOR
    public void SaveToFile(string path)
    {
        string json = JsonUtility.ToJson(this, true);
        File.WriteAllText(path, json);
    }

    public void LoadFromFile(string path)
    {
        string json = File.ReadAllText(path);
        JsonUtility.FromJsonOverwrite(json, this);
    }
    
    public void SaveSettings()
    {
        string path = EditorUtility.SaveFilePanel("Save Agent Settings", "", "AgentSettings", "json");
        if (!string.IsNullOrEmpty(path))
        {
            SaveToFile(path);
        }
    }

    public void LoadSettings()
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