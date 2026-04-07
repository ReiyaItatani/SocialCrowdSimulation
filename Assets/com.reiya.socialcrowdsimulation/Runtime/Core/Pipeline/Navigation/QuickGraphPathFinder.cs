using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Static utility for computing patrol routes on a QuickGraph.
    /// Finds the two most distant nodes by Euclidean distance,
    /// then builds a path between them that follows the graph's main axis.
    /// </summary>
    public static class QuickGraphPathFinder
    {
        /// <summary>
        /// Compute a patrol route: find the two Euclidean-farthest nodes,
        /// then connect them via the graph preferring main-axis movement.
        /// Returns an ordered list of nodes forming the route.
        /// </summary>
        public static List<QuickGraphNode> ComputePatrolRoute(List<QuickGraphNode> allNodes)
        {
            if (allNodes == null || allNodes.Count == 0)
                return new List<QuickGraphNode>();

            if (allNodes.Count == 1)
                return new List<QuickGraphNode> { allNodes[0] };

            var (endA, endB) = FindFarthestPairEuclidean(allNodes);
            return FindMainAxisPath(endA, endB);
        }

        /// <summary>
        /// Brute-force find the two nodes with the greatest Euclidean distance.
        /// </summary>
        private static (QuickGraphNode, QuickGraphNode) FindFarthestPairEuclidean(List<QuickGraphNode> nodes)
        {
            QuickGraphNode bestA = nodes[0];
            QuickGraphNode bestB = nodes[1];
            float maxDistSq = 0f;

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    float distSq = (nodes[i].transform.position - nodes[j].transform.position).sqrMagnitude;
                    if (distSq > maxDistSq)
                    {
                        maxDistSq = distSq;
                        bestA = nodes[i];
                        bestB = nodes[j];
                    }
                }
            }

            return (bestA, bestB);
        }

        /// <summary>
        /// A*-style path from 'from' to 'to', scoring by remaining distance
        /// with a penalty for deviation from the main axis (the line between endpoints).
        /// This avoids diagonal zigzag in grid-like graphs.
        /// </summary>
        private static List<QuickGraphNode> FindMainAxisPath(QuickGraphNode from, QuickGraphNode to)
        {
            if (from == to)
                return new List<QuickGraphNode> { from };

            Vector3 mainAxis = (to.transform.position - from.transform.position).normalized;

            var visited = new HashSet<QuickGraphNode> { from };
            var parent = new Dictionary<QuickGraphNode, QuickGraphNode>();
            var open = new List<(QuickGraphNode node, float score)> { (from, 0f) };

            while (open.Count > 0)
            {
                int bestIdx = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (open[i].score < open[bestIdx].score)
                        bestIdx = i;
                }

                var current = open[bestIdx].node;
                open.RemoveAt(bestIdx);

                if (current == to)
                    return ReconstructPath(parent, from, to);

                foreach (var neighbour in current._neighbours)
                {
                    if (visited.Contains(neighbour))
                        continue;

                    visited.Add(neighbour);
                    parent[neighbour] = current;

                    Vector3 offset = neighbour.transform.position - from.transform.position;
                    float progress = Vector3.Dot(offset, mainAxis);
                    Vector3 projection = from.transform.position + mainAxis * progress;
                    float deviation = (neighbour.transform.position - projection).magnitude;
                    float remainingDist = (to.transform.position - neighbour.transform.position).magnitude;

                    open.Add((neighbour, remainingDist + deviation * 2f));
                }
            }

            return new List<QuickGraphNode>();
        }

        private static List<QuickGraphNode> ReconstructPath(
            Dictionary<QuickGraphNode, QuickGraphNode> parent,
            QuickGraphNode from,
            QuickGraphNode to)
        {
            var path = new List<QuickGraphNode>();
            var current = to;
            while (current != from)
            {
                path.Add(current);
                current = parent[current];
            }
            path.Add(from);
            path.Reverse();
            return path;
        }
    }
}
