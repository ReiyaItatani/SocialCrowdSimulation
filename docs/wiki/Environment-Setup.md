# Environment Setup: Walls and Obstacles

Walls and obstacles influence agent movement through repulsion forces computed in the L4 Decision layer.

## Adding Walls

1. Use the **Walls** prefab from `Sample/QuickStart/ForSetUpEnvironment/`
2. Place it in your scene
3. Ensure the GameObject has the **Wall** tag
4. The wall must have a `NormalVector` component — this computes the repulsion direction that pushes agents away

## Adding Obstacles

1. Use the **Obstacle** prefab from `Sample/QuickStart/ForSetUpEnvironment/`
2. Place it in your scene
3. Ensure the GameObject has the **Obstacle** tag
4. The obstacle must have a `NormalVector` component

## How NormalVector Works

`NormalVector` computes a repulsion vector from the wall/obstacle surface toward the agent:

1. The wall's forward direction (`transform.forward`) defines the wall orientation
2. The normal is computed perpendicular to the wall direction, pointing toward the agent
3. The magnitude is scaled inversely with distance — closer agents receive stronger repulsion

## Pipeline Integration

1. **CollisionAvoidanceController** detects walls (via `AgentCollisionDetection` trigger) and obstacles in the agent's field of view
2. **L1-2 (Perception + Attention)** resolves `NormalVector` components to get repulsion normals
3. **L4 (Decision)** applies wall/obstacle forces using the configured weights:
   - `wallRepForceWeight` (default: 0.3) — strength of wall repulsion
   - `avoidObstacleWeight` (default: 1.0) — strength of obstacle avoidance

These weights are configured via [Agent Manager](Agent-Manager.md).

## Required Tags

The following tags are automatically created when you click **Create AvatarCreator** in the [Scene Setup](Quick-Start.md):
- **Agent** — agent GameObjects
- **Group** — group collider GameObjects
- **Wall** — wall GameObjects
- **Object** — general objects
- **Obstacle** — obstacle GameObjects

---

Next: [Agent Manager](Agent-Manager.md)
