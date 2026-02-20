# Data Contracts

All inter-layer data is defined as `readonly struct` in `PipelineContracts.cs`. These structs are immutable — once created, they cannot be modified. This guarantees predictable data flow through the pipeline.

---

## Input Structs (Built by Coordinator)

### SensorInput
Raw sensor data fed into L1-2. Built by `AgentPipelineCoordinator.BuildSensorInput()` from `CollisionAvoidanceController`.

```csharp
public readonly struct SensorInput
{
    public readonly List<GameObject> FOVAgents;
    public readonly List<GameObject> AvoidanceAreaAgents;
    public readonly GameObject SelfGameObject;
    public readonly List<GameObject> SharedFOVAgents;    // From GroupManager shared FOV
    public readonly GameObject WallTarget;               // Has NormalVector component
    public readonly List<GameObject> ObstaclesInFOV;     // Each has NormalVector component
    public readonly Vector3 AvoidanceColliderSize;
    public readonly float AgentColliderRadius;
}
```

### AgentFrame
Current per-agent state. Shared by all pipeline layers. Replaces repeated `(position, direction, speed)` triples.

```csharp
public readonly struct AgentFrame
{
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly float Speed;
}
```

### GroupContext
Pre-resolved group data. Built by `AgentPipelineCoordinator.BuildGroupContext()` from `GroupManager`. Returns `GroupContext.None` for individual (non-group) agents.

```csharp
public readonly struct GroupContext
{
    public readonly bool IsInGroup;
    public readonly bool IsGroupColliderActive;
    public readonly string GroupName;
    public readonly List<GroupMember> Members;       // Excluding self
    public readonly AgentFrame GroupFrame;            // From GroupParameterManager
    public readonly Vector3 CenterOfMass;            // Of members excluding self

    public static GroupContext None => ...;           // Default for individuals
}
```

### GroupMember
Pre-resolved group member data. Replaces `GetComponent<IParameterManager>` calls inside downstream layers.

```csharp
public readonly struct GroupMember
{
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly float Speed;
}
```

### ForceWeights
The 6 force weight "sliders". Set by `AgentManager` via `AgentPathController` properties.

```csharp
public readonly struct ForceWeights
{
    public readonly float ToGoal;
    public readonly float Avoidance;
    public readonly float AnticipatedCollision;
    public readonly float Group;
    public readonly float Wall;
    public readonly float Obstacle;
}
```

### MotorContext
Per-tick context for L5. Contains runtime configuration that changes per tick.

```csharp
public readonly struct MotorContext
{
    public readonly Vector3 GoalPosition;
    public readonly UpperBodyAnimationState AnimationState;
    public readonly float InitialSpeed;
    public readonly float MinSpeed;
    public readonly float MaxSpeed;
    public readonly float SlowingRadius;
}
```

### DecisionInput
Fully-resolved input for L4. Combines L3 prediction output with attention targets and environment normals. No `GetComponent` calls needed inside L4.

```csharp
public readonly struct DecisionInput
{
    public readonly PredictionOutput Prediction;
    public readonly PerceivedAgent? UrgentAvoidanceTarget;
    public readonly float UrgentTargetWeight;
    public readonly PerceivedAgent? PotentialAvoidanceTarget;
    public readonly Vector3 WallNormal;
    public readonly bool HasWall;
    public readonly Vector3 ClosestObstacleNormal;
    public readonly bool HasObstacle;
    public readonly Vector3 GoalPosition;
    public readonly Vector3 AvoidanceColliderSize;
    public readonly float AgentColliderRadius;
}
```

---

## Layer Output Structs

### PerceivedAgent (L1-2 internal)
A neighboring agent resolved from `GetComponent<IParameterManager>()`. Downstream layers never need to query Unity components.

```csharp
public readonly struct PerceivedAgent
{
    public readonly GameObject GameObject;
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly float Speed;
    public readonly Vector3 AvoidanceVector;
    public readonly string GroupName;
    public readonly bool IsSameGroup;
    public readonly float ColliderRadius;
    public readonly bool IsGroupTag;
    public readonly int InstanceId;
}
```

### AttentionOutput (L1-2 output)
```csharp
public readonly struct AttentionOutput
{
    public readonly List<PerceivedAgent> VisibleAgents;
    public readonly List<PerceivedAgent> AvoidanceAreaAgents;
    public readonly PerceivedAgent? UrgentAvoidanceTarget;
    public readonly PerceivedAgent? PotentialAvoidanceTarget;
    public readonly float UrgentTargetWeight;
    public readonly Vector3 WallNormal;
    public readonly bool HasWall;
    public readonly Vector3 ClosestObstacleNormal;
    public readonly bool HasObstacle;
    public readonly Vector3 AvoidanceColliderSize;
    public readonly float AgentColliderRadius;
}
```

### PredictedNeighbor (L3 internal)
```csharp
public readonly struct PredictedNeighbor
{
    public readonly PerceivedAgent Agent;
    public readonly Vector3 PredictedPosition;
    public readonly Vector3 MyPositionAtApproach;
    public readonly float TimeToApproach;
    public readonly float DistanceAtApproach;
}
```

### PredictionOutput (L3 output)
```csharp
public readonly struct PredictionOutput
{
    public readonly List<PredictedNeighbor> PredictedNeighbors;
    public readonly float TimeToNearestCollision;
    public readonly PredictedNeighbor? MostUrgentNeighbor;
}
```

### DecisionOutput (L4 output)
```csharp
public readonly struct DecisionOutput
{
    public readonly Vector3 DesiredDirection;
    public readonly float DesiredSpeed;
    public readonly bool MutualAvoidanceDetected;
    public readonly GameObject MutualAvoidanceTarget;
    // Debug vectors:
    public readonly Vector3 ToGoalForce;
    public readonly Vector3 AvoidanceForce;
    public readonly Vector3 AnticipatedCollisionForce;
    public readonly Vector3 GroupForce;
    public readonly Vector3 WallForce;
    public readonly Vector3 ObstacleForce;
}
```

### MotorOutput (L5 output)
```csharp
public readonly struct MotorOutput
{
    public readonly Vector3 NextPosition;
    public readonly Vector3 NextDirection;
    public readonly float ActualSpeed;
    public readonly bool IsInSlowingArea;
}
```

---

## Data Flow Summary

```
Coordinator builds:
  SensorInput  ──→  L1-2
  GroupContext  ──→  L1-2, L3, L4, L5
  AgentFrame   ──→  L1-2, L3, L4, L5

L1-2 produces:
  AttentionOutput  ──→  L3, Coordinator

Coordinator builds:
  DecisionInput (from AttentionOutput + PredictionOutput + goal)  ──→  L4

L3 produces:
  PredictionOutput  ──→  Coordinator

L4 produces:
  DecisionOutput  ──→  L5

L5 produces:
  MotorOutput  ──→  AgentPathController
```

---

Next: [Group System](Group-System.md) | [Customization](Customization.md)
