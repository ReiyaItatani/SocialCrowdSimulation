# Group System

Groups: agents walking together with shared perception, coordinated speed, and formation forces.

<!-- TODO: images/group-walking.gif — Group of 2-3 agents walking together, maintaining formation -->

---

## Inheritance Chain

```
GroupManagerBase         → Group membership + path sync
  └── SharedFOV          → Merged field of view
        └── GroupColliderMovement  → Center of mass + distance-based collider
              └── GroupManager     → Entry point
```

<!-- TODO: images/group-inspector.png — GroupManager Inspector showing group members list -->

---

## How Groups Affect the Pipeline

<!-- TODO: images/group-pipeline-diagram.png — Diagram showing how GroupContext flows into each layer -->

| Layer | Group Behavior |
|-------|---------------|
| **L1-2** | Uses shared FOV when group collider active |
| **L3** | Uses group-level position/direction/speed for prediction |
| **L4** | Computes group force (cohesion + repulsion + alignment) |
| **L5** | Speed adjustment based on distance to center of mass |

---

## Group Force (L4)

<!-- TODO: images/group-forces-diagram.png — Diagram showing cohesion (pull), repulsion (push), alignment (arrows) -->

| Sub-Force | Weight | Behavior |
|-----------|--------|----------|
| **Cohesion** | 2.0 | Pull toward center of mass (threshold: `memberCount * 0.3 + 0.05`) |
| **Repulsion** | 1.5 | Push apart (distance < `2 * agentRadius + 0.05`) |
| **Alignment** | 1.5 | Align direction with group average |

---

## Group Speed (L5)

| Condition | Speed |
|-----------|-------|
| Beyond threshold from center | Speed up (toward) or slow down (away) |
| Within threshold | Group average speed |
| Initial | Average of members - `0.1 * memberCount` |

---

## Group Collider

<!-- TODO: images/group-collider.png — Scene view showing group collider around compact group -->

- Position: center of mass of all members
- **Enabled** when `maxDistance <= memberCount / 2` (compact group)
- **Disabled** when group is spread out
- When active: group acts as single entity for perception

---

## Path Synchronization

When one member reaches a node → `GroupManager.NotifyNextNode()` → updates target for all members.

---

## Configuration

Groups are defined in **AgentsList** ScriptableObject:

<!-- TODO: images/agent-list-groups.png — AgentsList Inspector showing Group section with groupName, count, agents -->

| Field | |
|-------|-|
| `groupName` | Unique name (not "Individual") |
| `count` | Number of groups to spawn |
| `agents` | 2-3 member prefabs |
| `speedRange` | Min/max speed per agent |

---

Next: [Animation and Gaze](Animation-and-Gaze.md) | [Customization](Customization.md)
