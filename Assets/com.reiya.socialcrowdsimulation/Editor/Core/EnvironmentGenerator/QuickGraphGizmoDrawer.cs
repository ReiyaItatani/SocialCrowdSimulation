#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Draws QuickGraph nodes and connections with high-visibility gizmos.
    /// Yellow lines (thick) + orange node spheres (larger).
    /// </summary>
    [InitializeOnLoad]
    public static class QuickGraphGizmoDrawer
    {
        private static readonly Color NodeColor = new Color(1f, 0.5f, 0f);       // Orange
        private static readonly Color LineColor = new Color(1f, 0.9f, 0f);       // Yellow
        private static readonly Color SelectedNodeColor = Color.green;
        private const float NodeRadius = 0.25f;
        private const float LineWidth = 3f;

        static QuickGraphGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var graphs = Object.FindObjectsByType<QuickGraph>(FindObjectsSortMode.None);

            foreach (var graph in graphs)
            {
                var nodes = graph.GetComponentsInChildren<QuickGraphNode>();
                if (nodes.Length == 0) continue;

                // Draw connections
                Handles.color = LineColor;
                var drawn = new System.Collections.Generic.HashSet<long>();
                foreach (var node in nodes)
                {
                    foreach (var nb in node._neighbours)
                    {
                        if (nb == null) continue;
                        long key = node.GetInstanceID() * 100000L + nb.GetInstanceID();
                        long keyRev = nb.GetInstanceID() * 100000L + node.GetInstanceID();
                        if (drawn.Contains(key) || drawn.Contains(keyRev)) continue;
                        drawn.Add(key);

                        Handles.DrawAAPolyLine(LineWidth,
                            node.transform.position + Vector3.up * 0.1f,
                            nb.transform.position + Vector3.up * 0.1f);
                    }
                }

                // Draw nodes
                foreach (var node in nodes)
                {
                    bool isSelected = Selection.activeGameObject == node.gameObject;
                    Handles.color = isSelected ? SelectedNodeColor : NodeColor;
                    Handles.SphereHandleCap(0, node.transform.position + Vector3.up * 0.1f,
                        Quaternion.identity, NodeRadius * 2f, EventType.Repaint);
                }
            }
        }
    }
}
#endif
