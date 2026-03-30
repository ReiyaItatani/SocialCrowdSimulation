using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates an L-shaped corridor: arm1 runs along Z, arm2 runs along X.
    /// They meet at the origin corner.
    /// </summary>
    public class LShapeLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float w1 = config.arm1Width;
            float d1 = config.arm1Depth;
            float w2 = config.arm2Width;
            float d2 = config.arm2Depth;
            float h = config.wallHeight;
            float t = config.wallThickness;

            var root = new GameObject("Environment_LShape");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            // Arm1: runs along Z from (0,0,0) to (w1, 0, d1)
            WallFactory.CreateFloor(
                new Vector3(w1 / 2f, 0f, d1 / 2f), w1, d1, root.transform, "Floor_Arm1");

            // Arm2: starts at x=w1, z=0 and extends to x=w1+d2, z=w2
            WallFactory.CreateFloor(
                new Vector3(w1 + d2 / 2f, 0f, w2 / 2f), d2, w2, root.transform, "Floor_Arm2");

            // Left wall of arm1 (full length, spans Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(-t / 2f, h / 2f, d1 / 2f),
                t, h, d1, Vector3.forward,
                root.transform, "Wall_Arm1_Left"));

            // Right wall of arm1 (from z=w2 to z=d1, above the junction, spans Z)
            float arm1RightLen = d1 - w2;
            if (arm1RightLen > 0)
            {
                walls.Add(WallFactory.CreateWall(
                    new Vector3(w1 + t / 2f, h / 2f, w2 + arm1RightLen / 2f),
                    t, h, arm1RightLen, Vector3.forward,
                    root.transform, "Wall_Arm1_Right"));
            }

            // End wall of arm1 (z=d1, spans X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(w1 / 2f, h / 2f, d1 + t / 2f),
                t, h, w1 + t * 2f, Vector3.right,
                root.transform, "Wall_Arm1_End"));

            // Bottom wall of arm2 (full length from x=0 to x=w1+d2, spans X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(w1 / 2f + d2 / 2f, h / 2f, -t / 2f),
                t, h, w1 + d2, Vector3.right,
                root.transform, "Wall_Arm2_Bottom"));

            // Top wall of arm2 (from x=w1 to x=w1+d2, spans X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(w1 + d2 / 2f, h / 2f, w2 + t / 2f),
                t, h, d2, Vector3.right,
                root.transform, "Wall_Arm2_Top"));

            // End wall of arm2 (x=w1+d2, spans Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(w1 + d2 + t / 2f, h / 2f, w2 / 2f),
                t, h, w2 + t * 2f, Vector3.forward,
                root.transform, "Wall_Arm2_End"));

            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("arm1", new Vector3(w1 / 2f, 0f, d1 / 2f), w1, d1),
                new WalkableRegion("arm2", new Vector3(w1 + d2 / 2f, 0f, w2 / 2f), d2, w2)
            };

            return new LayoutResult
            {
                rootGameObject = root,
                walkableRegions = regions,
                walls = walls,
                obstacles = obstacles
            };
        }
    }
}
