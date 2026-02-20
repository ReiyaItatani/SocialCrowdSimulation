# Social Crowd Simulation Wiki

Welcome to the **Social Crowd Simulation** wiki. This system simulates dynamic pedestrian movement by integrating social forces, group behavior, gaze control, and motion matching animation.

For the published papers: [MIG2024](https://dl.acm.org/doi/10.1145/3677388.3696337) | [Computers & Graphics 2025](https://www.sciencedirect.com/science/article/pii/S009784932500127X)

---

## Getting Started

| Page | Description |
|------|-------------|
| [Installation](Installation.md) | Prerequisites and Unity Package Manager setup |
| [Quick Start](Quick-Start.md) | Step-by-step demo scene setup (prefab creation, agent list, path, spawning) |
| [First-Person Camera](First-Person-Camera.md) | Adding a controllable first-person player |
| [Environment Setup](Environment-Setup.md) | Adding walls and obstacles to the scene |

## Configuration

| Page | Description |
|------|-------------|
| [Agent Manager](Agent-Manager.md) | Force weights, motion matching parameters, debug gizmos |

## Architecture

| Page | Description |
|------|-------------|
| [Architecture Overview](Architecture-Overview.md) | 5-layer pipeline design, data flow, design principles |
| [Pipeline Layers](Pipeline-Layers.md) | Detailed documentation of L1-2, L3, L4, L5 |
| [Data Contracts](Data-Contracts.md) | All `readonly struct` types used for inter-layer communication |
| [Group System](Group-System.md) | Group management, shared FOV, group collider |
| [Animation and Gaze](Animation-and-Gaze.md) | SocialBehaviour, animation states, gaze controller |

## Extending

| Page | Description |
|------|-------------|
| [Customization](Customization.md) | How to swap pipeline layers, create custom implementations |

---

## Editor Tools

All editor tools are accessed via **CollisionAvoidance > Social Crowd Simulation**. The unified window has three sections:

| Section | Purpose |
|---------|---------|
| **Scene Setup** | Creates AvatarCreator GameObject + adds required tags (Agent, Group, Wall, Object, Obstacle) |
| **Auto Setup** | Drag & drop humanoid models to generate fully-configured agent prefabs |
| **Create Player** | Create a first-person controllable player from a humanoid model |

## Project Structure

```
Assets/com.reiya.socialcrowdsimulation/
  Runtime/Core/
    Pipeline/              # 5-layer pipeline architecture
      Perception/          # L1-2: DefaultPerceptionAttentionLayer, CollisionAvoidanceController
      Prediction/          # L3: DefaultPredictionLayer
      Decision/            # L4: DefaultDecisionLayer
      Motor/               # L5: DefaultMotorLayer
      Driver/              # AgentPathController, BasePathController, AgentDebugGizmos
      Navigation/          # AgentPathManager (graph-based navigation)
    Animation/             # SocialBehaviour, GazeController, AnimationModifier
    Avatar/                # ParameterManager, GroupParameterManager, AgentState
    Group/                 # GroupManager, SharedFOV, GroupColliderMovement
    Creator/
      AvatarCreator/       # AvatarCreatorQuickGraph, AgentManager, AgentsList, QuickGraph
      PlayerCreator/       # InputManager, PlayerInput
    Environment/           # NormalVector (wall/obstacle normals)
    Utils/                 # DrawUtils, CrowdSimulationMonoBehaviour
  Editor/Core/
    PrefabCreator/         # AgentPrefabFactory, AgentPrefabConfig
    AutoSetup/             # AutoSetupWindow, DefaultAssetLocator
    PlayerCreator/         # PlayerCreationWindow
    AgentManager/          # AgentManagerEditor, AvatarCreatorQuickGraphEditor
    Navigation/            # QuickGraphEditor
    SocialCrowdSimulationWindow.cs  # Unified editor window
  Sample/
    QuickStart/
      ForPrefabCreator/    # Default assets for agent prefab creation
      ForAvatarCreator/    # Example AgentLists and PathGraphs
      ForPlayerCreator/    # MotionMatchingData for player
      ForSetUpEnvironment/ # Wall and Obstacle prefabs
    Animation/             # Motion Matching data and Unity Animator animations
    SocialCrowdSimulationDemo.unity  # Demo scene
```
