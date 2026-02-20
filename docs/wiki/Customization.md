# Customization

Each pipeline layer is defined by an interface. Implement it, attach to the correct child GameObject, and the coordinator uses it automatically.

---

## Swapping a Layer

<!-- TODO: images/layer-swap-prefab.png — Agent prefab Hierarchy showing Pipeline children with one custom component highlighted -->

### 1. Choose the layer

| Layer | Interface | Child GameObject |
|-------|-----------|-----------------|
| L1-2 | `IPerceptionAttentionLayer` | `PerceptionAttention/` |
| L3 | `IPredictionLayer` | `Prediction/` |
| L4 | `IDecisionLayer` | `Decision/` |
| L5 | `IMotorLayer` | `Motor/` |

### 2. Implement the interface

### 3. Replace on prefab

1. Open agent prefab → find `Pipeline/<LayerName>/`
2. Remove or disable the default component
3. Add your component

The coordinator resolves via `GetComponentInChildren<IDecisionLayer>()` — no other changes needed.

---

## Example: Custom Decision Layer (ORCA)

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

## Example: Custom Prediction Layer

```csharp
public class PolynomialPredictionLayer : MonoBehaviour, IPredictionLayer
{
    public PredictionOutput Tick(AttentionOutput attention, AgentFrame frame,
        GroupContext group)
    {
        // Your prediction logic using attention.VisibleAgents
        return new PredictionOutput(predicted, nearestTime, mostUrgent);
    }
}
```

## Example: Custom Perception Layer

```csharp
public class OcclusionPerceptionLayer : MonoBehaviour, IPerceptionAttentionLayer
{
    public AttentionOutput Tick(AgentFrame frame, SensorInput sensors,
        GroupContext group)
    {
        // Raycast-based occlusion filtering
    }
}
```

## Example: Custom Motor Layer

```csharp
public class EnergyMotorLayer : MonoBehaviour, IMotorLayer
{
    private float energy = 100f;
    private float currentSpeed;

    public void Initialize(float initialSpeed) { currentSpeed = initialSpeed; }

    public MotorOutput Tick(DecisionOutput decision, AgentFrame frame,
        MotorContext motor, GroupContext group, float deltaTime)
    {
        energy -= currentSpeed * deltaTime;
        if (energy < 20f) currentSpeed *= 0.8f;
        Vector3 nextPos = frame.Position + decision.DesiredDirection * currentSpeed * deltaTime;
        return new MotorOutput(nextPos, decision.DesiredDirection, currentSpeed, false);
    }
}
```

---

## Tips

- Review [Data Contracts](Data-Contracts.md) before implementing
- Use `PredictionMath` for shared collision math
- `DecisionOutput` debug vectors → set to `Vector3.zero` if not needed
- Check `GroupContext.IsInGroup` / `IsGroupColliderActive` for group handling

---

Back to: [Architecture Overview](Architecture-Overview.md) | [Home](Home.md)
