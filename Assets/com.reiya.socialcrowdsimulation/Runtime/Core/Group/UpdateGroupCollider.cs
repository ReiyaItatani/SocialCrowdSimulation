using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
public class UpdateGroupCollider : MonoBehaviour
{
    private CapsuleCollider groupCollider;
    private List<AgentPipelineCoordinator> coordinators = new List<AgentPipelineCoordinator>();
    public float agentRadius = 0.3f;

    void Start()
    {
        coordinators = GetComponent<GroupParameterManager>().coordinators;
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
        foreach (AgentPipelineCoordinator coordinator in coordinators)
        {
            combinedPosition += (Vector3)coordinator.GetCurrentPosition();
        }
        this.transform.position = combinedPosition / coordinators.Count;
    }

    void UpdateCircleColliderRadius()
    {
        float maxDistance = 0f;
        foreach (AgentPipelineCoordinator coordinator in coordinators)
        {
            float distance = Vector3.Distance(this.transform.position, coordinator.GetCurrentPosition());
            if (distance > maxDistance)
            {
                maxDistance = distance;
            }
        }
        if(maxDistance <= (coordinators.Count)/2){
            groupCollider.radius = maxDistance + agentRadius;
        }
    }
}
}