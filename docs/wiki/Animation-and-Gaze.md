# Animation and Gaze

The animation system controls upper body states and gaze behavior, layered on top of the Motion Matching locomotion.

## SocialBehaviour

**File**: `Runtime/Core/Animation/SocialBehaviour.cs`

`SocialBehaviour` manages the social animation layer for each agent.

### Animation States

```csharp
public enum UpperBodyAnimationState
{
    Walk,       // Walking activities (smoking, carrying luggage) — uses Motion Matching
    Talk,       // Conversation within a group — uses Unity Animator
    SmartPhone  // Individual activities (texting, calling) — uses Unity Animator
}
```

- Animation states switch periodically (every 5-10 seconds, randomized)
- `WalkAnimationProbability` (0-1) controls the chance of transitioning to Walk vs other states
- **SmartPhone** reduces agent speed to `MinSpeed` (handled by L5 Motor layer)

### Gaze Behavior

Gaze targets are updated every 1.5 seconds. The gaze system selects targets based on context:

#### Group Members (in a group)
- Agents look at the group center of mass
- Enables natural "looking at each other while walking" behavior

#### Avoidance Targets
- When avoiding another agent, the gaze shifts toward the avoidance target
- Creates realistic awareness behavior during collision avoidance

#### Mutual Gaze Detection
- When `DecisionOutput.MutualAvoidanceDetected` is true, the agent looks at the `MutualAvoidanceTarget`
- This creates the natural "making eye contact before dodging" behavior

### Configuration

| Parameter | Description |
|-----------|-------------|
| `WalkAnimationProbability` | Probability (0-1) of choosing Walk state during random transitions |
| `audioSource` / `audioClips` | Audio for conversation (Talk state) |
| `smartPhone` | GameObject reference for smartphone prop |

### Facial Expressions
Facial expression support requires either:
- **Microsoft Rocket Box Avatar** — set via CollisionAvoidance > Target Framework > RocketBox Avatar
- **Avatar SDK** — set via CollisionAvoidance > Target Framework > Avatar SDK

## GazeController

**File**: `Runtime/Core/Animation/GazeController.cs`

Handles the IK-based head/eye tracking for gaze targets set by `SocialBehaviour`.

## AnimationModifier

**File**: `Runtime/Core/Animation/AnimationModifier.cs`

Modifies animation state transitions and blending.

## Integration with Pipeline

The animation system integrates with the pipeline at two points:

1. **L5 (Motor)**: `MotorContext.AnimationState` is read from `SocialBehaviour.currentAnimationState`. The motor layer adjusts speed based on animation state (e.g., SmartPhone → MinSpeed).

2. **L4 (Decision)**: `DecisionOutput.MutualAvoidanceDetected` and `MutualAvoidanceTarget` are read by `SocialBehaviour` to trigger mutual gaze events.

```
SocialBehaviour.currentAnimationState
    → AgentPathController.getUpperBodyAnimationState()
    → AgentPipelineCoordinator.BuildMotorContext()
    → L5 Motor: speed adjustment

L4 Decision: MutualAvoidanceDetected
    → AgentPathController.LastDecision
    → SocialBehaviour: trigger mutual gaze
```

---

Next: [Customization](Customization.md)
