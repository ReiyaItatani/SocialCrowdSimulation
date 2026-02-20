# Social Crowd Simulation Wiki

Dynamic pedestrian simulation with social forces, group behavior, gaze control, and motion matching.

[MIG2024 Paper](https://dl.acm.org/doi/10.1145/3677388.3696337) | [Computers & Graphics 2025](https://www.sciencedirect.com/science/article/pii/S009784932500127X)

<!-- TODO: images/hero.gif — Running simulation showing agents walking, avoiding each other, group behavior -->

---

## Getting Started

| | Page | |
|:-:|------|:-:|
| 1 | [Installation](Installation.md) | Package Manager setup |
| 2 | [Quick Start](Quick-Start.md) | Prefab creation → agent list → spawn → run |
| 3 | [First-Person Camera](First-Person-Camera.md) | Add a controllable player |
| 4 | [Environment Setup](Environment-Setup.md) | Walls and obstacles |

## Configuration

| Page | |
|------|:-:|
| [Agent Manager](Agent-Manager.md) | Force weights, motion matching, debug gizmos |

## Architecture

| Page | |
|------|:-:|
| [Architecture Overview](Architecture-Overview.md) | 5-layer pipeline design |
| [Pipeline Layers](Pipeline-Layers.md) | L1-2, L3, L4, L5 details |
| [Data Contracts](Data-Contracts.md) | `readonly struct` types |
| [Group System](Group-System.md) | Group management, shared FOV |
| [Animation and Gaze](Animation-and-Gaze.md) | SocialBehaviour, gaze controller |
| [Customization](Customization.md) | Swap pipeline layers |

---

## Editor Window

All tools: **CollisionAvoidance > Social Crowd Simulation**

<!-- TODO: images/editor-window-overview.png — Full Social Crowd Simulation window showing all 3 sections -->

| Section | Purpose |
|---------|---------|
| **Scene Setup** | Create AvatarCreator + required tags |
| **Auto Setup** | Drag & drop humanoid → agent prefab |
| **Create Player** | First-person controllable player |

---

## Project Structure

```
Assets/com.reiya.socialcrowdsimulation/
  Runtime/Core/
    Pipeline/              # 5-layer pipeline
      Perception/          # L1-2
      Prediction/          # L3
      Decision/            # L4
      Motor/               # L5
      Driver/              # AgentPathController
      Navigation/          # AgentPathManager
    Animation/             # SocialBehaviour, GazeController
    Avatar/                # ParameterManager
    Group/                 # GroupManager, SharedFOV
    Creator/
      AvatarCreator/       # AvatarCreatorQuickGraph, AgentManager
      PlayerCreator/       # InputManager
    Environment/           # NormalVector
  Editor/Core/
    SocialCrowdSimulationWindow.cs  # Unified editor window
    PrefabCreator/         # AgentPrefabFactory
    AutoSetup/             # DefaultAssetLocator
    PlayerCreator/         # PlayerCreationWindow
  Sample/
    QuickStart/            # Default assets, examples
    SocialCrowdSimulationDemo.unity
```
