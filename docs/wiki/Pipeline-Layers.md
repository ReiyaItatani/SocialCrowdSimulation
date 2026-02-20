# Pipeline Layers

Each layer has an interface, a default implementation, and clearly defined input/output contracts. Layers live on child GameObjects of the agent prefab and are resolved via `GetComponentInChildren`.

---

## L1-2: Perception + Attention

**Interface**: `IPerceptionAttentionLayer`
**Default**: `DefaultPerceptionAttentionLayer`
**Location**: `Runtime/Core/Pipeline/Perception/`

### Purpose
Converts raw `GameObject` lists from `CollisionAvoidanceController` into `PerceivedAgent` structs. This is the **only layer that calls `GetComponent`** on neighboring agents.

### Input
| Parameter | Type | Source |
|-----------|------|--------|
| `frame` | `AgentFrame` | Current agent position, direction, speed |
| `sensors` | `SensorInput` | Raw FOV agents, avoidance area agents, wall/obstacle targets |
| `group` | `GroupContext` | Pre-resolved group data |

### Output: `AttentionOutput`
| Field | Description |
|-------|-------------|
| `VisibleAgents` | Agents in FOV, resolved as `PerceivedAgent` structs |
| `AvoidanceAreaAgents` | Agents in the immediate avoidance area |
| `UrgentAvoidanceTarget` | Most urgent collision target (closest predicted collision) |
| `PotentialAvoidanceTarget` | Target for anticipated collision avoidance |
| `UrgentTargetWeight` | Weight multiplier based on tag (group agents get higher weight) |
| `WallNormal` / `HasWall` | Resolved wall repulsion normal from `NormalVector` component |
| `ClosestObstacleNormal` / `HasObstacle` | Resolved obstacle normal |

### Key Logic
1. **ResolveAgentsInto**: Iterates `GameObject` list, calls `GetComponent<IParameterManager>()` on each, creates `PerceivedAgent` structs
2. **FindUrgentAvoidanceTarget**: Predicts time-to-nearest-approach for all visible agents, selects the most urgent one that is also in the avoidance area
3. **FindPotentialAvoidanceTarget**: For anticipated collision avoidance. Uses group-level shared FOV when in a group with active collider
4. **ResolveEnvironment**: Gets wall/obstacle normals from `NormalVector` components

---

## L3: Prediction

**Interface**: `IPredictionLayer`
**Default**: `DefaultPredictionLayer`
**Location**: `Runtime/Core/Pipeline/Prediction/`

### Purpose
Computes predicted future positions for perceived agents using linear extrapolation. Separates prediction math from force computation so prediction algorithms can be swapped independently.

### Input
| Parameter | Type | Source |
|-----------|------|--------|
| `attention` | `AttentionOutput` | From L1-2 |
| `frame` | `AgentFrame` | Current agent state |
| `group` | `GroupContext` | Group data (uses group frame when group collider active) |

### Output: `PredictionOutput`
| Field | Description |
|-------|-------------|
| `PredictedNeighbors` | List of `PredictedNeighbor` with future positions |
| `TimeToNearestCollision` | Time to the closest predicted collision |
| `MostUrgentNeighbor` | The neighbor with the nearest dangerous approach |

### Key Logic
- Uses `PredictionMath.PredictNearestApproachTime()` for linear extrapolation
- Computes both the agent's and neighbor's predicted positions at approach time
- When in a group with active collider, uses group-level direction/position/speed instead of individual values

---

## L4: Decision

**Interface**: `IDecisionLayer`
**Default**: `DefaultDecisionLayer`
**Location**: `Runtime/Core/Pipeline/Decision/`

### Purpose
Implements the **Social Force Model** — computes 6 weighted forces and combines them into a desired direction. This is where the force weight "sliders" from [Agent Manager](Agent-Manager.md) are applied.

### Input
| Parameter | Type | Source |
|-----------|------|--------|
| `input` | `DecisionInput` | Prediction + attention targets + wall/obstacle normals + goal |
| `frame` | `AgentFrame` | Current agent state |
| `weights` | `ForceWeights` | 6 force weight values |
| `group` | `GroupContext` | Group data |
| `deltaTime` | `float` | Frame delta time |

