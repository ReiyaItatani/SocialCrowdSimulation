using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Validates QuickGraph connectivity via BFS.
    /// Force-connects disconnected components if needed.
    /// </summary>
    public static class GraphValidator
    {
        public struct ValidationResult
        {
            public bool isFullyConnected;
            public int componentCount;
            public int forceConnections;
        }

        /// <summary>
        /// Validates and repairs graph connectivity.
        /// </summary>
        public static ValidationResult ValidateAndRepair(QuickGraph graph)
        {
            var nodes = new List<QuickGraphNode>(graph.GetComponentsInChildren<QuickGraphNode>());
            if (nodes.Count == 0)
            {
                Debug.LogWarning("[GraphValidator] No nodes in graph.");
                return new ValidationResult { isFullyConnected = true, componentCount = 0 };
            }

            var components = FindComponents(nodes);
            var result = new ValidationResult
            {
                componentCount = components.Count,
                isFullyConnected = components.Count <= 1
            };

            if (components.Count <= 1)
            {
                Debug.Log($"[GraphValidator] Graph is fully connected ({nodes.Count} nodes).");
                return result;
            }

            Debug.LogWarning($"[GraphValidator] Found {components.Count} disconnected components. Repairing...");

            // Connect each component to the main (largest) component
            var mainComponent = components[0];
            for (int i = 1; i < components.Count; i++)
            {
                mainComponent = mainComponent.Count >= components[i].Count ? mainComponent : components[i];
            }

            int forced = 0;
            foreach (var component in components)
            {
                if (component == mainComponent) continue;

                // Find closest pair between this component and main
                float bestDist = float.MaxValue;
                QuickGraphNode bestA = null;
                QuickGraphNode bestB = null;

                foreach (var nodeA in component)
                {
                    foreach (var nodeB in mainComponent)
                    {
                        float dist = (nodeA.transform.position - nodeB.transform.position).sqrMagnitude;
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestA = nodeA;
                            bestB = nodeB;
                        }
                    }
                }

                if (bestA != null && bestB != null)
                {
                    bestA.AddNeighbour(bestB);
                    bestB.AddNeighbour(bestA);
                    forced++;
                    Debug.LogWarning($"[GraphValidator] Force-connected {bestA.name} <-> {bestB.name} " +
                                    $"(distance: {Mathf.Sqrt(bestDist):F1}m)");
                }

                // Merge into main for subsequent components
                mainComponent.AddRange(component);
            }

            result.forceConnections = forced;
            return result;
        }

        private static List<List<QuickGraphNode>> FindComponents(List<QuickGraphNode> nodes)
        {
            var visited = new HashSet<QuickGraphNode>();
            var components = new List<List<QuickGraphNode>>();

            foreach (var node in nodes)
            {
                if (visited.Contains(node)) continue;

                var component = new List<QuickGraphNode>();
                var queue = new Queue<QuickGraphNode>();
                queue.Enqueue(node);
                visited.Add(node);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);

                    foreach (var neighbour in current._neighbours)
                    {
                        if (neighbour != null && !visited.Contains(neighbour))
                        {
                            visited.Add(neighbour);
                            queue.Enqueue(neighbour);
                        }
                    }
                }

                components.Add(component);
            }

            return components;
        }
    }
}
