using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace CollisionAvoidance{

public class GroupColliderManager : MonoBehaviour
{
    public List<GameObject> groupMembers = new List<GameObject>();
    public GameObject groupColliderGameObject;
    private CapsuleCollider groupCollider;
    private GroupParameterManager groupParameterManager;
    private List<CollisionAvoidanceController> collisionAvoidanceControllers = new List<CollisionAvoidanceController>();
    private HashSet<GameObject> agentsInFOV = new HashSet<GameObject>();

    private bool onGroupCollider = false;

    private List<GameObject> agentsInCategory = new List<GameObject>();
    private bool initializedGroup = false;

    void Start()
    {
        agentsInCategory = GetNewGroupAgents();
        initializedGroup = true;

        foreach(GameObject agent in agentsInCategory){
            collisionAvoidanceControllers.Add(agent.GetComponent<ParameterManager>().GetCollisionAvoidanceController());
        }

        groupCollider         = groupColliderGameObject.GetComponent<CapsuleCollider>();
        groupParameterManager = groupColliderGameObject.GetComponent<GroupParameterManager>();
    }

        void OnEnable()
        {
            StartCoroutine(UpdateAgentsInGroupFOV(0.1f));
        }

        void OnDisable()
        {
            StopAllCoroutines();
        }

        void Update()
    {
        UpdateCenterOfMass();
        DistanceChecker();
    }

    // Update the center of mass of the group
    void UpdateCenterOfMass()
    {
        Vector3 combinedPosition = Vector3.zero;
        foreach (GameObject agent in agentsInCategory)
        {
            combinedPosition += agent.transform.position;
        }
        this.transform.position = combinedPosition / agentsInCategory.Count;
    }

    // Check if the agents are close enough to each other
    private void DistanceChecker(){
        float maxDistance = 0f;
        foreach (GameObject agent in agentsInCategory)
        {
            float distance = Vector3.Distance(this.transform.position, agent.transform.position);
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }
        }
        if(maxDistance <= (agentsInCategory.Count)/2){
            groupCollider.enabled = true;
            //groupColliderGameObject.SetActive(true);
            onGroupCollider = true;
        }else{
            groupCollider.enabled = false;
            //groupColliderGameObject.SetActive(false);
            onGroupCollider = false;
            agentsInFOV.Clear();
        }
    }

    public bool GetOnGroupCollider(){
        return onGroupCollider;
    }

    public List<GameObject> GetAgentsInSharedFOV(){
        return agentsInFOV.ToList();
    }

    public GroupParameterManager GetGroupParameterManager(){
        return groupParameterManager;
    }

    private List<GameObject> GetNewGroupAgents(){
        List<GameObject> _groupMembers = new List<GameObject>();
        foreach(GameObject agent in groupMembers){
            _groupMembers.Add(agent.GetComponentInChildren<ParameterManager>().gameObject);
        }
        return _groupMembers;
    }

    public List<GameObject> GetGroupAgents(){
        if(initializedGroup == false){
           agentsInCategory = GetNewGroupAgents();
           initializedGroup = true;
        }
        return agentsInCategory;
    }

    private IEnumerator UpdateAgentsInGroupFOV(float updateTime){
        while(true){
            agentsInFOV.Clear();
            foreach(CollisionAvoidanceController collisionAvoidanceController in collisionAvoidanceControllers){
                agentsInFOV.UnionWith(collisionAvoidanceController.GetOthersInFOV());
            }
            //remove agents in same category
            agentsInFOV.ExceptWith(agentsInCategory); 
            yield return new WaitForSeconds(updateTime);
        }
    }
}
}