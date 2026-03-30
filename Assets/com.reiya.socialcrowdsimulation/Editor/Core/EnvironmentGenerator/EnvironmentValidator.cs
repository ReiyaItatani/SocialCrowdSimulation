using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Post-generation validation: checks tags, colliders, NormalVector, graph connectivity.
    /// </summary>
    public static class EnvironmentValidator
    {
        public struct ValidationReport
        {
            public int errors;
            public int warnings;
            public List<string> messages;

            public bool IsValid => errors == 0;
        }

        public static ValidationReport Validate(GameObject environmentRoot)
        {
            var report = new ValidationReport
            {
                messages = new List<string>()
            };

            if (environmentRoot == null)
            {
                AddError(ref report, "Environment root is null.");
                return report;
            }

            ValidateWalls(environmentRoot, ref report);
            ValidateObstacles(environmentRoot, ref report);
            ValidateGraph(environmentRoot, ref report);
            ValidateAvatarCreator(environmentRoot, ref report);

            if (report.IsValid)
            {
                report.messages.Add("[OK] All validation checks passed.");
            }

            return report;
        }

        private static void ValidateWalls(GameObject root, ref ValidationReport report)
        {
            var walls = FindChildrenWithTag(root, "Wall");
            if (walls.Count == 0)
            {
                AddWarning(ref report, "No objects with 'Wall' tag found.");
                return;
            }

            foreach (var wall in walls)
            {
                if (wall.GetComponent<BoxCollider>() == null)
                    AddError(ref report, $"Wall '{wall.name}' missing BoxCollider.");

                if (wall.GetComponent<NormalVector>() == null)
                    AddError(ref report, $"Wall '{wall.name}' missing NormalVector component.");
            }

            report.messages.Add($"[OK] {walls.Count} walls validated.");
        }

        private static void ValidateObstacles(GameObject root, ref ValidationReport report)
        {
            var obstacles = FindChildrenWithTag(root, "Obstacle");

            foreach (var obs in obstacles)
            {
                if (obs.GetComponent<BoxCollider>() == null)
                    AddError(ref report, $"Obstacle '{obs.name}' missing BoxCollider.");

                if (obs.GetComponent<NormalVector>() == null)
                    AddError(ref report, $"Obstacle '{obs.name}' missing NormalVector component.");
            }

            if (obstacles.Count > 0)
                report.messages.Add($"[OK] {obstacles.Count} obstacles validated.");
        }

        private static void ValidateGraph(GameObject root, ref ValidationReport report)
        {
            var graph = root.GetComponentInChildren<QuickGraph>();
            if (graph == null)
            {
                AddError(ref report, "No QuickGraph found in environment.");
                return;
            }

            var nodes = graph.GetComponentsInChildren<QuickGraphNode>();
            if (nodes.Length < 2)
            {
                AddError(ref report, $"QuickGraph has only {nodes.Length} node(s). Need at least 2.");
                return;
            }

            // Check each node has at least 1 neighbor
            int isolated = 0;
            foreach (var node in nodes)
            {
                if (node._neighbours == null || node._neighbours.Count == 0)
                    isolated++;
            }

            if (isolated > 0)
                AddWarning(ref report, $"{isolated} node(s) have no neighbors.");

            report.messages.Add($"[OK] QuickGraph: {nodes.Length} nodes.");
        }

        private static void ValidateAvatarCreator(GameObject root, ref ValidationReport report)
        {
            var creator = root.GetComponentInChildren<AvatarCreatorQuickGraph>();
            if (creator == null)
            {
                AddWarning(ref report, "No AvatarCreatorQuickGraph found. Agents cannot spawn.");
                return;
            }

            if (creator.agentsList == null)
            {
                AddError(ref report, "AvatarCreatorQuickGraph has no AgentsList assigned.");
                return;
            }

            if (creator.quickGraph == null)
            {
                AddError(ref report, "AvatarCreatorQuickGraph has no QuickGraph assigned.");
                return;
            }

            int total = 0;
            if (creator.agentsList.individuals?.agents != null)
                total += creator.agentsList.individuals.agents.Count;
            if (creator.agentsList.groups != null)
            {
                foreach (var group in creator.agentsList.groups)
                    total += group.agents.Count;
            }

            report.messages.Add($"[OK] AvatarCreator configured: {total} agents.");
        }

        private static List<GameObject> FindChildrenWithTag(GameObject root, string tag)
        {
            var results = new List<GameObject>();
            FindChildrenWithTagRecursive(root.transform, tag, results);
            return results;
        }

        private static void FindChildrenWithTagRecursive(Transform parent, string tag, List<GameObject> results)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.CompareTag(tag))
                    results.Add(child.gameObject);
                FindChildrenWithTagRecursive(child, tag, results);
            }
        }

        private static void AddError(ref ValidationReport report, string message)
        {
            report.errors++;
            report.messages.Add($"[ERROR] {message}");
            Debug.LogError($"[EnvironmentValidator] {message}");
        }

        private static void AddWarning(ref ValidationReport report, string message)
        {
            report.warnings++;
            report.messages.Add($"[WARN] {message}");
            Debug.LogWarning($"[EnvironmentValidator] {message}");
        }
    }
}
