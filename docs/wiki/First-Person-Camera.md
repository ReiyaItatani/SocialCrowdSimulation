# Adding a First-Person Camera Player

You can include a controllable first-person player in the simulation.

## Setup

1. Open the **Create Player** window from the **CollisionAvoidance** tab
2. In **Motion Matching Data**, select a `.asset` file from `Packages/SocialCrowdSimulation/Sample/QuickStart/ForPlayerCreator/MotionMatchingData`
3. For **Humanoid Avatar**, choose a humanoid rig
4. Click **CreatePlayer** to generate the player in your scene

## Controls

- Move with **WASD** keys
- Uses the **SpringCharacterController** from the Motion Matching system
- For details on `SpringCharacterController`, see the [Motion Matching documentation](https://jlpm22.github.io/motionmatching-docs/basics/character_controller/)

![Create Player](../../.github/media/create_player.gif)

---

Next: [Environment Setup](Environment-Setup.md)
