# Animation and Gaze

Upper body animation states and gaze behavior, layered on top of Motion Matching locomotion.

---

## SocialBehaviour

<!-- TODO: images/social-behaviour-inspector.png — SocialBehaviour component in Inspector showing WalkAnimationProbability, audio, smartphone fields -->

### Animation States

<!-- TODO: images/animation-states.gif — Agent switching between Walk, Talk, and SmartPhone states -->

| State | Description | System |
|-------|-------------|--------|
| `Walk` | Walking activities (smoking, luggage) | Motion Matching |
| `Talk` | Conversation within a group | Unity Animator |
| `SmartPhone` | Texting, calling (reduces speed) | Unity Animator |

States switch randomly every 5-10 seconds. `WalkAnimationProbability` (0-1) controls Walk transition chance.

---

## Gaze Behavior

<!-- TODO: images/gaze-behavior.gif — Agent looking at group members, then shifting gaze to avoidance target -->

Gaze targets update every 1.5 seconds:

| Context | Target |
|---------|--------|
| **Group member** | Group center of mass |
| **Avoiding agent** | Avoidance target |
| **Mutual avoidance** | Eye contact before dodging |

---

## Pipeline Integration

```
SocialBehaviour.currentAnimationState
  → MotorContext.AnimationState
  → L5: speed adjustment (SmartPhone → MinSpeed)

L4: MutualAvoidanceDetected
  → SocialBehaviour: trigger mutual gaze
```

<!-- TODO: images/mutual-gaze.gif — Two agents making eye contact before dodging each other -->

---

## Configuration

| Parameter | |
|-----------|---|
| `WalkAnimationProbability` | 0-1, chance of Walk state |
| `audioSource` / `audioClips` | Conversation audio (Talk state) |
| `smartPhone` | Smartphone prop GameObject |

---

## Components

| Component | File | Role |
|-----------|------|------|
| `SocialBehaviour` | `Animation/SocialBehaviour.cs` | Animation state management |
| `GazeController` | `Animation/GazeController.cs` | IK-based head/eye tracking |
| `AnimationModifier` | `Animation/AnimationModifier.cs` | State transition blending |

---

Next: [Customization](Customization.md)
