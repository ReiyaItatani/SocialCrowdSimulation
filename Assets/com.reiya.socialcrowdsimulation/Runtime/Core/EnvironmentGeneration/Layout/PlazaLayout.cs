using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Generates an open rectangular plaza with walls on all 4 sides.
    /// </summary>
    public class PlazaLayout : ILayoutStrategy
    {
        public LayoutResult Build(LayoutConfig config, int seed, Transform parent)
        {
            float w = config.width;
            float d = config.depth;
            float h = config.wallHeight;
            float t = config.wallThickness;

            var root = new GameObject("Environment_Plaza");
            root.transform.parent = parent;

            var walls = new List<GameObject>();
            var obstacles = new List<GameObject>();

            WallFactory.CreateFloor(
                new Vector3(w / 2f, 0f, d / 2f), w, d, root.transform);

            // West wall (X=0): spans along Z
            walls.Add(WallFactory.CreateWall(
                new Vector3(-t / 2f, h / 2f, d / 2f),
                t, h, d, Vector3.forward,
                root.transform, "Wall_West"));

            // East wall (X=w): spans along Z
            walls.Add(WallFactory.CreateWall(
                new Vector3(w + t / 2f, h / 2f, d / 2f),
                t, h, d, Vector3.forward,
                root.transform, "Wall_East"));

            // South wall (Z=0): spans along X
            walls.Add(WallFactory.CreateWall(
                new Vector3(w / 2f, h / 2f, -t / 2f),
                t, h, w + t * 2f, Vector3.right,
                root.transform, "Wall_South"));

            // North wall (Z=d): spans along X
            walls.Add(WallFactory.CreateWall(
                new Vector3(w / 2f, h / 2f, d + t / 2f),
                t, h, w + t * 2f, Vector3.right,
                root.transform, "Wall_North"));

            foreach (var obs in config.obstacles)
            {
                obstacles.Add(WallFactory.CreateObstacle(
                    obs.GetPosition(), obs.GetSize(), root.transform));
            }

            var regions = new List<WalkableRegion>
            {
                new WalkableRegion("plaza", new Vector3(w / 2f, 0f, d / 2f), w, d)
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
