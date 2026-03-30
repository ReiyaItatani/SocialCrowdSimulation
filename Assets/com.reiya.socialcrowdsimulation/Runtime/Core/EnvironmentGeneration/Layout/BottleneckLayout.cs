using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates a bottleneck: two wide areas connected by a narrow passage.
    /// Layout along Z-axis: wide1 -> narrow -> wide2.
    /// Centered on X-axis at x=0.
    /// </summary>
    public class BottleneckLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float ww = config.wideWidth;
            float wd = config.wideDepth;
            float nw = config.narrowWidth;
            float nd = config.narrowDepth;
            float h = config.wallHeight;
            float t = config.wallThickness;

            float halfWw = ww / 2f;
            float halfNw = nw / 2f;

            // z layout: wide1 [0, wd], narrow [wd, wd+nd], wide2 [wd+nd, wd+nd+wd]
            float zNStart = wd;
            float z2Start = wd + nd;
            float z2End = wd + nd + wd;

            var root = new GameObject("Environment_Bottleneck");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            // Floors
            WallFactory.CreateFloor(
                new Vector3(0f, 0f, wd / 2f), ww, wd, root.transform, "Floor_Wide1");
            WallFactory.CreateFloor(
                new Vector3(0f, 0f, zNStart + nd / 2f), nw, nd, root.transform, "Floor_Narrow");
            WallFactory.CreateFloor(
                new Vector3(0f, 0f, z2Start + wd / 2f), ww, wd, root.transform, "Floor_Wide2");

            // Wide area 1 walls (span Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfWw - t / 2f, h / 2f, wd / 2f),
                t, h, wd, Vector3.forward,
                root.transform, "Wall_Wide1_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfWw + t / 2f, h / 2f, wd / 2f),
                t, h, wd, Vector3.forward,
                root.transform, "Wall_Wide1_Right"));

            // Wide area 1 end wall (span X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(0f, h / 2f, -t / 2f),
                t, h, ww + t * 2f, Vector3.right,
                root.transform, "Wall_Wide1_End"));

            // Narrowing walls (span Z)
            float sideWidth = (ww - nw) / 2f;
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfNw - sideWidth / 2f, h / 2f, zNStart + nd / 2f),
                sideWidth, h, nd, Vector3.forward,
                root.transform, "Wall_Narrow_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfNw + sideWidth / 2f, h / 2f, zNStart + nd / 2f),
                sideWidth, h, nd, Vector3.forward,
                root.transform, "Wall_Narrow_Right"));

            // Wide area 2 walls (span Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(-halfWw - t / 2f, h / 2f, z2Start + wd / 2f),
                t, h, wd, Vector3.forward,
                root.transform, "Wall_Wide2_Left"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(halfWw + t / 2f, h / 2f, z2Start + wd / 2f),
                t, h, wd, Vector3.forward,
                root.transform, "Wall_Wide2_Right"));

            // Wide area 2 end wall (span X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(0f, h / 2f, z2End + t / 2f),
                t, h, ww + t * 2f, Vector3.right,
                root.transform, "Wall_Wide2_End"));

            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("wide1", new Vector3(0f, 0f, wd / 2f), ww, wd),
                new WalkableRegion("narrow", new Vector3(0f, 0f, zNStart + nd / 2f), nw, nd),
                new WalkableRegion("wide2", new Vector3(0f, 0f, z2Start + wd / 2f), ww, wd)
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
