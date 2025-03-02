using UnityEditor;
using UnityEngine;

namespace CollisionAvoidance
{
    [CustomEditor(typeof(QuickGraph))]
    public class QuickGraphEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            QuickGraph graph = (QuickGraph)target;

            if (GUILayout.Button("Check Neighbourhood"))
            {
                graph.CheckNeighbourhood();
            }
        }
    }
}
