using UnityEngine;
using MotionMatching;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

namespace CollisionAvoidance{
[RequireComponent(typeof(AgentManager))]
public class AvatarCreatorQuickGraph : MonoBehaviour
{
    [Header("Spawn Settings")]
    public AgentsList agentsList;
    public float spawnRadius = 1f;
    public QuickGraph quickGraph;
    public enum SpawnMethod
    {
        OnNode,
        OnEdge,
    }
    public SpawnMethod spawnMethod = SpawnMethod.OnNode;

    [Header("Agents Settings")]
    public float agentHeight = 1.8f;
    public float agentRadius = 0.3f;

    [Header("Info After Instantiation")]
    // instantiatedAvatars: A list of instantiated avatars.
    [ReadOnly] public List<GameObject> instantiatedAvatars = new List<GameObject>();
    [ReadOnly] public List<GameObject> instantiatedGroups = new List<GameObject>();

    public virtual void InstantiateAvatars()
    {
        InstantiateIndividuals();
        InstantiateGroups();
    }

    protected virtual void InstantiateIndividuals()
    {
        GameObject individualParent = GetOrCreateParent("Individual");

        foreach (GameObject agent in agentsList.individuals.agents)
        {
            var node = quickGraph._nodes[UnityEngine.Random.Range(0, quickGraph._nodes.Count)];
            var neighbours = node._neighbours[UnityEngine.Random.Range(0, node._neighbours.Count)];
            if (ComputeSafeSpawnPosition(node, neighbours, out Vector3 pos)){
                InstantiateAgent(agent, individualParent, "Individual", agentsList.individuals.speedRange, node, neighbours, pos);
            }
        }
    }

    protected virtual void InstantiateGroups()
    {
        foreach (GroupEntry group in agentsList.groups)
        {
            GameObject groupParent = GetOrCreateParent(group.groupName);
            instantiatedGroups.Add(groupParent);
            GameObject groupColliderObject = CreateGroupCollider(groupParent, group.groupName);
            GroupParameterManager groupParameterManager = groupColliderObject.GetComponent<GroupParameterManager>();
            GroupManager groupManager = CreateGroupManager(groupParent, groupColliderObject);
            var node = quickGraph._nodes[UnityEngine.Random.Range(0, quickGraph._nodes.Count)];
            var neighbours = node._neighbours[UnityEngine.Random.Range(0, node._neighbours.Count)];
            foreach (GameObject agent in group.agents)
            {
                if (ComputeSafeSpawnPosition(node, neighbours, out Vector3 pos)){
                    GameObject instance = InstantiateAgent(agent, groupParent, group.groupName, group.speedRange, node, neighbours , pos);
                    AssignGroupComponents(instance, groupParameterManager, groupManager);
                }
            }
        }
    }

    protected virtual GameObject InstantiateAgent(GameObject agent, GameObject parent, string groupName, SpeedRange speedRange, QuickGraphNode node, QuickGraphNode neighbours, Vector3 pos)
    {
        Debug.Log("Instantiating agent: " + agent.name);
        GameObject instance = Instantiate(agent, pos, Quaternion.identity);
        instance.name = groupName;
        instance.transform.parent = parent.transform;
        AssignPathController(instance, groupName, speedRange, pos, node, neighbours);
        instantiatedAvatars.Add(instance);
        return instance;
    }

    protected virtual void AssignPathController(GameObject instance, string groupName, SpeedRange speedRange, Vector3 pos, QuickGraphNode node, QuickGraphNode neighbours)
    {
        AgentPathController pathController = instance.GetComponentInChildren<AgentPathController>();
        AgentPathManager agentPathManager = instance.GetComponentInChildren<AgentPathManager>();
        // MotionMatchingController motionMatchingController = instance.GetComponentInChildren<MotionMatchingController>();
        // ConversationalAgentFramework conversationalAgentFramework = instance.GetComponentInChildren<ConversationalAgentFramework>();
        
        pathController.maxSpeed = speedRange.maxSpeed;
        pathController.minSpeed = speedRange.minSpeed;
        pathController.initialSpeed = UnityEngine.Random.Range(speedRange.minSpeed, speedRange.maxSpeed);
        pathController.groupName = groupName;
        agentPathManager.SetTargetNode(node);
        agentPathManager.SetTargetNode(neighbours);
        // pathController.transform.position = pos;
        // motionMatchingController.transform.position = pos;
        // conversationalAgentFramework.transform.position = pos;
    }

