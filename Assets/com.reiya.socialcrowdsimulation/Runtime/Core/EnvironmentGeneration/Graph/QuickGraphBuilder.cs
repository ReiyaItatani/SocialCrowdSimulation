using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Orchestrates QuickGraph generation: placement → region adjacency → connection → validation.
    /// </summary>
    public static class QuickGraphBuilder
    {
        public static QuickGraph Build(LayoutResult layout, GraphConfig config, int seed)
        {
            Random.InitState(seed);

            // Step 1: Generate node placements (position + region index)
            var placements = GraphPlacementStrategy.GenerateNodePlacements(
                layout.walkableRegions, config);

            if (placements.Count == 0)
            {
                Debug.LogError("[QuickGraphBuilder] No node positions generated.");
                return null;
            }

            // Step 2: Build region adjacency map
            var regionAdjacency = GraphPlacementStrategy.BuildRegionAdjacency(
                layout.walkableRegions);

            Debug.Log($"[QuickGraphBuilder] Placed {placements.Count} nodes across {layout.walkableRegions.Count} regions.");

            // Step 3: Create QuickGraph with region-aware connections
            var graph = GraphConnector.BuildGraph(
                placements, config, regionAdjacency, layout.rootGameObject.transform);

            // Step 4: Validate connectivity and repair if needed
            var validation = GraphValidator.ValidateAndRepair(graph);

            if (validation.forceConnections > 0)
            {
                Debug.LogWarning($"[QuickGraphBuilder] {validation.forceConnections} force-connections made. " +
                                 "Consider adjusting nodeSpacing or maxConnectionDistance.");
            }

            Debug.Log($"[QuickGraphBuilder] Graph complete: {placements.Count} nodes, " +
                      $"{(validation.isFullyConnected ? "fully connected" : "repaired")}.");

            return graph;
        }
    }
}
