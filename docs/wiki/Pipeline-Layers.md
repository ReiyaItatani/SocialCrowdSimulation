# Pipeline Layers

Each layer: interface + default implementation + defined input/output. Resolved via `GetComponentInChildren`.

---

## L1-2: Perception + Attention

**Interface**: `IPerceptionAttentionLayer` · **Default**: `DefaultPerceptionAttentionLayer`

The **only layer that calls `GetComponent`** on neighbors.

| Input | → | Output (`AttentionOutput`) |
|-------|---|---------------------------|
| `SensorInput` (raw GameObjects) | | `VisibleAgents` (PerceivedAgent list) |
| `AgentFrame` | | `AvoidanceAreaAgents` |
| `GroupContext` | | `UrgentAvoidanceTarget` / `PotentialAvoidanceTarget` |
| | | `WallNormal` / `ClosestObstacleNormal` |

**Key steps:**
1. Resolve GameObjects → `PerceivedAgent` structs
2. Find urgent avoidance target (closest predicted collision in avoidance area)
3. Find potential avoidance target (anticipated collision)
4. Resolve wall/obstacle normals from `NormalVector` components

---

## L3: Prediction

**Interface**: `IPredictionLayer` · **Default**: `DefaultPredictionLayer`

Linear extrapolation of neighbor positions.

| Input | → | Output (`PredictionOutput`) |
|-------|---|----------------------------|
| `AttentionOutput` | | `PredictedNeighbors` (with future positions) |
| `AgentFrame` | | `TimeToNearestCollision` |
| `GroupContext` | | `MostUrgentNeighbor` |

When group collider is active → uses group-level position/direction/speed.

---

## L4: Decision

**Interface**: `IDecisionLayer` · **Default**: `DefaultDecisionLayer`

**Social Force Model** — 6 weighted forces combined into a desired direction.

| Input | → | Output (`DecisionOutput`) |
|-------|---|--------------------------|
| `DecisionInput` | | `DesiredDirection` (normalized) |
| `ForceWeights` (6 sliders) | | `DesiredSpeed` |
| `AgentFrame`, `GroupContext` | | `MutualAvoidanceDetected` |
| | | Individual force vectors (for debug) |

### The 6 Forces

| # | Force | Trigger | Transition |
|---|-------|---------|------------|
| 1 | **ToGoal** | Always | Immediate (no smooth) |
| 2 | **Avoidance** | Urgent target detected | Lerp (0.3s) |
| 3 | **Anticipated Collision** | Predicted future collision | Slerp |
| 4 | **Group** | Group agent only | Slerp (0.1s) |
| 5 | **Wall** | Wall in range | Slerp fade |
| 6 | **Obstacle** | Obstacle in FOV | Slerp |

#### Avoidance details
- Perpendicular avoidance vector via cross products
- **Mutual avoidance**: parallel approach → reflection vector
- Distance-scaled, tag-weighted

#### Group Force sub-forces
| Sub-Force | Weight | |
|-----------|--------|---|
| Cohesion | 2.0 | Pull toward center of mass |
| Repulsion | 1.5 | Push apart when too close |
| Alignment | 1.5 | Align direction with members |

---

## L5: Motor

**Interface**: `IMotorLayer` · **Default**: `DefaultMotorLayer`

Final position, direction, speed for the animation system.

| Input | → | Output (`MotorOutput`) |
|-------|---|----------------------|
| `DecisionOutput` | | `NextPosition` |
| `AgentFrame` | | `NextDirection` |
| `MotorContext` | | `ActualSpeed` |
| `GroupContext` | | `IsInSlowingArea` |

### Speed Management

| Condition | Behavior |
|-----------|----------|
| **Individual** | `InitialSpeed` default; `MinSpeed` during SmartPhone animation |
| **Group** | Average speed ± adjustment based on distance to center of mass |
| **Goal slowing** | Linear interpolation within `SlowingRadius` |
| **Path target reached** | Smooth 0.5s transition back to initial speed |

---

Next: [Data Contracts](Data-Contracts.md) | [Group System](Group-System.md)
