# Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior(MIG2024, C＆G2025)

Welcome to the **Social Crowd Simulation: Improving Realism with Social Rules and Gaze Behavior** repository. This system simulates dynamic pedestrian movement by integrating various social and physical forces. Agents will:

- Steer toward their goals
- Avoid collisions
- Form natural groups
- Realistic Attention Model
- Natural Animation by [motion matching](https://github.com/JLPM22/MotionMatching)

For a detailed overview and demo, visit the [Project Page](https://reiyaitatani.github.io/SocialCrowdSimulation/).
For the paper, [MIG2024](https://dl.acm.org/doi/10.1145/3677388.3696337), [Computers & Graphics 2025](https://www.sciencedirect.com/science/article/pii/S009784932500127X)

<img src=".github/media/collision_avoidance_system.png" alt="Collision Avoidance System" width="300"/>

![Collision Avoidance Demo](.github/media/collision_avoidance.gif)

---

## Table of Contents
1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Quick Start: Demo Scene Setup](#quick-start-demo-scene-setup)
   1. [Step 0. Prepare a Humanoid Character for Social Crowd Simulation](#0-prepare-a-humanoid-character-for-social-crowd-simulation)
   2. [Step 1. Define the Crowd to be Spawned in the Scene](#1-define-the-crowd-to-be-spawned-in-the-scene)
   3. [Step 2. Create the GameObjects Required for Avatar Creation](#2-create-the-gameobjects-required-for-avatar-creation)
   4. [Step 3. Path Setup and Agent Instantiation](#3-path-setup-and-agent-instantiation)
   5. [Step 4. Run the Simulation](#4-run-the-simulation)
4. [Adding a First-Person Camera Player](#adding-a-first-person-camera-player)
5. [To Add Obstacles and Walls](#to-add-obstacles-and-walls)
6. [AgentManager](#agentmanager)
7. [Architecture](#architecture)
8. [Contributions](#contributions)
9. [Citation](#citation)

---

## Prerequisites

1. **Unity 2021.2 or Newer**  
   This project requires Unity 2021.2 or later. Compatibility is not guaranteed on older versions.
---

## Installation

1. **Open Unity Editor**  
   Launch Unity (version 2021.2 or newer).

2. **Open Package Manager**  
   - Go to **Window > Package Manager**.

3. **Add Package via Git URL**  
   - Click the **Add (+)** button, then select **Add package by git URL...**.
   - Enter the following URL:
     ```
     https://github.com/ReiyaItatani/SocialCrowdSimulation.git?path=Assets/com.reiya.socialcrowdsimulation
     ```
   - Click **Add** to begin the installation.

> **Note:** All sample scenes are configured for the **Universal Render Pipeline (URP)**. If you are using a different render pipeline, you may need to convert the scenes accordingly.

---

## Quick Start: Demo Scene Setup
If you prefer to watch a video instead of reading texts, [Video](https://youtu.be/U8zkxdCCsnY)
Below is a step-by-step guide using the demo scenes included in this package. If you simply want to see an example or test the system quickly, follow these steps in order.

**But, to quickly start, there is a demo scene inside `Packages/SocialCrowdSimulation/Sample`, so use it!**

### 0. Prepare a Humanoid Character for Social Crowd Simulation

1. **Prefab Creation**  
   - In the **CollisionAvoidance** tab, click **PrefabCreator**. A window with setup options will appear.  
   - Everything required for setup is located in the `Packages/SocialCrowdSimulation/Sample/QuickStart/PrefabCreator` folder.
   - Drag and drop a Humanoid Character, and it will automatically create an Agent in the **Resources** folder.

   <img src=".github/media/PrefabCreator.png" alt="Prefab Creator" width="600"/>

2. **[MicroSoftRocketBoxAvatar](https://github.com/microsoft/Microsoft-Rocketbox) or [AvatarSDK](https://avatarsdk.com/)**  
   - If you **are not** using either MicroSoftRocketBoxAvatar or AvatarSDK avatars, **uncheck** the “Yes” box related to these avatars.  
     - This prevents blend shape scripts (associated with those specific avatars) from attaching incorrectly.
   - If using **MicroSoftRocketBoxAvatar**, open **CollisionAvoidance** tab > **Target Framework** > **RocketBox Avatar**.  
   - If using an **Avatar from Avatar SDK**, open **CollisionAvoidance** tab > **Target Framework** > **Avatar SDK**.  
   - Correctly setting this ensures that any blend shape functionality works as intended.

   <img src=".github/media/TargetFramework.png" alt="Target Framework" width="600"/>

### 1. Define the Crowd to be Spawned in the Scene

1. **Create an AgentList**  
   - In the **Project** window, right-click and select **Create > SocialCrowdSimulation > AgentList** to create a new **ScriptableObject**, or use the example in `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator`.
2. **Add Agents**  
   - The **AgentList** manages who will be spawned:
     - **Individual**: An agent walking alone.
     - **Group**: Agents in a group walking together.
   - **maxSpeed** and **minSpeed** define individual speed ranges.
   - **Group Names** must be unique. Do not use “Individual” as a group name. A group must have at least two members (2–3 recommended).

   <img src=".github/media/AgentList.png" alt="Agent List" width="600"/>

### 2. Create the GameObjects Required for Avatar Creation

1. **Automatically Create Required GameObjects**  
   - In the **CollisionAvoidance** tab, click **Create AvatarCreator**.
   - This adds necessary tags (**Agent**, **Group**, **Wall**, **Obstacle**) and creates two GameObjects in your Scene:
     - **OVRLipSyncObject**: Required for lip sync functionality.
     - **AgentCreator**: Contains:
       - **AgentManager**: A script for changing parameters of spawned avatars collectively.
       - **AvatarCreatorQuickGraph** (or similarly named): A script for spawning avatars in the Scene.

   <img src=".github/media/AvatarCreation.png" alt="Avatar Creator" width="600"/>

### 3. Path Setup and Agent Instantiation

#### 3.0 Set Up Paths

- Use the examples under `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator`.  
- Place one of the **PathGraph_Example** assets in your scene.  
- Paths are defined by connecting nodes, allowing agents to know where to walk.

#### 3.1 Instantiate Agents in the Scene

1. **Configure the AvatarCreatorQuickGraph**  
   - Assign your **AgentList** (created in Step 1) or use the example under `Packages/SocialCrowdSimulation/Sample/QuickStart/ForAvatarCreator` to `AvatarCreatorQuickGraph`.
   - Assign the **Path** (the one you placed in your scene) to `AgentCreator`.

2. **Spawn Settings**  
   - **SpawnRadius**: Controls how scattered agents will be when they appear.  
   - **SpawnMethod**: 
     - **OnNode**: Spawns avatars around selected nodes.  
     - **OnEdge**: Spawns avatars on the paths (edges) between nodes.

3. **Make Sure to Bake the Field Before Instantiation!**  
   - **Important**: Be sure to bake the navigation field (the area where the agents will be placed) before spawning avatars to ensure the correct paths and environment setup.

4. **Spawn the Avatars**  
   - Click **Instantiate Avatar** to create the avatars in your scene.

   <img src=".github/media/AvatarCreation2.png" alt="Avatar Creator" width="600"/>
   

### 4. Run the Simulation

- Press **Play**. The avatars should begin moving according to the defined rules.

---

## Adding a First-Person Camera Player

You can include a first-person camera player in your scene using the following steps:

1. **Open the "Create Player" Window**  
   - In the Unity Editor, open the **Create Player** window (from the CollisionAvoidance tab or the plugin menu).

2. **Configure Motion Matching Data**  
   - In the **Motion Matching Data** field, select a `.asset` file from  
     `Packages/SocialCrowdSimulation/Sample/QuickStart/ForPlayerCreator/MotionMatchingData`.

3. **Set a Humanoid Avatar**  
   - For the **Humanoid Avatar** field, choose a humanoid rig.

4. **Create the Player**  
   - Click **CreatePlayer** to generate a first-person camera player in your scene.

5. **Player Movement**  
   - The newly created player can be controlled with **WASD** keys.
   - It uses the **SpringCharacterController**, which you can learn more about in the Motion Matching documentation.
   - For detailed information on the `SpringCharacterController`, refer to the [Motion Matching documentation](https://jlpm22.github.io/motionmatching-docs/basics/character_controller/).

![Create Player Demo](.github/media/create_player.gif)

---

## To Add Obstacles and Walls

When adding walls or obstacles, please use the **Obstacle** and **Walls** assets located in  
`Packages/SocialCrowdSimulation/Sample/QuickStart/ForSetUpEnvironment`.

---

## AgentManager

`AgentManager` allows you to adjust the weights of various movement behaviors. Below is an overview of each weight parameter:

- **Goal Weight**: Controls how strongly agents move toward their goal.
- **Avoid Neighbor Weight**: Prevents collisions with other agents within the field of view by adjusting the agent’s position.
- **Avoidance Weight**: Handles sudden collision avoidance to keep agents from colliding unexpectedly.
- **Group Force Weight**: Manages the force that encourages agents to form or maintain a group.
- **Wall Rep Force**: Adjusts the force received from walls to keep agents from getting too close.
- **Avoid Obstacle Weight**: Helps agents avoid obstacles that appear in their path.

Additionally, you can toggle **Gizmos On/Off** for debugging, fine-tune **Motion Matching** parameters, and adjust **Facial Expressions**.  
However, to use the Facial Expression feature, you must use either the **Microsoft Rocket Box Avatar** or an avatar created with **Avatar SDK**.

<img src=".github/media/AgentManager.png" alt="Avatar Creator" width="600"/>

---

## Architecture

The steering system uses a **composition-based** architecture. Each agent GameObject is composed of independent MonoBehaviour components that communicate through a shared `AgentState` object.

### Component Overview

Each agent is assembled from independent, swappable components:

```mermaid
graph TB
    subgraph "Agent GameObject"
        APC["AgentPathController<br/><i>Facade + Motion Matching</i>"]
        FSC["ForceSolverComponent<br/><i>6 Force Calculations</i>"]
        SSC["SpeedSolverComponent<br/><i>Speed Management</i>"]
        RSC["ReactSolverComponent<br/><i>Collision Reaction</i>"]
        ADG["AgentDebugGizmos<br/><i>Debug Visualization (Optional)</i>"]
    end

    AS["AgentState<br/><i>Shared Mutable State</i>"]
    PS["PathSimulator<br/><i>Pure C# Class</i>"]

    APC -->|owns| AS
    APC -->|uses| PS
    FSC -->|read/write| AS
    SSC -->|read/write| AS
    RSC -->|read/write| AS
    ADG -->|read-only| AS
    PS -->|reads| AS

    style APC fill:#4a90d9,color:#fff
    style AS fill:#e8a838,color:#fff
    style PS fill:#7bc67e,color:#fff
    style FSC fill:#6c8ebf,color:#fff
    style SSC fill:#6c8ebf,color:#fff
    style RSC fill:#6c8ebf,color:#fff
    style ADG fill:#999,color:#fff
```

### Shared State: `AgentState`

All components read from and write to a single `AgentState` instance owned by `AgentPathController`. This replaces the tightly-coupled inherited fields from the previous deep inheritance chain.

```mermaid
classDiagram
    class AgentState {
        +Vector3 CurrentPosition
        +Vector3 CurrentDirection
        +float CurrentSpeed
        +Vector3 AvoidanceVector
        +Vector3 ToGoalVector
        +Vector3 AvoidNeighborsVector
        +Vector3 GroupForce
        +Vector3 WallRepForce
        +Vector3 AvoidObstacleVector
        +float ToGoalWeight
        +float AvoidanceWeight
        +float AvoidNeighborWeight
        +float GroupForceWeight
        +float WallRepForceWeight
        +float AvoidObstacleWeight
        +bool OnCollide
        +bool OnMoving
        +GameObject CollidedAgent
        +bool OnInSlowingArea
        +string GroupName
    }
```

### Data Flow

```mermaid
flowchart LR
    subgraph Input
        CA["CollisionAvoidance<br/>Controller"]
        APM["AgentPath<br/>Manager"]
        AM["AgentManager"]
    end

    subgraph "Force Calculation"
        FSC["ForceSolver<br/>Component"]
    end

    subgraph "Speed Control"
        SSC["SpeedSolver<br/>Component"]
    end

    subgraph "Collision Reaction"
        RSC["ReactSolver<br/>Component"]
    end

    subgraph "Shared State"
        AS["AgentState"]
    end

    subgraph "Path Simulation"
        PS["PathSimulator"]
    end

    subgraph Output
        APC["AgentPath<br/>Controller"]
        MM["Motion<br/>Matching"]
    end

    CA -->|targets| FSC
    APM -->|goal position| FSC
    AM -->|weights| AS
    FSC -->|force vectors| AS
    SSC -->|speed| AS
    RSC -->|collision flags| AS
    AS -->|all state| PS
    PS -->|position, direction| APC
    APC -->|trajectory| MM
```

### Component Responsibilities

| Component | Responsibility | Writes to AgentState |
|-----------|---------------|---------------------|
| **ForceSolverComponent** | Calculates 6 forces (goal, avoidance, anticipated collision, group, wall, obstacle) via coroutines | Force vectors, avoidance targets |
| **SpeedSolverComponent** | Group-based speed adjustment, goal-based slowing, animation-based speed changes | CurrentSpeed, OnInSlowingArea |
| **ReactSolverComponent** | Collision detection callbacks, step-back/redirect reactions based on social relationships | OnCollide, OnMoving, CollidedAgent |
| **AgentPathController** | Facade for external API, owns AgentState, runs PathSimulator for trajectory prediction | CurrentPosition, CurrentDirection |
| **AgentDebugGizmos** | Draws force vectors as gizmos in Scene View (optional) | Read-only |

### PathSimulator

`PathSimulator` is a pure C# class (not a MonoBehaviour) that combines weighted forces into movement:

```mermaid
flowchart TD
    A["Weighted Force Sum"] --> B{"OnCollide?"}
    B -->|No| C["Normal Movement<br/>pos += direction * speed * dt"]
    B -->|Yes| D{"CollidedAgent<br/>exists?"}
    D -->|No| C
    D -->|Yes| E{"OnMoving?"}
    E -->|No| F["Step Back<br/>pos -= offset * 0.3 * dt"]
    E -->|Yes| G{"Anti-parallel?"}
    G -->|Yes| H["Reflection Vector<br/>pos += reflect(dir) * 0.1 * dt"]
    G -->|No| I{"Other agent<br/>in front?"}
    I -->|Yes| J["Stop<br/>pos = current"]
    I -->|No| C
```

### Adding / Removing Components

Because each component is independent, you can:
- **Remove `AgentDebugGizmos`** to disable debug visualization without affecting behavior
- **Replace `SpeedSolverComponent`** with a custom speed controller that writes to `state.CurrentSpeed`
- **Replace `ForceSolverComponent`** with a different steering algorithm that writes to the force vectors

The three core components (`ForceSolverComponent`, `SpeedSolverComponent`, `ReactSolverComponent`) are enforced via `[RequireComponent]` on `AgentPathController`.

### Initialization Order

```mermaid
sequenceDiagram
    participant Unity
    participant APC as AgentPathController
    participant FSC as ForceSolverComponent
    participant SSC as SpeedSolverComponent
    participant RSC as ReactSolverComponent

    Unity->>APC: Awake()
    APC->>APC: Create AgentState
    APC->>APC: GetComponent (resolve siblings)
    APC->>FSC: Initialize(state, controller)
    FSC->>FSC: Start force coroutines
    APC->>SSC: Initialize(state, controller)
    SSC->>SSC: Start speed coroutines
    APC->>RSC: Initialize(state, controller)
    RSC->>RSC: Start delayed collision subscription
    APC->>APC: InitMotionMatching()
    Unity->>APC: OnUpdate() (every frame)
    APC->>APC: PathSimulator.SimulatePath()
    APC->>APC: Sync to MotionMatching
```

---

## Contributions

We encourage contributions and inquiries—please **open an issue** or submit a **pull request** if you wish to collaborate or have questions.

---

## Citation

If you find this work beneficial, kindly attribute it to the authors or cite the following publication:

```bibtex
@article{Reiya2025,
title = {Social crowd simulation: Improving realism with social rules and gaze behavior},
journal = {Computers & Graphics},
volume = {131},
pages = {104286},
year = {2025},
issn = {0097-8493},
doi = {https://doi.org/10.1016/j.cag.2025.104286},
url = {https://www.sciencedirect.com/science/article/pii/S009784932500127X},
author = {Reiya Itatani and Nuria Pelechano},
keywords = {Crowd simulation, Collision avoidance, Gaze behavior, Social rules}
}
}
