# Customization

Each pipeline layer is defined by an interface. Implement it, attach to the correct child GameObject, and it works automatically.

---

## Interfaces

| Layer | Interface | Child GameObject |
|-------|-----------|-----------------|
| L1-2 | `IPerceptionAttentionLayer` | `Pipeline/PerceptionAttention/` |
| L3 | `IPredictionLayer` | `Pipeline/Prediction/` |
| L4 | `IDecisionLayer` | `Pipeline/Decision/` |
| L5 | `IMotorLayer` | `Pipeline/Motor/` |

## How to Swap

1. Open agent prefab → find `Pipeline/<LayerName>/`
2. Remove or disable the default component
3. Add your component

The coordinator resolves via `GetComponentInChildren<T>()` — no other changes needed.

---

## Example: Custom Decision Layer

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

Back to: [Pipeline](Pipeline.md) | [Home](Home.md)
