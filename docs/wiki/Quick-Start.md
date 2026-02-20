# Quick Start: Demo Scene Setup

A video walkthrough is available: [YouTube Tutorial](https://youtu.be/U8zkxdCCsnY)

To quickly try the simulation, open the demo scene at `Packages/SocialCrowdSimulation/Sample/SocialCrowdSimulationDemo.unity`.

---

## Step 0: Create Agent Prefabs from Humanoid Models

1. Open the unified editor window: **CollisionAvoidance > Social Crowd Simulation**
2. In the **Auto Setup** section, drag and drop one or more Humanoid models into the drop zone
3. A fully-configured agent prefab is created automatically in `Assets/Resources/<ModelName>/Agent.prefab`

Default assets (MotionMatchingData, FOV Mesh, Animator Controller, Avatar Mask, Phone Mesh, Audio Clips) are loaded automatically from `Sample/QuickStart/ForPrefabCreator/`. If any asset is missing, expand **Advanced Settings** to assign them manually.

<!-- TODO: screenshot of the Social Crowd Simulation window with Auto Setup section -->

### Generated Prefab Hierarchy

```
Agent (root, tag="Agent")
  Avatar (humanoid model)
    Rigidbody, CapsuleCollider, ParameterManager
    SocialBehaviour, GazeController
    MotionMatchingSkinnedMeshRenderer
    Sound (AudioSource)
    SmartPhone (prop on right hand)
  Pipeline
    AgentPathController, AgentPipelineCoordinator
    Navigation/       AgentPathManager
    PerceptionAttention/ DefaultPerceptionAttentionLayer + CollisionAvoidanceController
    Prediction/       DefaultPredictionLayer
    Decision/         DefaultDecisionLayer
    Motor/            DefaultMotorLayer
  Animation
    MotionMatchingController
```

### Validation

The factory validates:
- The model has a Humanoid rig (Animator with `isHuman = true`)
- A RightHand bone is mapped
- All required assets are assigned

If validation fails, an error dialog explains the issue.

---

## Step 1: Define the Crowd

1. In the **Project** window, right-click and select **Create > SocialCrowdSimulation > Agent List**
   - Or use the examples in `Sample/QuickStart/ForAvatarCreator/` (AgentsList_Example1 ~ 4)
2. Configure agents:
   - **Individual**: Agents that walk alone. Assign prefabs and a speed range (minSpeed / maxSpeed).
   - **Group**: Agents that walk together (2-3 members recommended). Each group has:
     - **groupName**: Must be unique. Do not use "Individual".
     - **count**: Number of members (validates the agents list size automatically)
     - **agents**: Prefabs for each member
     - **speedRange**: Min/max speed for the group

<!-- TODO: screenshot of AgentList ScriptableObject in Inspector -->

---

## Step 2: Scene Setup

1. Open **CollisionAvoidance > Social Crowd Simulation**
2. In the **Scene Setup** section, click **Create AvatarCreator**
3. This does two things:
   - Adds required tags: **Agent**, **Group**, **Wall**, **Object**, **Obstacle**
   - Creates an **AvatarCreator** GameObject with:
     - **AgentManager** — centralized parameter control for all agents
     - **AvatarCreatorQuickGraph** — agent spawning system

<!-- TODO: screenshot of AvatarCreator GameObject in Inspector -->

---

## Step 3: Path Setup and Agent Instantiation

### 3.1 Set Up Paths

- Use examples from `Sample/QuickStart/ForAvatarCreator/` (PathGraph_Example1 ~ 4)
- Place a **PathGraph** prefab in your scene
- Paths are defined by `QuickGraphNode` objects connected as neighbors — agents navigate between these nodes
- Select a `QuickGraph` and click **Check Neighbourhood** to validate bidirectional connections

### 3.2 Configure and Spawn

1. On the **AvatarCreator** GameObject, assign:
   - **AgentsList**: Your Agent List asset
   - **QuickGraph**: The path graph in the scene
2. Configure spawn settings:
   - **SpawnRadius**: How scattered agents are around spawn points
   - **SpawnMethod**:
     - **OnNode**: Spawns around randomly selected nodes
     - **OnEdge**: Spawns on the midpoint of edges between nodes
   - **Agent Height / Agent Radius**: Physics dimensions for colliders
3. **Bake the NavMesh** (Window > AI > Navigation) before instantiation — the spawner uses NavMesh to find valid positions
4. Click **Instantiate Avatars** to spawn all individual and group agents

<!-- TODO: screenshot of AvatarCreatorQuickGraph Inspector with spawn settings -->

---

## Step 4: Run the Simulation

Press **Play**. Agents will:
- Navigate between waypoints using the path graph
- Avoid collisions with each other (urgent avoidance + anticipated collision avoidance)
- Maintain group formations (cohesion, repulsion, alignment)
- Exhibit natural gaze behavior (looking at avoidance targets, group members)
- Switch between animation states (walking, talking, using smartphone)

---

Next: [First-Person Camera](First-Person-Camera.md) | [Agent Manager](Agent-Manager.md)
