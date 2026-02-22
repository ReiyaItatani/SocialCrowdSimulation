# First-Person Camera

Add a controllable first-person player to the simulation.

---

## Setup

**CollisionAvoidance > Social Crowd Simulation** > **Create Player**

1. Assign **Motion Matching Data** — use `Sample/QuickStart/ForPlayerCreator/MotionMatchingData.asset`
2. Assign **Humanoid Avatar** — any humanoid model
3. Click **Create Player**

![Create Player](../../.github/media/create_player.gif)

---

## Generated Hierarchy

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

---

Next: [Environment Setup](Environment-Setup.md)
