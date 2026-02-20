# Architecture Overview

Each agent runs a **5-layer pipeline** every frame, orchestrated by `AgentPipelineCoordinator`. All inter-layer communication uses **immutable `readonly struct` data contracts**.

---

## Pipeline

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

---

## Design Principles

| Principle | |
|-----------|---|
| **Immutable Data** | All structs are `readonly`. No layer mutates another's output |
| **GetComponent Boundary** | Only L1-2 calls `GetComponent`. Downstream layers receive pure data |
| **Interface-Based** | Each layer implements an interface — swap any layer freely |
| **Coordinator Pattern** | Only `AgentPipelineCoordinator` touches concrete Unity components |

---

## Data Flow

```mermaid
flowchart TD
    subgraph "Input (Unity Components)"
        CAC["CollisionAvoidanceController<br/>FOV, Avoidance Area, Walls"]
        GM["GroupManager<br/>Shared FOV, Group Members"]
        APM["AgentPathManager<br/>Goal Position"]
    end

    COORD["AgentPipelineCoordinator<br/>Builds SensorInput, GroupContext, AgentFrame"]

    subgraph "Pipeline"
        L12["L1-2: Perception + Attention<br/>GameObject → PerceivedAgent"]
        L3["L3: Prediction<br/>Linear extrapolation"]
        L4["L4: Decision<br/>6 weighted forces"]
        L5["L5: Motor<br/>Speed + position constraints"]
    end

    subgraph "Output"
        APC["AgentPathController<br/>→ Motion Matching"]
    end

    CAC --> COORD
    GM --> COORD
    APM --> COORD
    COORD -->|SensorInput + GroupContext| L12
    L12 -->|AttentionOutput| L3
    L3 -->|PredictionOutput| L4
    L4 -->|DecisionOutput| L5
    L5 -->|MotorOutput| APC

    style COORD fill:#d97706,color:#fff
    style L12 fill:#4a90d9,color:#fff
    style L3 fill:#6c8ebf,color:#fff
    style L4 fill:#e8a838,color:#fff
    style L5 fill:#7bc67e,color:#fff
```

---

## Initialization

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

---

## Agent Prefab Hierarchy

<!-- TODO: images/agent-prefab-hierarchy.png — Unity Hierarchy showing Agent prefab with Pipeline children -->

```
Agent (tag="Agent")
  Avatar/
    Rigidbody, CapsuleCollider
    ParameterManager, SocialBehaviour, GazeController
    MotionMatchingSkinnedMeshRenderer
  Pipeline/
    AgentPathController, AgentPipelineCoordinator
    Navigation/          → AgentPathManager
    PerceptionAttention/ → CollisionAvoidanceController + DefaultPerceptionAttentionLayer
    Prediction/          → DefaultPredictionLayer
    Decision/            → DefaultDecisionLayer
    Motor/               → DefaultMotorLayer
  Animation/
    MotionMatchingController
```

---

## Key Files

| File | Role |
|------|------|
| `AgentPipelineCoordinator.cs` | Runs pipeline per tick |
| `IPipelineLayer.cs` | Interface definitions |
| `PipelineContracts.cs` | All `readonly struct` contracts |
| `AgentPathController.cs` | Bridges pipeline ↔ Motion Matching |
| `PredictionMath.cs` | Collision prediction math |
| `TimedForce.cs` | Smooth force transitions |

---

Next: [Pipeline Layers](Pipeline-Layers.md) | [Data Contracts](Data-Contracts.md)
