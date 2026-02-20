# Quick Start

[YouTube Tutorial](https://youtu.be/U8zkxdCCsnY) | Demo scene: `Sample/SocialCrowdSimulationDemo.unity`

---

## Step 0: Create Agent Prefabs

**CollisionAvoidance > Social Crowd Simulation** > **Auto Setup**

Drag and drop Humanoid models into the drop zone. Agent prefabs are saved to `Assets/Resources/<ModelName>/Agent.prefab`.

<!-- TODO: images/auto-setup-window.png — Social Crowd Simulation window, Auto Setup section with drop zone visible -->

Default assets are auto-loaded from `Sample/QuickStart/ForPrefabCreator/`. Expand **Advanced Settings** to override.

<!-- TODO: images/auto-setup-advanced.png — Advanced Settings expanded (MMData, FOV Mesh, Animator, Avatar Mask, Phone, Audio) -->

<!-- TODO: images/auto-setup-result.png — Success dialog or generated prefab in Project window -->

---

## Step 1: Define the Crowd

**Create > SocialCrowdSimulation > Agent List** (or use `Sample/QuickStart/ForAvatarCreator/AgentsList_Example`)

<!-- TODO: images/agent-list.png — AgentsList Inspector showing Individual section + at least one Group section -->

| Field | Description |
|-------|-------------|
| **Individual** | Solo walking agents + speed range |
| **Group** | 2-3 agents walking together. Unique `groupName` required |

---

## Step 2: Scene Setup

**CollisionAvoidance > Social Crowd Simulation** > **Scene Setup** > **Create AvatarCreator**

<!-- TODO: images/scene-setup.png — Scene Setup section of the window with Create AvatarCreator button -->

This creates an **AvatarCreator** GameObject with `AgentManager` + `AvatarCreatorQuickGraph`.

<!-- TODO: images/avatar-creator-inspector.png — AvatarCreator Inspector showing AgentManager and AvatarCreatorQuickGraph -->

---

## Step 3: Path Setup & Spawn

### 3.1 Place a PathGraph

Drag a PathGraph from `Sample/QuickStart/ForAvatarCreator/` into the scene.

<!-- TODO: images/path-graph-scene.png — Scene view showing QuickGraph nodes (red spheres) with connection lines -->

### 3.2 Configure & Instantiate

1. Assign **AgentsList** and **QuickGraph** on the AvatarCreator
2. Set **SpawnRadius** and **SpawnMethod** (OnNode / OnEdge)
3. **Bake NavMesh** (Window > AI > Navigation)
4. Click **Instantiate Avatars**

<!-- TODO: images/spawn-settings.png — AvatarCreatorQuickGraph Inspector with settings filled in -->

<!-- TODO: images/instantiated-agents.png — Scene view after Instantiate Avatars, showing spawned agents -->

---

## Step 4: Run

Press **Play**.

<!-- TODO: images/simulation-running.png — Running simulation showing agents walking, avoiding each other -->

---

Next: [First-Person Camera](First-Person-Camera.md) | [Agent Manager](Agent-Manager.md)