### Output: `DecisionOutput`
| Field | Description |
|-------|-------------|
| `DesiredDirection` | Weighted combined direction (normalized, Y=0) |
| `DesiredSpeed` | Desired speed from decision logic |
| `MutualAvoidanceDetected` | Whether agents are approaching each other in parallel |
| `MutualAvoidanceTarget` | Target involved in mutual avoidance (for gaze events) |
| Individual force vectors | `ToGoalForce`, `AvoidanceForce`, `AnticipatedCollisionForce`, `GroupForce`, `WallForce`, `ObstacleForce` (for debug visualization) |

### The 6 Forces

#### 1. ToGoal
Direction from current position to goal. Updated every 0.1s. Snaps immediately (no smooth transition).

#### 2. Avoidance (Urgent)
Triggered when an urgent avoidance target is detected. Computes a perpendicular avoidance vector using cross products. Includes:
- **Mutual avoidance detection**: When two agents approach each other in parallel, uses a reflection vector instead
- **Distance scaling**: Force scales inversely with distance to target
- **Tag weighting**: Group-tagged agents receive higher avoidance weight

Uses `TimedForce` with Lerp transition (interval: 0.1s, transition: 0.3s).

#### 3. Anticipated Collision
Steers to avoid predicted future collisions from L3 prediction output. Determines steer direction (left/right) based on:
- **Anti-parallel approach**: Both agents heading toward each other
- **Parallel approach**: Both heading the same direction (side dodge)
- **Angled approach**: Based on relative speed comparison

Suppressed when urgent avoidance is active. Uses `TimedForce` with Slerp transition.

#### 4. Group Force
Active only for group agents. Combines three sub-forces:
- **Cohesion** (weight: 2.0): Pulls toward center of mass when beyond threshold distance
- **Repulsion** (weight: 1.5): Pushes away from members that are too close
- **Alignment** (weight: 1.5): Aligns direction with group members

Uses `TimedForce` with Slerp transition (interval: 0.1s, transition: 0.1s).

#### 5. Wall Repulsion
Applied when a wall is detected. Uses the wall's surface normal (from `NormalVector` component, pre-resolved in L1-2). Set immediately when detected, fades via Slerp when wall leaves FOV.

#### 6. Obstacle Avoidance
Applied when an obstacle is detected. Uses the closest obstacle's surface normal. Transitions smoothly via `TimedForce` Slerp.

### TimedForce System
Each force (except ToGoal) uses a `TimedForce` struct that encapsulates:
- **Update interval**: How often the force is recomputed (e.g., 0.1s)
- **Transition duration**: How long the smooth transition takes (e.g., 0.3s)
- **Transition mode**: `Lerp` for linear, `Slerp` for spherical interpolation
- `SetImmediate()`: Snap to value (used for urgent forces)
- `SetTarget()`: Begin smooth transition to new value

---

## L5: Motor

**Interface**: `IMotorLayer`
**Default**: `DefaultMotorLayer`
**Location**: `Runtime/Core/Pipeline/Motor/`

### Purpose
Applies speed management and produces the final position/direction/speed for the animation system.

### Input
| Parameter | Type | Source |
|-----------|------|--------|
| `decision` | `DecisionOutput` | From L4 |
| `frame` | `AgentFrame` | Current agent state |
| `motor` | `MotorContext` | Goal position, animation state, speed config |
| `group` | `GroupContext` | Group data |
| `deltaTime` | `float` | Frame delta time |

### Output: `MotorOutput`
| Field | Description |
|-------|-------------|
| `NextPosition` | Final world position after motor constraints |
| `NextDirection` | Final movement direction |
| `ActualSpeed` | Speed after all constraints |
| `IsInSlowingArea` | Whether agent is within slowing radius of goal |

### Speed Management

#### Individual Speed
- Uses `InitialSpeed` as default
- Reduces to `MinSpeed` when animation state is `SmartPhone`
- Restores to `InitialSpeed` when switching back to `Walk` or `Talk`

#### Group Speed
- On first tick with group, computes group average speed (adjusted by `-0.1 * memberCount`)
- When distance to center of mass exceeds threshold:
  - Moving toward center: speed up (capped at `MaxSpeed`)
  - Moving away: slow down (floored at `MinSpeed`)
- When within threshold: use group average speed

#### Goal Slowing
- When within `SlowingRadius`: linearly interpolates speed from `MaxSpeed` to `MinSpeed`
- Uses `MaxSpeed` as reference (not current speed) to prevent exponential decay

#### Speed Transitions
When a path target is reached, `OnTargetReached()` triggers a smooth 0.5s speed transition from current speed back to initial speed.

---

Next: [Data Contracts](Data-Contracts.md) | [Group System](Group-System.md)
