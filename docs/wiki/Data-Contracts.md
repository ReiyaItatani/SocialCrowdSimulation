# Data Contracts

All inter-layer data: `readonly struct` in `PipelineContracts.cs`. Immutable — once created, cannot be modified.

---

## Data Flow

```
Coordinator builds:  SensorInput, GroupContext, AgentFrame  →  all layers
L1-2 produces:       AttentionOutput                       →  L3, Coordinator
Coordinator builds:  DecisionInput                         →  L4
L3 produces:         PredictionOutput                      →  Coordinator
L4 produces:         DecisionOutput                        →  L5
L5 produces:         MotorOutput                           →  AgentPathController
```

---

## Input Structs (Built by Coordinator)

### SensorInput
```csharp
public readonly struct SensorInput
{
    public readonly List<GameObject> FOVAgents;
    public readonly List<GameObject> AvoidanceAreaAgents;
    public readonly GameObject SelfGameObject;
    public readonly List<GameObject> SharedFOVAgents;
    public readonly GameObject WallTarget;
    public readonly List<GameObject> ObstaclesInFOV;
    public readonly Vector3 AvoidanceColliderSize;
    public readonly float AgentColliderRadius;
}
```

### AgentFrame
```csharp
public readonly struct AgentFrame
{
    public readonly Vector3 Position;
    public readonly Vector3 Direction;
    public readonly float Speed;
}
```

### GroupContext
```csharp
public readonly struct GroupContext
{
    public readonly bool IsInGroup;
    public readonly bool IsGroupColliderActive;
    public readonly string GroupName;
    public readonly List<GroupMember> Members;    // Excluding self
    public readonly AgentFrame GroupFrame;
    public readonly Vector3 CenterOfMass;

    public static GroupContext None => ...;
}
```

### ForceWeights
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

### PerceivedAgent (L1-2)
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

### AttentionOutput (L1-2 →)
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

### PredictedNeighbor (L3)
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

### PredictionOutput (L3 →)
```csharp
public readonly struct PredictionOutput
{
    public readonly List<PredictedNeighbor> PredictedNeighbors;
    public readonly float TimeToNearestCollision;
    public readonly PredictedNeighbor? MostUrgentNeighbor;
}
```

### DecisionOutput (L4 →)
```csharp
public readonly struct DecisionOutput
{
    public readonly Vector3 DesiredDirection;
    public readonly float DesiredSpeed;
    public readonly bool MutualAvoidanceDetected;
    public readonly GameObject MutualAvoidanceTarget;
    // Debug:
    public readonly Vector3 ToGoalForce;
    public readonly Vector3 AvoidanceForce;
    public readonly Vector3 AnticipatedCollisionForce;
    public readonly Vector3 GroupForce;
    public readonly Vector3 WallForce;
    public readonly Vector3 ObstacleForce;
}
```

### MotorOutput (L5 →)
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

Next: [Group System](Group-System.md) | [Customization](Customization.md)
