using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    [Serializable]
    public class EnvironmentConfig
    {
        public string version = "1.0";
        public int seed = 42;
        public LayoutConfig layout = new LayoutConfig();
        public GraphConfig graph = new GraphConfig();
    }

    [Serializable]
    public class LayoutConfig
    {
        public LayoutType type = LayoutType.Corridor;

        // Common
        public float width = 4f;
        public float depth = 20f;
        public float wallHeight = 3f;
        public float wallThickness = 0.2f;
        public bool openEnds;

        // Intersection
        public float armWidth = 4f;
        public float armDepth = 12f;

        // LShape
        public float arm1Width = 4f;
        public float arm1Depth = 12f;
        public float arm2Width = 4f;
        public float arm2Depth = 12f;

        // TJunction
        public float mainWidth = 4f;
        public float mainDepth = 20f;
        public float branchWidth = 4f;
        public float branchDepth = 10f;

        // Bottleneck
        public float wideWidth = 10f;
        public float wideDepth = 8f;
        public float narrowWidth = 2f;
        public float narrowDepth = 4f;

        public List<ObstacleEntry> obstacles = new List<ObstacleEntry>();
    }

    [Serializable]
    public class ObstacleEntry
    {
        public string type = "box";
        public float[] position = { 0f, 0f, 0f };
        public float[] size = { 0.5f, 1f, 0.5f };

        public Vector3 GetPosition() => new Vector3(position[0], position[1], position[2]);
        public Vector3 GetSize() => new Vector3(size[0], size[1], size[2]);
    }

    [Serializable]
    public class GraphConfig
    {
        public float nodeSpacing = 3f;
        public float wallMargin = 0.8f;
        public float maxConnectionDistance = 8f;
        public bool requireLineOfSight = true;
    }

    public struct WalkableRegion
    {
        public string regionId;
        public Vector3 center;
        public float width;
        public float depth;

        public float Area => width * depth;

        public WalkableRegion(string id, Vector3 center, float width, float depth)
        {
            regionId = id;
            this.center = center;
            this.width = width;
            this.depth = depth;
        }
    }

    public struct LayoutResult
    {
        public GameObject rootGameObject;
        public List<WalkableRegion> walkableRegions;
        public List<GameObject> walls;
        public List<GameObject> obstacles;

        public float TotalWalkableArea
        {
            get
            {
                float total = 0f;
                foreach (var region in walkableRegions)
                    total += region.Area;
                return total;
            }
        }
    }
}
