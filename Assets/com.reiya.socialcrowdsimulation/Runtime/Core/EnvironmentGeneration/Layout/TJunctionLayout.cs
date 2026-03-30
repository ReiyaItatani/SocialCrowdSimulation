using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates a T-junction: a main corridor along Z with a branch extending in +X from the center.
    /// Main corridor: (0,0,0) to (mainWidth, 0, mainDepth).
    /// Branch: extends from center of main corridor in +X direction.
    /// </summary>
    public class TJunctionLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float mw = config.mainWidth;
            float md = config.mainDepth;
            float bw = config.branchWidth;
            float bd = config.branchDepth;
            float h = config.wallHeight;
            float t = config.wallThickness;

            float branchZCenter = md / 2f;
            float halfBw = bw / 2f;

            var root = new GameObject("Environment_TJunction");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            // Main corridor floor
            WallFactory.CreateFloor(
                new Vector3(mw / 2f, 0f, md / 2f), mw, md, root.transform, "Floor_Main");

            // Branch floor
            WallFactory.CreateFloor(
                new Vector3(mw + bd / 2f, 0f, branchZCenter), bd, bw, root.transform, "Floor_Branch");

            // Left wall of main corridor (x=0, full length, spans Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(-t / 2f, h / 2f, md / 2f),
                t, h, md, Vector3.forward,
                root.transform, "Wall_Main_Left"));

            // Right wall of main corridor — split around branch opening
            float belowLen = branchZCenter - halfBw;
            if (belowLen > 0)
            {
                walls.Add(WallFactory.CreateWall(
                    new Vector3(mw + t / 2f, h / 2f, belowLen / 2f),
                    t, h, belowLen, Vector3.forward,
                    root.transform, "Wall_Main_Right_Below"));
            }

            float aboveLen = md - (branchZCenter + halfBw);
            if (aboveLen > 0)
            {
                walls.Add(WallFactory.CreateWall(
                    new Vector3(mw + t / 2f, h / 2f, branchZCenter + halfBw + aboveLen / 2f),
                    t, h, aboveLen, Vector3.forward,
                    root.transform, "Wall_Main_Right_Above"));
            }

            // End walls of main corridor (span X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(mw / 2f, h / 2f, -t / 2f),
                t, h, mw + t * 2f, Vector3.right,
                root.transform, "Wall_Main_Near"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(mw / 2f, h / 2f, md + t / 2f),
                t, h, mw + t * 2f, Vector3.right,
                root.transform, "Wall_Main_Far"));

            // Branch side walls (span X)
            walls.Add(WallFactory.CreateWall(
                new Vector3(mw + bd / 2f, h / 2f, branchZCenter + halfBw + t / 2f),
                t, h, bd, Vector3.right,
                root.transform, "Wall_Branch_Top"));
            walls.Add(WallFactory.CreateWall(
                new Vector3(mw + bd / 2f, h / 2f, branchZCenter - halfBw - t / 2f),
                t, h, bd, Vector3.right,
                root.transform, "Wall_Branch_Bottom"));

            // Branch end wall (span Z)
            walls.Add(WallFactory.CreateWall(
                new Vector3(mw + bd + t / 2f, h / 2f, branchZCenter),
                t, h, bw + t * 2f, Vector3.forward,
                root.transform, "Wall_Branch_End"));

            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("main", new Vector3(mw / 2f, 0f, md / 2f), mw, md),
                new WalkableRegion("branch", new Vector3(mw + bd / 2f, 0f, branchZCenter), bd, bw)
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
