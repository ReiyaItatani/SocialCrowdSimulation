using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{

public class GroupParameterManager : CrowdSimulationMonoBehaviour, IParameterManager
{
    public List<AgentPipelineCoordinator> coordinators = new List<AgentPipelineCoordinator>();

    public Vector3 GetCurrentDirection(){
        Vector3 currentDirectionAverage = Vector3.zero;
        foreach(AgentPipelineCoordinator coordinator in coordinators){
            currentDirectionAverage += coordinator.GetCurrentDirection();
        }
        return currentDirectionAverage.normalized;
    }

    public Vector3 GetCurrentPosition(){
        Vector3 currentPositionAverage = Vector3.zero;
        foreach(AgentPipelineCoordinator coordinator in coordinators){
            currentPositionAverage += (Vector3)coordinator.GetCurrentPosition();
        }
        return currentPositionAverage/coordinators.Count;
    }

    public float GetCurrentSpeed(){
        float currentSpeedAverage = 0f;
        foreach(AgentPipelineCoordinator coordinator in coordinators){
            currentSpeedAverage += coordinator.GetCurrentSpeed();
        }
        return currentSpeedAverage/coordinators.Count;
    }

    public Vector3 GetCurrentAvoidanceVector(){
        Vector3 currentAvoidanceVectorAverage = Vector3.zero;
        foreach(AgentPipelineCoordinator coordinator in coordinators){
            currentAvoidanceVectorAverage += coordinator.GetCurrentAvoidanceVector();
        }
        return currentAvoidanceVectorAverage.normalized;
    }

    public string GetGroupName(){
        return coordinators[0].GetGroupName();
    }
}
}