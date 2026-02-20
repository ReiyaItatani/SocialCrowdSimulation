# Environment Setup: Walls and Obstacles

Walls and obstacles influence agent movement through repulsion forces computed in the pipeline's L4 Decision layer.

## Adding Walls

1. Use the **Wall** prefab from `Packages/SocialCrowdSimulation/Sample/QuickStart/ForSetUpEnvironment`
2. Place it in your scene
3. Ensure the GameObject has the **Wall** tag
4. The wall must have a `NormalVector` component — this computes the repulsion normal that pushes agents away

## Adding Obstacles

1. Use the **Obstacle** prefab from `Packages/SocialCrowdSimulation/Sample/QuickStart/ForSetUpEnvironment`
2. Place it in your scene
3. Ensure the GameObject has the **Obstacle** tag
4. The obstacle must have a `NormalVector` component

## How It Works

The pipeline handles walls and obstacles as follows:

1. **CollisionAvoidanceController** detects walls/obstacles in the agent's field of view
2. **L1-2 (Perception + Attention)** resolves `NormalVector` components to get repulsion normals
3. **L4 (Decision)** applies wall/obstacle forces using the configured weights:
   - `wallRepForceWeight` — strength of wall repulsion
   - `avoidObstacleWeight` — strength of obstacle avoidance

These weights are configured via [Agent Manager](Agent-Manager.md).

## Tags

The following tags are automatically created when you use **Create AvatarCreator**:
- **Agent** — applied to agent GameObjects
- **Group** — applied to group collider GameObjects
- **Wall** — applied to wall GameObjects
- **Obstacle** — applied to obstacle GameObjects

---

Next: [Agent Manager](Agent-Manager.md)
