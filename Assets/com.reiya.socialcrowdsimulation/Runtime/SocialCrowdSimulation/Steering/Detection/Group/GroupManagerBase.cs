using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.PlayerLoop;

namespace CollisionAvoidance{

public class GroupManagerBase : MonoBehaviour
{
    //Group Members
    public List<GameObject> groupMembers = new List<GameObject>();
    protected GroupParameterManager groupParameterManager;
    protected List<GameObject> agentsInCategory = new List<GameObject>();

    protected bool initializedGroup = false;
    //Group Collider
    protected bool onGroupCollider = false;
    //Shared FOV
    protected HashSet<GameObject> agentsInFOV = new HashSet<GameObject>();

    private void Start()
    {
        Init();
    }

    protected virtual void Init(){
        agentsInCategory = GetNewGroupAgents();
        initializedGroup = true;
    }

    private void Update()
    {
        CoUpdate();
    }

    protected virtual void CoUpdate(){

    }

    public GroupParameterManager GetGroupParameterManager(){
        return groupParameterManager;
    }

    protected virtual List<GameObject> GetNewGroupAgents(){
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

    public bool GetOnGroupCollider(){
        return onGroupCollider;
    }

    public List<GameObject> GetAgentsInSharedFOV(){
        return agentsInFOV.ToList();
    }
}
}