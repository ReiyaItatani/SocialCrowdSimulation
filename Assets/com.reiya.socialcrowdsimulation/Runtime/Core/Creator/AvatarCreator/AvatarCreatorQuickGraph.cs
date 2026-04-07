using UnityEngine;
using MotionMatching;
using System.Collections.Generic;
using System;
using UnityEngine.AI;

namespace CollisionAvoidance
{
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

    [Header("Observer Patrol")]
    [Tooltip("Index into instantiatedAvatars for the observer agent. -1 = no patrol (random walk).")]
    public int observerAgentIndex = -1;

    [Header("Info After Instantiation")]
    // instantiatedAvatars: A list of instantiated avatars.
    [ReadOnly] public List<GameObject> instantiatedAvatars = new List<GameObject>();
    [ReadOnly] public List<GameObject> instantiatedGroups = new List<GameObject>();

    public virtual void InstantiateAvatars()
    {
        // Pre-compute patrol route to determine the observer's start node
        QuickGraphNode observerStartNode = null;
        List<QuickGraphNode> patrolRoute = null;
        if (observerAgentIndex >= 0)
        {
            patrolRoute = QuickGraphPathFinder.ComputePatrolRoute(quickGraph._nodes);
            if (patrolRoute.Count > 0)
                observerStartNode = patrolRoute[0];
        }

        InstantiateIndividuals(observerStartNode);
        InstantiateGroups(observerStartNode);

        if (observerAgentIndex >= 0 && observerAgentIndex < instantiatedAvatars.Count)
        {
            SetupObserverPatrol(instantiatedAvatars[observerAgentIndex], patrolRoute);
        }
    }

    /// <summary>
    /// Replace the observer agent's AgentPathManager with ObserverPathManager
    /// so it follows a structured patrol route instead of random walking.
    /// </summary>
    private void SetupObserverPatrol(GameObject observerAgent, List<QuickGraphNode> patrolRoute)
    {
        var coordinator = observerAgent.GetComponentInChildren<AgentPipelineCoordinator>();
        var oldPathManager = observerAgent.GetComponentInChildren<AgentPathManager>();

        if (coordinator == null || oldPathManager == null)
        {
            Debug.LogError("[AvatarCreator] Cannot setup observer patrol: missing coordinator or path manager.");
            return;
        }

        // Unsubscribe from old path manager's event before destroying
        oldPathManager.OnTargetReached -= coordinator.OnTargetReached;

        // Swap component: destroy old, add new
        GameObject navGO = oldPathManager.gameObject;
        DestroyImmediate(oldPathManager);

        var observerPathManager = navGO.AddComponent<ObserverPathManager>();
        observerPathManager.coordinator = coordinator;
        observerPathManager.goalRadius = 2.0f;
        observerPathManager.InitializePatrolWithRoute(patrolRoute);

        // Re-wire coordinator reference and re-subscribe to event
        coordinator.agentPathManager = observerPathManager;
        observerPathManager.OnTargetReached += coordinator.OnTargetReached;

        // Teleport to patrol start
        if (patrolRoute != null && patrolRoute.Count > 0)
        {
            Vector3 startPos = patrolRoute[0].transform.position;
            observerAgent.transform.position = new Vector3(startPos.x, observerAgent.transform.position.y, startPos.z);
        }
    }

    protected virtual void InstantiateIndividuals(QuickGraphNode excludeNode = null)
    {
        GameObject individualParent = GetOrCreateParent("Individual");
        var spawnableNodes = GetSpawnableNodes(excludeNode);

        foreach (GameObject agent in agentsList.individuals.agents)
        {
            var node = spawnableNodes[UnityEngine.Random.Range(0, spawnableNodes.Count)];
            var neighbours = node._neighbours[UnityEngine.Random.Range(0, node._neighbours.Count)];
            if (ComputeSafeSpawnPosition(node, neighbours, out Vector3 pos)){
                InstantiateAgent(agent, individualParent, "Individual", agentsList.individuals.speedRange, node, neighbours, pos);
            }
        }
    }

