using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
public class UpdateGroupCollider : MonoBehaviour
{
    private CapsuleCollider groupCollider;
    private List<AgentPathController> pathControllers = new List<AgentPathController>();
    public float agentRadius = 0.3f;

    void Start()
    {
        pathControllers = GetComponent<GroupParameterManager>().pathControllers;
        groupCollider = GetComponent<CapsuleCollider>();
    }

    void Update()
    {
        UpdateCenterOfMass();
        UpdateCircleColliderRadius();
    }

    void UpdateCenterOfMass()
    {
        Vector3 combinedPosition = Vector3.zero;
        foreach (AgentPathController agentPathController in pathControllers)
        {
            combinedPosition += (Vector3)agentPathController.GetCurrentPosition();
        }
        this.transform.position = combinedPosition / pathControllers.Count;
    }

    void UpdateCircleColliderRadius()
    {
        float maxDistance = 0f;
        foreach (AgentPathController agentPathController in pathControllers)
        {
            float distance = Vector3.Distance(this.transform.position, agentPathController.GetCurrentPosition());
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }
        }
        if(maxDistance <= (pathControllers.Count)/2){
            groupCollider.radius = maxDistance + agentRadius;    
        }
    }
}
}