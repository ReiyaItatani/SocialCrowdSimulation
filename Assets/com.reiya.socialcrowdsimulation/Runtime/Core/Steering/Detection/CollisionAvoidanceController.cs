using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace CollisionAvoidance{
    public class CollisionAvoidanceController : MonoBehaviour
    {
        public AgentPathController pathController;
        public CapsuleCollider agentCollider;
        public CapsuleCollider groupCollider;


        [Header("Basic Collision Avoidance")]
        public Vector3 avoidanceColliderSize = new Vector3(1.5f, 1.5f, 1.5f); 
        private GameObject basicAvoidanceArea;
        private UpdateAvoidanceTarget updateAvoidanceTarget;
        private BoxCollider avoidanceCollider;


        // [Header("Anticipated Collision Avoidance")]
        // public Vector3 anticipatedAvoidanceColliderSize = new Vector3(4.5f, 1.5f, 3.9f); 
        // private GameObject anticipatedAvoidanceArea;
        // private UpdateAnticipatedAvoidanceTarget updateAnticipatedAvoidanceTarget;
        // private BoxCollider anticipatedAvoidanceCollider;

        [Header("Basic Collision Avoidance Semi Circle Area")]
        public GameObject FOVMeshPrefab;
        private GameObject basicAvoidanceSemiCircleArea;
        private List<UpdateAvoidanceTarget> updateAvoidanceTargetsInFOV;
        private FOVActiveController fovActiveController;
        public SocialBehaviour socialBehaviour;


        [Header("Repulsion Force from the wall")]
        public AgentCollisionDetection agentCollisionDetection; 

        private bool isInitialized = false;
        public bool IsInitialized{
            get{
                return isInitialized;
            }
        }

        void Awake(){
            InitCollisionAvoidanceController();
        }

        void OnEnable()
        {
            StartCoroutine(UpdateBasicAvoidanceAreaPos(agentCollider.height/2));
            StartCoroutine(UpdateBasicAvoidanceSemiCircleAreaPos(agentCollider.height/2, agentCollider.radius));
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        public void InitCollisionAvoidanceController(){
            if(isInitialized == true){
                return;
            }
            //Create Box Collider for Collision Avoidance Force
            basicAvoidanceArea                  = new GameObject("BasicCollisionAvoidanceArea");
            basicAvoidanceArea.transform.parent = this.transform;
            updateAvoidanceTarget               = basicAvoidanceArea.AddComponent<UpdateAvoidanceTarget>();
            updateAvoidanceTarget.InitParameter(agentCollider, groupCollider);
            avoidanceCollider                   = basicAvoidanceArea.AddComponent<BoxCollider>();
            avoidanceCollider.size              = avoidanceColliderSize;
            avoidanceCollider.isTrigger         = true;

            //Create FOV for Collision Avoidance Force
            basicAvoidanceSemiCircleArea                  = Instantiate(FOVMeshPrefab, this.transform.position, this.transform.rotation);
            basicAvoidanceSemiCircleArea.transform.parent = this.transform;
            fovActiveController                           = basicAvoidanceSemiCircleArea.GetComponent<FOVActiveController>();
            fovActiveController.InitParameter(gameObject.GetComponent<CollisionAvoidanceController>());
            updateAvoidanceTargetsInFOV = GetAllChildObjects(basicAvoidanceSemiCircleArea)
                .Select(child => child.GetComponent<UpdateAvoidanceTarget>())
                .Where(component => component != null)
                .ToList();
            foreach (var updateAvoidanceTarget in updateAvoidanceTargetsInFOV)
            {
                updateAvoidanceTarget.InitParameter(agentCollider, groupCollider);
            }

            //Create Agent Collision Detection
            agentCollisionDetection                 = agentCollider.GetComponent<AgentCollisionDetection>();
            if (agentCollisionDetection == null)
            {
                agentCollisionDetection = agentCollider.gameObject.AddComponent<AgentCollisionDetection>();
                Debug.Log("AgentCollisionDetection script added");
            }

            //Call Once to Initialize
            CalculateBasicAvoidanceAreaPos(agentCollider.height/2);
            CalculateBasicAvoidanceSemiCircleAreaPos(agentCollider.height/2, agentCollider.radius);
            
            isInitialized = true;
        }

        private List<GameObject> GetAllChildObjects(GameObject parentObject)
        {
            List<GameObject> childObjects = new List<GameObject>();

            if (parentObject != null)
            {
                foreach (Transform childTransform in parentObject.transform)
                {
                    childObjects.Add(childTransform.gameObject);
                }
            }

            return childObjects;
        }

        private IEnumerator UpdateBasicAvoidanceAreaPos(float AgentHeight){
            while(true){
                if(pathController.GetCurrentDirection() == Vector3.zero) yield return null;
                CalculateBasicAvoidanceAreaPos(AgentHeight);
                yield return null;
            }
        }

        private void CalculateBasicAvoidanceAreaPos(float AgentHeight){
            Vector3 Center = (Vector3)pathController.GetCurrentPosition() + pathController.GetCurrentDirection().normalized * avoidanceCollider.size.z/2;
            basicAvoidanceArea.transform.position = new Vector3(Center.x, AgentHeight, Center.z);
            Quaternion targetRotation = Quaternion.LookRotation(pathController.GetCurrentDirection().normalized);
            basicAvoidanceArea.transform.rotation = targetRotation;
        }

        private IEnumerator UpdateBasicAvoidanceSemiCircleAreaPos(float AgentHeight, float AgentRadius){
            while(true){
                if(pathController.GetCurrentDirection() == Vector3.zero) yield return null;
                CalculateBasicAvoidanceSemiCircleAreaPos(AgentHeight, AgentRadius);
                yield return null;
            }
        }

        private void CalculateBasicAvoidanceSemiCircleAreaPos(float AgentHeight, float AgentRadius){
            Vector3   currentPosition = (Vector3)pathController.GetCurrentPosition();
            Vector3   lookAtDirection = socialBehaviour.GetCurrentLookAt().normalized;
            Vector3   newPosition     = currentPosition + lookAtDirection * AgentRadius;
            Quaternion targetRotation = Quaternion.LookRotation(lookAtDirection);

            basicAvoidanceSemiCircleArea.transform.position = new Vector3(newPosition.x, AgentHeight, newPosition.z);
            targetRotation *= Quaternion.Euler(0, 180, 0);
            
            basicAvoidanceSemiCircleArea.transform.rotation = targetRotation;
        }

        public List<GameObject> GetOthersInAvoidanceArea(){
            return updateAvoidanceTarget.GetOthersInAvoidanceArea();
        }

        public List<GameObject> GetOthersInFOV(){
            GameObject             activeGameObject      = fovActiveController.GetActiveChildObject();
            UpdateAvoidanceTarget _updateAvoidanceTarget = activeGameObject.GetComponent<UpdateAvoidanceTarget>();
            return _updateAvoidanceTarget.GetOthersInAvoidanceArea();
        }

        public List<GameObject> GetObstaclesInFOV(){
            GameObject             activeGameObject      = fovActiveController.GetActiveChildObject();
            UpdateAvoidanceTarget _updateAvoidanceTarget = activeGameObject.GetComponent<UpdateAvoidanceTarget>();
            return _updateAvoidanceTarget.GetObstaclesInAvoidanceArea();
        }

        public GameObject GetCurrentWallTarget(){
            return agentCollisionDetection.GetCurrentWallTarget();
        }

        public CapsuleCollider GetAgentCollider(){
            return agentCollider;
        }

        public GameObject GetAgentGameObject(){
            return agentCollider.gameObject;
        }

        public Vector3 GetAvoidanceColliderSize(){
            return avoidanceColliderSize;
        }

        public UpperBodyAnimationState GetUpperBodyAnimationState(){
            return socialBehaviour.GetUpperBodyAnimationState();
        }

        public AgentCollisionDetection GetAgentCollisionDetection(){
            return agentCollisionDetection;
        }
    }
}