    protected virtual void InstantiateGroups(QuickGraphNode excludeNode = null)
    {
        var spawnableNodes = GetSpawnableNodes(excludeNode);

        foreach (GroupEntry group in agentsList.groups)
        {
            GameObject groupParent = GetOrCreateParent(group.groupName);
            instantiatedGroups.Add(groupParent);
            GameObject groupColliderObject = CreateGroupCollider(groupParent, group.groupName);
            GroupParameterManager groupParameterManager = groupColliderObject.GetComponent<GroupParameterManager>();
            GroupManager groupManager = CreateGroupManager(groupParent, groupColliderObject);
            var node = spawnableNodes[UnityEngine.Random.Range(0, spawnableNodes.Count)];
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
        AgentPipelineCoordinator coordinator = instance.GetComponentInChildren<AgentPipelineCoordinator>();
        AgentPathManager agentPathManager = instance.GetComponentInChildren<AgentPathManager>();

        Debug.Log($"[AvatarCreator] AssignPathController for '{instance.name}'" +
            $"\n  coordinator={coordinator != null} | agentPathManager={agentPathManager != null}");

        if (coordinator == null)
        {
            Debug.LogError($"[AvatarCreator] AgentPipelineCoordinator NOT FOUND in '{instance.name}'! " +
                $"Children: {string.Join(", ", GetChildNames(instance.transform))}");
        }

        coordinator.maxSpeed = speedRange.maxSpeed;
        coordinator.minSpeed = speedRange.minSpeed;
        coordinator.initialSpeed = UnityEngine.Random.Range(speedRange.minSpeed, speedRange.maxSpeed);
        coordinator.groupName = groupName;
        agentPathManager.SetTargetNode(node);
        agentPathManager.SetTargetNode(neighbours);

        // DI: Inject Model components into Pipeline controllers (GazeController, SocialBehaviour)
        Animator anim = instance.GetComponentInChildren<Animator>();
        CollisionAvoidance.MotionMatchingSkinnedMeshRenderer mmRenderer = instance.GetComponentInChildren<CollisionAvoidance.MotionMatchingSkinnedMeshRenderer>();
        AvatarParameterProxy pm = instance.GetComponentInChildren<AvatarParameterProxy>();

        Debug.Log($"[AvatarCreator]   anim={anim != null} | mmRenderer={mmRenderer != null} | pm={pm != null}");

        GazeController gazeController = instance.GetComponentInChildren<GazeController>();
        Debug.Log($"[AvatarCreator]   gazeController={gazeController != null}" +
            (gazeController != null ? $" (on '{gazeController.gameObject.name}')" : " — NOT FOUND!"));

        if (gazeController != null)
        {
            gazeController.InitializeDependencies(anim, mmRenderer, pm, coordinator);
        }
        else
        {
            Debug.LogError($"[AvatarCreator] GazeController NOT FOUND in '{instance.name}'!");
        }

        SocialBehaviour socialBehaviour = instance.GetComponentInChildren<SocialBehaviour>();
        if (socialBehaviour != null)
        {
            // Save AvatarMask before InitializeDependencies — FollowMotionMatching() inside sets it to null.
            var savedMask = mmRenderer != null ? mmRenderer.AvatarMask : null;
            socialBehaviour.InitializeDependencies(anim, mmRenderer, pm);
            if (mmRenderer != null) mmRenderer.AvatarMask = savedMask;

        }
    }

    protected virtual void AssignGroupComponents(GameObject instance, GroupParameterManager groupParameterManager, GroupManager groupManager)
    {
        AgentPipelineCoordinator coordinator = instance.GetComponentInChildren<AgentPipelineCoordinator>();
        CollisionAvoidanceController collisionAvoidanceController = instance.GetComponentInChildren<CollisionAvoidanceController>();

        if (groupParameterManager != null)
        {
            groupParameterManager.coordinators.Add(coordinator);
            collisionAvoidanceController.groupCollider = groupParameterManager.GetComponent<CapsuleCollider>();
        }
        if (groupManager != null)
        {
            coordinator.groupManager = groupManager;
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

    private static string[] GetChildNames(Transform root)
    {
        var names = new List<string>();
        foreach (Transform child in root)
        {
            names.Add(child.name);
            foreach (Transform grandchild in child)
            {
                names.Add($"  {child.name}/{grandchild.name}");
            }
        }
        return names.ToArray();
    }

    private List<QuickGraphNode> GetSpawnableNodes(QuickGraphNode excludeNode)
    {
        if (excludeNode == null)
            return quickGraph._nodes;

        var nodes = new List<QuickGraphNode>();
        foreach (var node in quickGraph._nodes)
        {
            if (node != excludeNode)
                nodes.Add(node);
        }
        return nodes.Count > 0 ? nodes : quickGraph._nodes;
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