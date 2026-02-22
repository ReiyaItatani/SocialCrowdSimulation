# Pipeline

Each agent runs a **5-layer pipeline** every frame. All layers communicate via immutable `readonly struct` data.

---

## Overview

```mermaid
flowchart LR
    S["SensorInput"] --> L12["L1-2<br/>Perception +<br/>Attention"]
    L12 -->|AttentionOutput| L3["L3<br/>Prediction"]
    L3 -->|PredictionOutput| L4["L4<br/>Decision"]
    L4 -->|DecisionOutput| L5["L5<br/>Motor"]
    L5 -->|MotorOutput| MM["Motion Matching"]

    style L12 fill:#4a90d9,color:#fff
    style L3 fill:#6c8ebf,color:#fff
    style L4 fill:#e8a838,color:#fff
    style L5 fill:#7bc67e,color:#fff
```

All layers also receive **AgentFrame** (Position, Direction, Speed) and **GroupContext** (group membership info).

---

## Layer I/O

### L1-2: Perception + Attention

The only layer that reads Unity components from neighbors. Everything downstream is pure data.

```mermaid
flowchart LR
    subgraph Input
        SI["SensorInput<br/>· FOVAgents<br/>· AvoidanceAreaAgents<br/>· WallTarget<br/>· ObstaclesInFOV"]
    end
    subgraph Output
        AO["AttentionOutput<br/>· VisibleAgents<br/>· UrgentAvoidanceTarget<br/>· PotentialAvoidanceTarget<br/>· WallNormal<br/>· ClosestObstacleNormal"]
    end
    Input --> AO

    style SI fill:#e8f4fd,color:#000
    style AO fill:#d4edda,color:#000
```

---

### L3: Prediction

Linear extrapolation of neighbor positions to predict future collisions.

```mermaid
flowchart LR
    subgraph Input
        AI["AttentionOutput<br/>· VisibleAgents<br/>· AvoidanceTargets"]
    end
    subgraph Output
        PO["PredictionOutput<br/>· PredictedNeighbors<br/>· TimeToNearestCollision<br/>· MostUrgentNeighbor"]
    end
    Input --> PO

    style AI fill:#e8f4fd,color:#000
    style PO fill:#d4edda,color:#000
```

---

### L4: Decision

Combines **6 weighted forces** into a desired direction and speed.

```mermaid
flowchart LR
    subgraph Input
        DI["DecisionInput<br/>· PredictionOutput<br/>· AttentionTargets<br/>· Wall/Obstacle normals<br/>· GoalPosition"]
        FW["ForceWeights<br/>· ToGoal<br/>· Avoidance<br/>· AnticipatedCollision<br/>· Group<br/>· Wall<br/>· Obstacle"]
    end
    subgraph Output
        DO["DecisionOutput<br/>· DesiredDirection<br/>· DesiredSpeed<br/>· MutualAvoidanceDetected<br/>· 6 force vectors (debug)"]
    end
    DI --> DO
    FW --> DO

    style DI fill:#e8f4fd,color:#000
    style FW fill:#fff3cd,color:#000
    style DO fill:#d4edda,color:#000
```

ForceWeights are configured in [Agent Manager](Agent-Manager.md).

---

### L5: Motor

Applies speed limits, goal slowing, and group speed adjustments.

```mermaid
flowchart LR
    subgraph Input
        DEC["DecisionOutput<br/>· DesiredDirection<br/>· DesiredSpeed"]
        MC["MotorContext<br/>· GoalPosition<br/>· InitialSpeed / MinSpeed<br/>· SlowingRadius<br/>· AnimationState"]
    end
    subgraph Output
        MO["MotorOutput<br/>· NextPosition<br/>· NextDirection<br/>· ActualSpeed<br/>· IsInSlowingArea"]
    end
    DEC --> MO
    MC --> MO

    style DEC fill:#e8f4fd,color:#000
    style MC fill:#fff3cd,color:#000
    style MO fill:#d4edda,color:#000
```

---

## Interfaces

| Layer | Interface |
|-------|-----------|
| L1-2 | `IPerceptionAttentionLayer` |
| L3 | `IPredictionLayer` |
| L4 | `IDecisionLayer` |
| L5 | `IMotorLayer` |

Each layer can be swapped — see [Customization](Customization.md).

---

Back to: [Home](Home.md)
