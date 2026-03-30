using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates a cross-intersection: 4 corridor arms meeting at a center square.
    /// Center is at origin. Arms extend in +Z (north), -Z (south), +X (east), -X (west).
    /// </summary>
    public class IntersectionLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float aw = config.armWidth;
            float ad = config.armDepth;
            float h = config.wallHeight;
            float t = config.wallThickness;
            float halfAw = aw / 2f;

            var root = new GameObject("Environment_Intersection");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            // Floor: covers center + all 4 arms
            float totalSize = aw + ad * 2f;
            WallFactory.CreateFloor(Vector3.zero, totalSize, totalSize, root.transform);

            // --- North arm (Z+): side walls span along Z ---
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfAw - t / 2f, h / 2f, halfAw + ad / 2f),
                t, h, ad, Vector3.forward,
                root.transform, "Wall_North_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfAw + t / 2f, h / 2f, halfAw + ad / 2f),
                t, h, ad, Vector3.forward,
                root.transform, "Wall_North_Right"));

            // --- South arm (Z-): side walls span along Z ---
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfAw - t / 2f, h / 2f, -halfAw - ad / 2f),
                t, h, ad, Vector3.forward,
                root.transform, "Wall_South_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfAw + t / 2f, h / 2f, -halfAw - ad / 2f),
                t, h, ad, Vector3.forward,
                root.transform, "Wall_South_Right"));

            // --- East arm (X+): side walls span along X ---
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfAw + ad / 2f, h / 2f, halfAw + t / 2f),
                t, h, ad, Vector3.right,
                root.transform, "Wall_East_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfAw + ad / 2f, h / 2f, -halfAw - t / 2f),
                t, h, ad, Vector3.right,
                root.transform, "Wall_East_Right"));

            // --- West arm (X-): side walls span along X ---
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfAw - ad / 2f, h / 2f, halfAw + t / 2f),
                t, h, ad, Vector3.right,
                root.transform, "Wall_West_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfAw - ad / 2f, h / 2f, -halfAw - t / 2f),
                t, h, ad, Vector3.right,
                root.transform, "Wall_West_Right"));

            // End walls for each arm: span along X or Z
            walls.Add(WallFactory.CreateWall(
                new Vector3(0f, h / 2f, halfAw + ad + t / 2f),
                t, h, aw + t * 2f, Vector3.right,
                root.transform, "Wall_North_End"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(0f, h / 2f, -halfAw - ad - t / 2f),
                t, h, aw + t * 2f, Vector3.right,
                root.transform, "Wall_South_End"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfAw + ad + t / 2f, h / 2f, 0f),
                t, h, aw + t * 2f, Vector3.forward,
                root.transform, "Wall_East_End"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfAw - ad - t / 2f, h / 2f, 0f),
                t, h, aw + t * 2f, Vector3.forward,
                root.transform, "Wall_West_End"));

            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("center", Vector3.zero, aw, aw),
                new WalkableRegion("arm_north", new Vector3(0f, 0f, halfAw + ad / 2f), aw, ad),
                new WalkableRegion("arm_south", new Vector3(0f, 0f, -halfAw - ad / 2f), aw, ad),
                new WalkableRegion("arm_east", new Vector3(halfAw + ad / 2f, 0f, 0f), ad, aw),
                new WalkableRegion("arm_west", new Vector3(-halfAw - ad / 2f, 0f, 0f), ad, aw)
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
