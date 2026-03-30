using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Stores a node position and the region it belongs to.
    /// </summary>
    public struct NodePlacement
    {
        public Vector3 position;
        public int regionIndex;

        public NodePlacement(Vector3 position, int regionIndex)
        {
            this.position = position;
            this.regionIndex = regionIndex;
        }
    }

    /// <summary>
    /// Places QuickGraphNode positions within walkable regions on a grid,
    /// respecting wall margins. Tracks which region each node belongs to.
    /// </summary>
    public static class GraphPlacementStrategy
    {
        /// <summary>
        /// Generates node placements (position + region index) across all walkable regions.
        /// </summary>
        public static List<NodePlacement> GenerateNodePlacements(
            List<WalkableRegion> regions,
            GraphConfig config)
        {
            var placements = new List<NodePlacement>();
            float spacing = config.nodeSpacing;
            float margin = config.wallMargin;

            for (int r = 0; r < regions.Count; r++)
            {
                var region = regions[r];
                float usableWidth = region.width - margin * 2f;
                float usableDepth = region.depth - margin * 2f;

                if (usableWidth <= 0f || usableDepth <= 0f)
                {
                    placements.Add(new NodePlacement(region.center, r));
                    continue;
                }

                int countX = Mathf.Max(1, Mathf.FloorToInt(usableWidth / spacing) + 1);
                int countZ = Mathf.Max(1, Mathf.FloorToInt(usableDepth / spacing) + 1);

                float actualSpacingX = countX > 1 ? usableWidth / (countX - 1) : 0f;
                float actualSpacingZ = countZ > 1 ? usableDepth / (countZ - 1) : 0f;

                float startX = countX > 1 ? region.center.x - usableWidth / 2f : region.center.x;
                float startZ = countZ > 1 ? region.center.z - usableDepth / 2f : region.center.z;

                for (int ix = 0; ix < countX; ix++)
                {
                    for (int iz = 0; iz < countZ; iz++)
                    {
                        float x = startX + ix * actualSpacingX;
                        float z = startZ + iz * actualSpacingZ;
                        var pos = new Vector3(x, 0f, z);

                        if (!HasNearbyPlacement(placements, pos, spacing * 0.5f))
                        {
                            placements.Add(new NodePlacement(pos, r));
                        }
                    }
                }
            }

            // Ensure at least one node per region center
            for (int r = 0; r < regions.Count; r++)
            {
                if (!HasNearbyPlacement(placements, regions[r].center, spacing * 0.5f))
                {
                    placements.Add(new NodePlacement(regions[r].center, r));
                }
            }

            return placements;
        }

        /// <summary>
        /// Builds adjacency between regions. Two regions are adjacent if they
        /// share an edge (overlap along one axis with nonzero length).
        /// Point-only touching (corners) does NOT count as adjacent.
        /// </summary>
        public static HashSet<long> BuildRegionAdjacency(List<WalkableRegion> regions)
        {
            var adjacent = new HashSet<long>();

            for (int i = 0; i < regions.Count; i++)
            {
                adjacent.Add(RegionPairKey(i, i));

                for (int j = i + 1; j < regions.Count; j++)
                {
                    if (RegionsShareEdge(regions[i], regions[j]))
                    {
                        adjacent.Add(RegionPairKey(i, j));
                        adjacent.Add(RegionPairKey(j, i));
                    }
                }
            }

            return adjacent;
        }

        public static long RegionPairKey(int a, int b)
        {
            return (long)a * 10000 + b;
        }

        /// <summary>
        /// Two regions share an edge if their bounding rects overlap with nonzero
        /// length along at least one axis. This excludes corner-only touching.
        /// </summary>
        private static bool RegionsShareEdge(WalkableRegion a, WalkableRegion b)
        {
            float tolerance = 0.5f;

            float aMinX = a.center.x - a.width / 2f;
            float aMaxX = a.center.x + a.width / 2f;
            float aMinZ = a.center.z - a.depth / 2f;
            float aMaxZ = a.center.z + a.depth / 2f;

            float bMinX = b.center.x - b.width / 2f;
            float bMaxX = b.center.x + b.width / 2f;
            float bMinZ = b.center.z - b.depth / 2f;
            float bMaxZ = b.center.z + b.depth / 2f;

            // Overlap ranges
            float overlapX = Mathf.Min(aMaxX, bMaxX) - Mathf.Max(aMinX, bMinX);
            float overlapZ = Mathf.Min(aMaxZ, bMaxZ) - Mathf.Max(aMinZ, bMinZ);

            // Regions must overlap in both axes, AND have significant overlap
            // in at least one axis (not just a corner point)
            float minEdgeLength = 1f;

            // Case 1: overlap in both axes (actual area overlap)
            if (overlapX > 0f && overlapZ > 0f)
                return true;

            // Case 2: touching along X edge (overlapX > minEdgeLength, Z edges touch within tolerance)
            if (overlapX >= minEdgeLength && overlapZ >= -tolerance)
                return true;

            // Case 3: touching along Z edge (overlapZ > minEdgeLength, X edges touch within tolerance)
            if (overlapZ >= minEdgeLength && overlapX >= -tolerance)
                return true;

            return false;
        }

        private static bool HasNearbyPlacement(List<NodePlacement> placements, Vector3 target, float threshold)
        {
            float sqrThreshold = threshold * threshold;
            foreach (var p in placements)
            {
                if ((p.position - target).sqrMagnitude < sqrThreshold)
                    return true;
            }
            return false;
        }
    }
}
