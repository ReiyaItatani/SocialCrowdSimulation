# Group System

Groups allow agents to walk together with shared perception, coordinated speed, and formation forces.

## Inheritance Chain

```
GroupManagerBase
  └── SharedFOV
        └── GroupColliderMovement
              └── GroupManager
```

Each class in the chain adds a specific capability:

### GroupManagerBase
**File**: `Runtime/Core/Group/GroupManagerBase.cs`

Base class that manages group membership:
- `groupMembers` — list of agent GameObjects assigned in the Inspector
- `agentsInCategory` — resolved list of `ParameterManager` GameObjects
- `GetGroupAgents()` — returns the resolved agent list
- `NotifyNextNode(node, sender)` — when one member reaches a path node, synchronizes the target node for all other members via `AgentPathManager.SetTargetNode()`

### SharedFOV
**File**: `Runtime/Core/Group/SharedFOV.cs`

Merges the field of view of all group members:
- Collects `CollisionAvoidanceController` from each member
- Union of all members' FOV agents → shared perception
- Removes agents that are in the same group (don't need to avoid each other through FOV)
- The shared FOV list is used by L1-2 when the group collider is active

### GroupColliderMovement
**File**: `Runtime/Core/Group/GroupColliderMovement.cs`

Manages the group's physical representation:
- **Center of Mass**: Updates the group collider's position to the average position of all members
- **Distance Checker**: Enables/disables the group collider based on member proximity:
  - If `maxDistance <= memberCount / 2` → collider enabled (group is compact)
  - Otherwise → collider disabled (group is spread out)
- When the collider is active, the group acts as a single entity for perception purposes

### GroupManager
**File**: `Runtime/Core/Group/GroupManager.cs`

Entry point attached to group GameObjects. Simple initialization:
```
Init() → base.Init() + InitColliderMovement() + InitSharedFOV()
CoUpdate() → GroupColliderMovementCoUpdate() + SharedFOVCoUpdate()
```

## How Groups Affect the Pipeline

### GroupContext Struct
`AgentPipelineCoordinator` pre-resolves group data into a `GroupContext` struct each frame:

```csharp
GroupContext {
    IsInGroup: true
    IsGroupColliderActive: true/false
    GroupName: "GroupA"
    Members: [GroupMember, GroupMember, ...]
    GroupFrame: AgentFrame (from GroupParameterManager)
    CenterOfMass: Vector3
}
```

### Per-Layer Effects

| Layer | Group Behavior |
|-------|---------------|
| **L1-2** | When group collider active: uses shared FOV for `PotentialAvoidanceTarget` detection |
| **L3** | When group collider active: uses group-level direction/position/speed for prediction |
| **L4** | Computes group force (cohesion + repulsion + alignment). Uses group frame for anticipated collision when collider active |
| **L5** | Group speed adjustment: members speed up/slow down based on distance to center of mass |

### Group Force Sub-Forces (L4)

| Sub-Force | Weight | Behavior |
|-----------|--------|----------|
| **Cohesion** | 2.0 | Pulls agent toward group center of mass when beyond threshold (`memberCount * 0.3 + 0.05`) |
| **Repulsion** | 1.5 | Pushes agents apart when closer than `2 * agentRadius + 0.05` |
| **Alignment** | 1.5 | Aligns agent direction with the average direction of group members |

### Group Speed (L5)
- **Initialization**: Computes average speed of all members, adjusted by `-0.1 * totalMemberCount`
- **Beyond threshold**: Speeds up (moving toward center) or slows down (moving away)
- **Within threshold**: Uses group average speed

### Path Synchronization
When one group member reaches a path node (`AgentPathManager.Update()`), it calls `GroupManager.NotifyNextNode()` which updates the target node for all other members. This keeps the group navigating together.

## Configuration

Groups are defined in the **AgentList** ScriptableObject:
- Each group entry has a unique name and 2-3 member prefabs
- Do not use "Individual" as a group name
- `maxSpeed` and `minSpeed` apply per-agent within the group

---

Next: [Animation and Gaze](Animation-and-Gaze.md) | [Customization](Customization.md)