    protected virtual void AssignGroupComponents(GameObject instance, GroupParameterManager groupParameterManager, GroupManager groupManager)
    {
        AgentPathController pathController = instance.GetComponentInChildren<AgentPathController>();
        CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();
        
        if (groupParameterManager != null)
        {
            groupParameterManager.pathControllers.Add(pathController);
            collisionAvoidanceController.groupCollider = groupParameterManager.GetComponent<CapsuleCollider>();
        }
        if (groupManager != null)
        {
            pathController.groupManager = groupManager;
            groupManager.groupMembers.Add(instance);
        }
    }

    protected virtual GameObject GetOrCreateParent(string name)
    {
        Transform found = transform.Find(name);
        if (found != null) return found.gameObject;
        
        GameObject newParent = new GameObject(name);
        newParent.transform.SetParent(transform);
        return newParent;
    }

    protected virtual GameObject CreateGroupCollider(GameObject parent, string groupName)
    {
        GameObject groupColliderObject = new GameObject("GroupCollider");
        groupColliderObject.transform.SetParent(parent.transform);
        groupColliderObject.tag = "Group";
        CapsuleCollider groupCollider = groupColliderObject.AddComponent<CapsuleCollider>();
        groupCollider.height = agentHeight;
        groupCollider.center = new Vector3(0, agentHeight / 2f, 0);
        groupCollider.isTrigger = true;
        groupColliderObject.AddComponent<Rigidbody>();
        GroupParameterManager groupParameterManager = groupColliderObject.AddComponent<GroupParameterManager>();
        UpdateGroupCollider updateCenterOfMassPos = groupColliderObject.AddComponent<UpdateGroupCollider>();
        updateCenterOfMassPos.agentRadius = agentRadius;
        return groupColliderObject;
    }

    protected virtual GroupManager CreateGroupManager(GameObject parent, GameObject groupColliderObject)
    {
        GameObject managerObject = new GameObject("GroupManager");
        managerObject.transform.SetParent(parent.transform);
        GroupManager groupColliderManager = managerObject.AddComponent<GroupManager>();
        groupColliderManager.groupColliderGameObject = groupColliderObject;
        return groupColliderManager;
    }

    public virtual void DeleteAvatars()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }
        foreach (GameObject child in children)
        {
            DestroyImmediate(child);
        }
        instantiatedAvatars.Clear();
        instantiatedGroups.Clear();
    }

    protected virtual bool ComputeSafeSpawnPosition(QuickGraphNode node, QuickGraphNode neighbours, out Vector3 pos)
    {
        for (int i = 0; i < 100; i++)
        {
            Vector3 p = Vector3.zero;
            if (spawnMethod == SpawnMethod.OnNode)
            {
                Vector3 center = node.transform.position;
                Vector2 tmp = UnityEngine.Random.insideUnitCircle * spawnRadius;
                p = new Vector3(center.x + tmp.x, center.y, center.z + tmp.y);
            }
            else if (spawnMethod == SpawnMethod.OnEdge)
            {
                //Look for a random neighbour
                Vector3 v = (neighbours.transform.position + node.transform.position)/2.0f;
                Vector2 tmp = UnityEngine.Random.insideUnitCircle * spawnRadius;
                p = new Vector3(v.x + tmp.x, v.y, v.z + tmp.y);
            }

            if (NavMesh.SamplePosition(p, out NavMeshHit nmHit, 1.0f, -1))
            {
                RaycastHit[] hits = Physics.SphereCastAll(nmHit.position, 0.5f, Vector3.down, 0.1f, 1 << LayerMask.NameToLayer("Agent"));
                if (hits.Length == 0)
                {
                    pos = p;
                    return true;
                }
            }
        }

        pos = Vector3.zero;
        return false;
    }
}
}