# Customization

The pipeline architecture is designed for easy layer replacement. Each layer is defined by an interface — implement the interface, attach it to the correct child GameObject, and the coordinator will use it automatically.

## Swapping a Pipeline Layer

### Step 1: Choose the layer to replace

| Layer | Interface | What it does |
|-------|-----------|-------------|
| L1-2 | `IPerceptionAttentionLayer` | Perception and attention filtering |
| L3 | `IPredictionLayer` | Neighbor position prediction |
| L4 | `IDecisionLayer` | Force computation / steering model |
| L5 | `IMotorLayer` | Speed and position constraints |

### Step 2: Implement the interface

Example — replacing the Social Force Model (L4) with ORCA:

```csharp
using UnityEngine;

namespace CollisionAvoidance
{
    public class ORCADecisionLayer : MonoBehaviour, IDecisionLayer
    {
        public DecisionOutput Tick(DecisionInput input, AgentFrame frame,
            ForceWeights weights, GroupContext group, float deltaTime)
        {
            // Your ORCA implementation here
            Vector3 desiredDirection = ComputeORCA(input, frame, weights);

            return new DecisionOutput(
                desiredDirection,
                frame.Speed,
                false,  // mutualAvoidanceDetected
                null,   // mutualAvoidanceTarget
                Vector3.zero, Vector3.zero, Vector3.zero,
                Vector3.zero, Vector3.zero, Vector3.zero
            );
        }

        private Vector3 ComputeORCA(DecisionInput input, AgentFrame frame,
            ForceWeights weights)
        {
            // Use input.Prediction.PredictedNeighbors for neighbor data
            // Use input.GoalPosition for goal
            // Use input.WallNormal / input.ClosestObstacleNormal for environment
            // ...
            return (input.GoalPosition - frame.Position).normalized;
        }
    }
}
```

### Step 3: Attach to the agent prefab

1. Open the agent prefab
2. Find the child GameObject for the layer (e.g., `Decision/`)
3. Remove or disable `DefaultDecisionLayer`
4. Add your `ORCADecisionLayer` component

The coordinator resolves layers via `GetComponentInChildren<IDecisionLayer>()`, so it will automatically find your new implementation.

### Step 4: No other changes needed

Other layers are unaffected because they only communicate through data contracts (`DecisionInput` → `DecisionOutput`).

## Replacing the Prediction Model (L3)

Example — polynomial prediction instead of linear extrapolation:

```csharp
public class PolynomialPredictionLayer : MonoBehaviour, IPredictionLayer
{
    public PredictionOutput Tick(AttentionOutput attention, AgentFrame frame,
        GroupContext group)
    {
        List<PredictedNeighbor> predicted = new List<PredictedNeighbor>();
        // Your polynomial prediction logic using attention.VisibleAgents
        // ...
        return new PredictionOutput(predicted, nearestTime, mostUrgent);
    }
}
```

## Adding a Custom Perception Filter (L1-2)

To change how agents are perceived (e.g., add occlusion, change FOV angle):

```csharp
public class OcclusionPerceptionLayer : MonoBehaviour, IPerceptionAttentionLayer
{
    public AttentionOutput Tick(AgentFrame frame, SensorInput sensors,
        GroupContext group)
    {
        // Filter sensors.FOVAgents with raycasting for occlusion
        // Resolve remaining agents into PerceivedAgent structs
        // ...
    }
}
```

## Custom Motor Layer (L5)

To change speed behavior (e.g., acceleration curves, energy-based movement):

```csharp
public class EnergyMotorLayer : MonoBehaviour, IMotorLayer
{
    private float energy = 100f;
    private float currentSpeed;

    public void Initialize(float initialSpeed)
    {
        currentSpeed = initialSpeed;
    }

    public MotorOutput Tick(DecisionOutput decision, AgentFrame frame,
        MotorContext motor, GroupContext group, float deltaTime)
    {
        // Energy-based speed management
        energy -= currentSpeed * deltaTime;
        if (energy < 20f) currentSpeed *= 0.8f;

        Vector3 nextPos = frame.Position + decision.DesiredDirection * currentSpeed * deltaTime;
        return new MotorOutput(nextPos, decision.DesiredDirection, currentSpeed, false);
    }
}
```

## Agent Prefab Hierarchy

![Agent Prefab Hierarchy](images/agent-prefab-hierarchy.png)

Each layer lives on its own child GameObject under `Pipeline/`:

| Child GameObject | Default Component | Replace with |
|-----------------|-------------------|-------------|
| `Navigation/` | `AgentPathManager` | — |
| `PerceptionAttention/` | `DefaultPerceptionAttentionLayer` | Your `IPerceptionAttentionLayer` |
| `Prediction/` | `DefaultPredictionLayer` | Your `IPredictionLayer` |
| `Decision/` | `DefaultDecisionLayer` | Your `IDecisionLayer` |
| `Motor/` | `DefaultMotorLayer` | Your `IMotorLayer` |

## Tips

- **Read the data contracts**: Before implementing a layer, review [Data Contracts](Data-Contracts.md) to understand exactly what data you receive and must produce
- **Use PredictionMath**: The `PredictionMath` static class provides shared collision prediction utilities that you can reuse in custom layers
- **Debug visualization**: `DecisionOutput` includes individual force vectors specifically for `AgentDebugGizmos`. If your custom L4 doesn't produce force vectors, set them to `Vector3.zero`
- **Group handling**: Check `GroupContext.IsInGroup` and `GroupContext.IsGroupColliderActive` to handle group agents correctly. When the group collider is active, you should use `GroupContext.GroupFrame` instead of the individual `AgentFrame` for some calculations

---

Back to: [Architecture Overview](Architecture-Overview.md) | [Home](Home.md)
