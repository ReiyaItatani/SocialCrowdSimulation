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

## Project Structure

```
Assets/com.reiya.socialcrowdsimulation/
  Runtime/Core/
    Pipeline/              # 5-layer pipeline architecture
      Perception/          # L1-2: DefaultPerceptionAttentionLayer
      Prediction/          # L3: DefaultPredictionLayer
      Decision/            # L4: DefaultDecisionLayer
      Motor/               # L5: DefaultMotorLayer
      Driver/              # AgentPathController, BasePathController
      Navigation/          # AgentPathManager (graph-based navigation)
    Animation/             # SocialBehaviour, GazeController
    Avatar/                # ParameterManager, AgentState
    Group/                 # GroupManager, SharedFOV, GroupColliderMovement
    Creator/               # AvatarCreatorQuickGraph, AgentManager
    Environment/           # NormalVector (wall/obstacle normals)
    Debug/                 # GizmoDrawer, TrailRendererGizmo
    Utils/                 # MathExtensions, DrawUtils
  Editor/                  # Custom inspectors and editor tools
  Sample/                  # Demo scenes and quickstart resources
```
