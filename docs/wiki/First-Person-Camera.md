# First-Person Camera

Add a controllable first-person player to the simulation.

---

## Setup

**CollisionAvoidance > Social Crowd Simulation** > **Create Player**

<!-- TODO: images/create-player-section.png — Create Player section of the unified window showing MMData and Avatar fields -->

1. Assign **Motion Matching Data** — use `Sample/QuickStart/ForPlayerCreator/MotionMatchingData.asset`
2. Assign **Humanoid Avatar** — any humanoid model
3. Click **Create Player**

<!-- TODO: images/create-player-result.png — Scene view or Hierarchy after player creation -->

---

## Generated Hierarchy

<!-- TODO: images/player-hierarchy.png — Unity Hierarchy showing the Player prefab structure -->

```
Player (tag="Agent")
  <ModelName>_PlayerInstance (tag="Agent")
    Rigidbody, CapsuleCollider
    MotionMatchingSkinnedMeshRenderer
    SpringParameterManager
    HeadCamera (Camera, parented to Head bone)
  MotionMatching
    MotionMatchingController
  CharacterController
    InputManager
    InputCharacterController
    SpringCharacterController
```

---

## Controls

| Key | Action |
|-----|--------|
| **WASD** | Move |

Uses `SpringCharacterController` from [Motion Matching](https://jlpm22.github.io/motionmatching-docs/basics/character_controller/).

<!-- TODO: images/first-person-view.gif — First-person view of walking among crowd agents -->

---

Next: [Environment Setup](Environment-Setup.md)
