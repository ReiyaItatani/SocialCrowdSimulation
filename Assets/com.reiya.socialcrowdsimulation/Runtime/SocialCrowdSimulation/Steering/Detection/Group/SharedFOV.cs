using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace CollisionAvoidance{

public class SharedFOV : GroupManagerBase
{
    protected List<CollisionAvoidanceController> collisionAvoidanceControllers = new List<CollisionAvoidanceController>();

    protected virtual void InitSharedFOV()
    {
        foreach(GameObject agent in agentsInCategory){
            collisionAvoidanceControllers.Add(agent.GetComponent<ParameterManager>().GetCollisionAvoidanceController());
        }
    }

    protected virtual void SharedFOVCoUpdate()
    {
        base.CoUpdate();
        UpdateAgentsInSharedFOV();
    }

    private void UpdateAgentsInSharedFOV(){
        agentsInFOV.Clear();
        foreach(CollisionAvoidanceController collisionAvoidanceController in collisionAvoidanceControllers){
            agentsInFOV.UnionWith(collisionAvoidanceController.GetOthersInFOV());
        }
        //remove agents in same category
        agentsInFOV.ExceptWith(agentsInCategory); 
    }
}
}