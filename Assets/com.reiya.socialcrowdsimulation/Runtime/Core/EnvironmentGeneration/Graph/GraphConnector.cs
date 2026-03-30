using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Creates a QuickGraph with QuickGraphNode children and connects them
    /// based on proximity, region adjacency, and line-of-sight.
    ///
    /// Nodes are only connected if they belong to the same region or
    /// adjacent regions, preventing diagonal shortcuts through open areas.
    /// </summary>
    public static class GraphConnector
    {
        /// <summary>
        /// Builds a QuickGraph from node placements with region-aware connections.
        /// </summary>
        public static QuickGraph BuildGraph(
            List<NodePlacement> placements,
            GraphConfig config,
            HashSet<long> regionAdjacency,
            Transform parent)
        {
            var graphGO = new GameObject("QuickGraph");
            graphGO.transform.parent = parent;

            var nodes = new List<QuickGraphNode>();

            // Create node GameObjects
            for (int i = 0; i < placements.Count; i++)
            {
                var nodeGO = new GameObject($"Node_{i}");
                nodeGO.transform.parent = graphGO.transform;
                nodeGO.transform.position = placements[i].position;
                var node = nodeGO.AddComponent<QuickGraphNode>();
                nodes.Add(node);
            }

            // Connect nodes: same/adjacent region + distance + LOS
            float maxDist = config.maxConnectionDistance;
            float maxDistSqr = maxDist * maxDist;
            float rayHeight = 0.5f;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    // Region adjacency check
                    long key = GraphPlacementStrategy.RegionPairKey(
                        placements[i].regionIndex, placements[j].regionIndex);
                    if (!regionAdjacency.Contains(key))
                        continue;

                    var posA = nodes[i].transform.position;
                    var posB = nodes[j].transform.position;

                    if ((posA - posB).sqrMagnitude > maxDistSqr)
                        continue;

                    if (config.requireLineOfSight)
                    {
                        var from = new Vector3(posA.x, rayHeight, posA.z);
                        var to = new Vector3(posB.x, rayHeight, posB.z);
                        var direction = to - from;

                        if (Physics.Raycast(from, direction.normalized, direction.magnitude, Physics.DefaultRaycastLayers))
                            continue;
                    }

                    nodes[i].AddNeighbour(nodes[j]);
                    nodes[j].AddNeighbour(nodes[i]);
                }
            }

            var quickGraph = graphGO.AddComponent<QuickGraph>();

            // In Editor, Awake() doesn't run, so populate _nodes manually.
            quickGraph.CheckNeighbourhood();

            return quickGraph;
        }
    }
}
