using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MotionMatching;

namespace CollisionAvoidance{

public class GroupParameterManager : MonoBehaviour, IParameterManager
{
    public List<AgentPathController> pathControllers = new List<AgentPathController>();

    public Vector3 GetCurrentDirection(){
        Vector3 currentDirectionAverage = Vector3.zero;  
        foreach(AgentPathController pathController in pathControllers){
            currentDirectionAverage += pathController.GetCurrentDirection();
        }
        return currentDirectionAverage.normalized;
    }

    public Vector3 GetCurrentPosition(){
        Vector3 currentPositionAverage = Vector3.zero;  
        foreach(AgentPathController pathController in pathControllers){
            currentPositionAverage += (Vector3)pathController.GetCurrentPosition();
        }
        return currentPositionAverage/pathControllers.Count;
    }

    public float GetCurrentSpeed(){
        float currentSpeedAverage = 0f;  
        foreach(AgentPathController pathController in pathControllers){
            currentSpeedAverage += pathController.GetCurrentSpeed();
        }
        return currentSpeedAverage/pathControllers.Count;
    }

    public Vector3 GetCurrentAvoidanceVector(){
        Vector3 currentAvoidanceVectorAverage = Vector3.zero;  
        foreach(AgentPathController pathController in pathControllers){
            currentAvoidanceVectorAverage += pathController.GetCurrentAvoidanceVector();
        }
        return currentAvoidanceVectorAverage.normalized;
    }

    public string GetGroupName(){
        return pathControllers[0].GetGroupName();
    }
}
}