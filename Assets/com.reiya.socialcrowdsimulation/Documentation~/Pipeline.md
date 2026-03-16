# Pipeline

Each agent runs a **5-layer pipeline** every frame. All layers communicate via immutable `readonly struct` data.

## Why This Design

This pipeline mirrors how humans actually navigate crowds:

1. **Perceive** — see who is around you (L1-2)
2. **Predict** — anticipate where they are heading (L3)
3. **Decide** — choose which direction to go (L4)
4. **Act** — move your body (L5)

Each layer corresponds to a stage of this cognitive process. Because each stage is a separate interface, you can replace any one algorithm (e.g., swap the decision model from Social Force to ORCA) without affecting the others.

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

ForceWeights are configured in [Agent Manager](Agent-Manager).

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

## Customization

Each layer is defined by an interface. Implement it, attach to the correct child GameObject, and it works automatically.

| Layer | Interface | Child GameObject |
|-------|-----------|-----------------|
| L1-2 | `IPerceptionAttentionLayer` | `Pipeline/PerceptionAttention/` |
| L3 | `IPredictionLayer` | `Pipeline/Prediction/` |
| L4 | `IDecisionLayer` | `Pipeline/Decision/` |
| L5 | `IMotorLayer` | `Pipeline/Motor/` |

### How to Swap

1. Open agent prefab → find `Pipeline/<LayerName>/`
2. Remove or disable the default component
3. Add your component

The coordinator resolves via `GetComponentInChildren<T>()` — no other changes needed.

### Example: Custom Decision Layer

```csharp
public class ORCADecisionLayer : MonoBehaviour, IDecisionLayer
{
    public DecisionOutput Tick(DecisionInput input, AgentFrame frame,
        ForceWeights weights, GroupContext group, float deltaTime)
    {
        Vector3 desiredDirection = ComputeORCA(input, frame, weights);

        return new DecisionOutput(
            desiredDirection, frame.Speed,
            false, null,
            Vector3.zero, Vector3.zero, Vector3.zero,
            Vector3.zero, Vector3.zero, Vector3.zero
        );
    }
}
```

Set debug force vectors to `Vector3.zero` if not needed.

---

Next: [Agent Manager](Agent-Manager)
