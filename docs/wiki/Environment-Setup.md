# Environment Setup

Walls and obstacles create repulsion forces that push agents away.

---

## Adding Walls

Use the **Walls** prefab from `Sample/QuickStart/ForSetUpEnvironment/`.

| Requirement | |
|-------------|---|
| **Tag** | `Wall` |
| **Component** | `NormalVector` (computes repulsion direction) |

---

## Adding Obstacles

Use the **Obstacle** prefab from `Sample/QuickStart/ForSetUpEnvironment/`.

| Requirement | |
|-------------|---|
| **Tag** | `Obstacle` |
| **Component** | `NormalVector` |

---

## How NormalVector Works

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
