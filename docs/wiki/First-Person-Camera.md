# Adding a First-Person Camera Player

You can include a controllable first-person player in the simulation.

## Setup

1. Open **CollisionAvoidance > Social Crowd Simulation**
2. Expand the **Create Player** section
3. Assign **Motion Matching Data** — use the asset from `Sample/QuickStart/ForPlayerCreator/MotionMatchingData.asset`
4. Assign **Humanoid Avatar** — select a humanoid model (must have a Humanoid rig)
5. Click **Create Player**

### Generated Player Hierarchy

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

## Controls

- Move with **WASD** keys
- Uses the **SpringCharacterController** from the Motion Matching system
- For details on `SpringCharacterController`, see the [Motion Matching documentation](https://jlpm22.github.io/motionmatching-docs/basics/character_controller/)

<!-- TODO: screenshot/gif of player creation -->

---

Next: [Environment Setup](Environment-Setup.md)
