# Environment Setup

Walls and obstacles create repulsion forces that push agents away.

---

## Adding Walls

Use the **Walls** prefab from `Sample/QuickStart/ForSetUpEnvironment/`.

<!-- TODO: images/wall-prefab.png — Wall prefab in Project window -->

<!-- TODO: images/wall-scene.png — Scene view with wall placed, showing agents being repelled -->

| Requirement | |
|-------------|---|
| **Tag** | `Wall` |
| **Component** | `NormalVector` (computes repulsion direction) |

---

## Adding Obstacles

Use the **Obstacle** prefab from `Sample/QuickStart/ForSetUpEnvironment/`.

<!-- TODO: images/obstacle-prefab.png — Obstacle prefab in Project window -->

<!-- TODO: images/obstacle-scene.png — Scene view with obstacle, agents avoiding it -->

| Requirement | |
|-------------|---|
| **Tag** | `Obstacle` |
| **Component** | `NormalVector` |

---

## How NormalVector Works

<!-- TODO: images/normal-vector-diagram.png — Diagram showing wall surface, normal direction pointing toward agent, distance-based scaling -->

`NormalVector` computes a repulsion vector from the wall surface toward the agent:
- Normal is perpendicular to wall direction, pointing toward agent
- Magnitude scales inversely with distance (closer = stronger)

---

## Force Weights

Configured in [Agent Manager](Agent-Manager.md):

| Force | Default | |
|-------|---------|-|
| `wallRepForceWeight` | 0.3 | Wall repulsion strength |
| `avoidObstacleWeight` | 1.0 | Obstacle avoidance strength |

---

## Required Tags

Auto-created by **Scene Setup > Create AvatarCreator**:

`Agent` · `Group` · `Wall` · `Object` · `Obstacle`

---

Next: [Agent Manager](Agent-Manager.md)
