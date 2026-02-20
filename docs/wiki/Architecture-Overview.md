# Architecture Overview

The steering system uses a **layered pipeline** architecture. Each agent runs a 5-layer Perception-to-Action pipeline every frame, orchestrated by `AgentPipelineCoordinator`. All inter-layer communication uses **immutable `readonly struct` data contracts**.

## Pipeline Overview

```mermaid
flowchart LR
    subgraph "Per-Agent Pipeline"
        L12["L1-2<br/>Perception + Attention"]
        L3["L3<br/>Prediction"]
        L4["L4<br/>Decision"]
        L5["L5<br/>Motor"]
    end

    Sensor["SensorInput"] --> L12
    L12 -->|AttentionOutput| L3
    L3 -->|PredictionOutput| L4
    L4 -->|DecisionOutput| L5
    L5 -->|MotorOutput| APC["AgentPathController"]

    style L12 fill:#4a90d9,color:#fff
    style L3 fill:#6c8ebf,color:#fff
    style L4 fill:#e8a838,color:#fff
    style L5 fill:#7bc67e,color:#fff
    style APC fill:#c9a0dc,color:#fff
```

## Design Principles

### Immutable Data Flow
Every struct passed between layers is `readonly`. No layer can mutate another layer's output. This makes the data flow predictable and easy to debug.

### GetComponent Boundary
Only **L1-2** calls `GetComponent` on neighboring agents. It resolves raw `GameObject` lists into `PerceivedAgent` structs. Downstream layers (L3, L4, L5) receive pure data — no Unity API calls needed. This makes them easy to unit test and swap.

### Interface-Based Layers
Each layer implements an interface:
- `IPerceptionAttentionLayer` (L1-2)
- `IPredictionLayer` (L3)
- `IDecisionLayer` (L4)
- `IMotorLayer` (L5)

You can replace any layer with a custom implementation. See [Customization](Customization.md).

### Coordinator Pattern
`AgentPipelineCoordinator` is the only component that touches concrete Unity components (CollisionAvoidanceController, GroupManager, NormalVector). It builds input structs and passes them through the pipeline. No layer has direct references to Unity MonoBehaviour components.

## Full Data Flow

```mermaid
flowchart TD
    subgraph "Input (Concrete Unity Components)"
        CAC["CollisionAvoidanceController<br/>FOV, Avoidance Area, Walls"]
        GM["GroupManager<br/>Shared FOV, Group Members"]
        APM["AgentPathManager<br/>Goal Position"]
    end

    COORD["AgentPipelineCoordinator<br/>Builds SensorInput, GroupContext, AgentFrame"]

    subgraph "Pipeline Layers"
        L12["L1-2: DefaultPerceptionAttentionLayer<br/>Resolves GameObjects to PerceivedAgent structs"]
        L3["L3: DefaultPredictionLayer<br/>Linear extrapolation of neighbor positions"]
        L4["L4: DefaultDecisionLayer<br/>6 weighted forces (Social Force Model)"]
        L5["L5: DefaultMotorLayer<br/>Speed management, goal slowing"]
    end

    subgraph "Output"
        APC["AgentPathController<br/>Syncs to Motion Matching trajectory"]
        AS["AgentState<br/>Backward-compat sync"]
    end

    CAC --> COORD
    GM --> COORD
    APM --> COORD
    COORD -->|SensorInput + GroupContext| L12
    L12 -->|AttentionOutput| L3
    L3 -->|PredictionOutput| L4
    L4 -->|DecisionOutput| L5
    L5 -->|MotorOutput| APC
    APC -->|sync each frame| AS

    style COORD fill:#d97706,color:#fff
    style L12 fill:#4a90d9,color:#fff
    style L3 fill:#6c8ebf,color:#fff
    style L4 fill:#e8a838,color:#fff
    style L5 fill:#7bc67e,color:#fff
```

## Initialization Order

```mermaid
sequenceDiagram
    participant Unity
    participant APC as AgentPathController
    participant COORD as AgentPipelineCoordinator

    Unity->>APC: Awake()
    APC->>COORD: Initialize(collisionAvoidance, groupManager, ...)
    COORD->>COORD: GetComponentInChildren for L1-2, L3, L4, L5
    COORD->>COORD: motorLayer.Initialize(initialSpeed)
    Unity->>APC: OnUpdate() (every frame)
    APC->>APC: Build AgentFrame + ForceWeights
    APC->>COORD: Tick(frame, deltaTime, weights)
    COORD->>COORD: BuildSensorInput() + BuildGroupContext()
    COORD->>COORD: L1-2 → L3 → L4 → L5
    COORD-->>APC: MotorOutput
    APC->>APC: Sync to AgentState + Motion Matching
```

## Agent Prefab Hierarchy

![Agent Prefab Hierarchy](images/agent-prefab-hierarchy.png)

Each layer lives on its own child GameObject under `Pipeline/`:
- **Navigation** — `AgentPathManager`
- **PerceptionAttention** — `CollisionAvoidanceController` + `DefaultPerceptionAttentionLayer`
- **Prediction** — `DefaultPredictionLayer`
- **Decision** — `DefaultDecisionLayer`
- **Motor** — `DefaultMotorLayer`

## Key Files

| File | Role |
|------|------|
| `AgentPipelineCoordinator.cs` | Orchestrator — runs pipeline per tick |
| `IPipelineLayer.cs` | Interface definitions for all 4 layers |
| `PipelineContracts.cs` | All `readonly struct` data contracts |
| `AgentPathController.cs` | Per-agent driver, bridges pipeline with Motion Matching |
| `BasePathController.cs` | Motion Matching integration base class |
| `PredictionMath.cs` | Shared collision prediction math |
| `TimedForce.cs` | Smooth force transition utility |

---

Next: [Pipeline Layers](Pipeline-Layers.md) | [Data Contracts](Data-Contracts.md)
