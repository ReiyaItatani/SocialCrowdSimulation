using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates a straight corridor with two side walls.
    /// Corridor runs along Z-axis: width = X, depth = Z.
    /// Origin at (0, 0, 0), corridor extends in +X and +Z.
    /// </summary>
    public class CorridorLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float w = config.width;
            float d = config.depth;
            float h = config.wallHeight;
            float t = config.wallThickness;

            var root = new GameObject("Environment_Corridor");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            // Floor
            WallFactory.CreateFloor(
                new Vector3(w / 2f, 0f, d / 2f), w, d, root.transform);

            // Left wall (X = 0 side): spans along Z
            walls.Add(WallFactory.CreateWall(
                new Vector3(-t / 2f, h / 2f, d / 2f),
                t, h, d, Vector3.forward,
                root.transform, "Wall_Left"));

            // Right wall (X = width side): spans along Z
            walls.Add(WallFactory.CreateWall(
                new Vector3(w + t / 2f, h / 2f, d / 2f),
                t, h, d, Vector3.forward,
                root.transform, "Wall_Right"));

            // End walls (optional, closed ends)
            if (!config.openEnds)
            {
                // Near end (Z = 0): spans along X
                walls.Add(WallFactory.CreateWall(
                    new Vector3(w / 2f, h / 2f, -t / 2f),
                    t, h, w + t * 2f, Vector3.right,
                    root.transform, "Wall_Near"));

                // Far end (Z = depth): spans along X
                walls.Add(WallFactory.CreateWall(
                    new Vector3(w / 2f, h / 2f, d + t / 2f),
                    t, h, w + t * 2f, Vector3.right,
                    root.transform, "Wall_Far"));
            }

            // Obstacles from config
            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("corridor", new Vector3(w / 2f, 0f, d / 2f), w, d)
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
