using UnityEngine;
using UnityEditor;

namespace CollisionAvoidance
{
    [CustomEditor(typeof(LayerInfo))]
    public class LayerInfoEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LayerInfo info = (LayerInfo)target;

            if (!string.IsNullOrEmpty(info.layerName))
            {
                EditorGUILayout.LabelField(info.layerName, EditorStyles.boldLabel);
            }

            if (!string.IsNullOrEmpty(info.description))
            {
                EditorGUILayout.HelpBox(info.description, MessageType.Info);
            }
        }
    }
}
