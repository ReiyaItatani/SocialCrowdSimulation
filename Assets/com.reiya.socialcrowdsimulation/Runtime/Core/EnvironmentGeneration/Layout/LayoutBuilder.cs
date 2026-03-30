using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Orchestrates layout generation by dispatching to the correct ILayoutStrategy.
    /// </summary>
    public static class LayoutBuilder
    {
        private static readonly Dictionary<LayoutType, ILayoutStrategy> Strategies =
            new Dictionary<LayoutType, ILayoutStrategy>
            {
                { LayoutType.Corridor, new CorridorLayout() },
                { LayoutType.Plaza, new PlazaLayout() },
                { LayoutType.Intersection, new IntersectionLayout() },
                { LayoutType.LShape, new LShapeLayout() },
                { LayoutType.TJunction, new TJunctionLayout() },
                { LayoutType.Bottleneck, new BottleneckLayout() }
            };

        public static LayoutResult Build(LayoutConfig config, int seed, Transform parent = null)
        {
            Random.InitState(seed);

            if (!Strategies.TryGetValue(config.type, out var strategy))
            {
                Debug.LogError($"[LayoutBuilder] Unknown layout type: {config.type}");
                return default;
            }

            var result = strategy.Build(config, seed, parent);
            Debug.Log($"[LayoutBuilder] Generated {config.type}: " +
                      $"{result.walls.Count} walls, {result.obstacles.Count} obstacles, " +
                      $"{result.walkableRegions.Count} walkable regions");
            return result;
        }
    }
}
