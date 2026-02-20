# Agent Manager

Centralized parameter panel on the **AvatarCreator** GameObject. Changes apply to all agents in real-time.

<!-- TODO: images/agent-manager-inspector.png — Full AgentManager Inspector showing all sections -->

---

## Force Weights

<!-- TODO: images/force-weights.png — Force Weights section of AgentManager Inspector -->

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

<!-- TODO: images/goal-parameters.png — Goal Parameters section of AgentManager Inspector -->

| Parameter | Default | Range |
|-----------|---------|-------|
| `goalRadius` | 2.0 | 0.1 - 5.0 |
| `slowingRadius` | 3.0 | 0.1 - 5.0 |

---

## Motion Matching Parameters

<!-- TODO: images/mm-parameters.png — Motion Matching Parameters section -->

| Parameter | Default |
|-----------|---------|
| `MaxDistanceMMAndCharacterController` | 0.1 |
| `PositionAdjustmentHalflife` | 0.1 |
| `PosMaximumAdjustmentRatio` | 0.1 |

---

## Debug Gizmos

<!-- TODO: images/debug-gizmos-toggles.png — Both PathController and MotionMatching gizmo toggle sections -->

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

<!-- TODO: images/debug-gizmos-scene.png — Scene view showing colored force vectors on agents -->

---

## Save / Load Settings

<!-- TODO: images/save-load-buttons.png — Save Settings / Load Settings buttons in Inspector -->

Export/import all parameters as JSON via the Inspector buttons.

---

Next: [Architecture Overview](Architecture-Overview.md)
