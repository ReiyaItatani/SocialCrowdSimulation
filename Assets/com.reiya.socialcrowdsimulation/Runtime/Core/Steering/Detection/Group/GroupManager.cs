using System.Collections;
using System.Collections.Generic;
using CollisionAvoidance;
using UnityEngine;

public class GroupManager : GroupColliderMovement
{
    protected override void Init()
    {
        base.Init();
        InitColliderMovementCoUpdate();
        InitSharedFOV();
    }
    protected override void CoUpdate()
    {
        base.CoUpdate();
        GroupColliderMovementCoUpdate();
        SharedFOVCoUpdate();
    }
}
