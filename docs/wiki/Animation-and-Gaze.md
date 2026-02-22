# Animation and Gaze

Upper body animation states and gaze behavior, layered on top of Motion Matching locomotion.

---

## SocialBehaviour

### Animation States

| State | Description | System |
|-------|-------------|--------|
| `Walk` | Walking activities (smoking, luggage) | Motion Matching |
| `Talk` | Conversation within a group | Unity Animator |
| `SmartPhone` | Texting, calling (reduces speed) | Unity Animator |

States switch randomly every 5-10 seconds. `WalkAnimationProbability` (0-1) controls Walk transition chance.

---

## Gaze Behavior

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
