# Agent Manager

`AgentManager` is a centralized parameter distributor attached to the **AgentCreator** GameObject. It configures all spawned agents simultaneously from a single Inspector panel.

## Force Weights

These control the strength of each social force in the L4 Decision layer:

| Parameter | Default | Description |
|-----------|---------|-------------|
| `toGoalWeight` | 1.5 | How strongly agents move toward their goal |
| `avoidNeighborWeight` | 0.5 | Avoidance of nearby agents within the avoidance area |
| `avoidanceWeight` | 2.3 | Urgent collision avoidance (sudden evasion) |
| `groupForceWeight` | 0.5 | Force to maintain group formation (cohesion + repulsion + alignment) |
| `wallRepForceWeight` | 0.3 | Repulsion from walls |
| `avoidObstacleWeight` | 1.0 | Avoidance of obstacles |

## Goal Parameters

| Parameter | Default | Range | Description |
|-----------|---------|-------|-------------|
| `goalRadius` | 2.0 | 0.1 - 5.0 | Distance to consider the goal reached |
| `slowingRadius` | 3.0 | 0.1 - 5.0 | Distance at which agents start slowing down |

## Motion Matching Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `MaxDistanceMMAndCharacterController` | 0.1 | Max distance between SimulationBone and SimulationObject |
| `PositionAdjustmentHalflife` | 0.1 | Time to move half the distance between them |
| `PosMaximumAdjustmentRatio` | 0.1 | Ratio between adjustment and character velocity |

## Debug Gizmos

### Path Controller Debug
| Toggle | Description |
|--------|-------------|
| `ShowAvoidanceForce` | Visualize urgent avoidance force vector |
| `ShowAnticipatedCollisionAvoidance` | Visualize anticipated collision force |
| `ShowGoalDirection` | Visualize goal direction vector |
| `ShowCurrentDirection` | Visualize current movement direction |
| `ShowGroupForce` | Visualize group cohesion/repulsion/alignment force |
| `ShowWallForce` | Visualize wall repulsion force |
| `ShowObstacleAvoidanceForce` | Visualize obstacle avoidance force |

### Motion Matching Debug
| Toggle | Description |
|--------|-------------|
| `DebugSkeleton` | Show skeleton visualization |
| `DebugCurrent` | Show current pose |
| `DebugPose` | Show target pose |
| `DebugTrajectory` | Show predicted trajectory |
| `DebugContacts` | Show contact points |

## How It Works

`AgentManager` distributes parameters to all agents during `Start()` and `OnValidate()`:

```
AgentManager.Start()
  └── for each avatar:
      ├── SetPathControllerParams(AgentPathController)
      ├── SetPathManagerParams(AgentPathManager)
      ├── SetMotionMatchingControllerParams(MotionMatchingController)
      └── SetSocialBehaviourParams(SocialBehaviour)
```

Changing values in the Inspector updates all agents in real-time via `OnValidate()`.

## Save / Load Settings

In the Editor, `AgentManager` provides Save/Load functionality:
- **Save Settings**: Exports all parameters to a JSON file
- **Load Settings**: Imports parameters from a JSON file

![Agent Manager](../../.github/media/AgentManager.png)

---

Next: [Architecture Overview](Architecture-Overview.md)
