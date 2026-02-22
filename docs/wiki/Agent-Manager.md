# Agent Manager

Centralized parameter panel on the **AvatarCreator** GameObject. Changes apply to all agents in real-time.

![Agent Manager](../../.github/media/AgentManager.png)

---

## Force Weights

| Parameter | Default | |
|-----------|---------|---|
| `toGoalWeight` | 1.5 | Strength toward goal |
| `avoidNeighborWeight` | 0.5 | Nearby agent avoidance |
| `avoidanceWeight` | 2.3 | Urgent collision evasion |
| `groupForceWeight` | 0.5 | Group formation (cohesion + repulsion + alignment) |
| `wallRepForceWeight` | 0.3 | Wall repulsion |
| `avoidObstacleWeight` | 1.0 | Obstacle avoidance |

---

## Goal Parameters

| Parameter | Default | Range |
|-----------|---------|-------|
| `goalRadius` | 2.0 | 0.1 - 5.0 |
| `slowingRadius` | 3.0 | 0.1 - 5.0 |

---

## Motion Matching Parameters

| Parameter | Default |
|-----------|---------|
| `MaxDistanceMMAndCharacterController` | 0.1 |
| `PositionAdjustmentHalflife` | 0.1 |
| `PosMaximumAdjustmentRatio` | 0.1 |

---

## Debug Gizmos

### Path Controller
| Toggle | |
|--------|-|
| `ShowAvoidanceForce` | Urgent avoidance vector |
| `ShowAnticipatedCollisionAvoidance` | Anticipated collision vector |
| `ShowGoalDirection` | Goal direction |
| `ShowCurrentDirection` | Current movement direction |
| `ShowGroupForce` | Group force vector |
| `ShowWallForce` | Wall repulsion vector |
| `ShowObstacleAvoidanceForce` | Obstacle avoidance vector |

### Motion Matching
| Toggle | |
|--------|-|
| `DebugSkeleton` | Skeleton visualization |
| `DebugCurrent` | Current pose |
| `DebugPose` | Target pose |
| `DebugTrajectory` | Predicted trajectory |
| `DebugContacts` | Contact points |

---

## Save / Load Settings

Export/import all parameters as JSON via the Inspector buttons.

---

Next: [Pipeline](Pipeline.md) | [Home](Home.md)
