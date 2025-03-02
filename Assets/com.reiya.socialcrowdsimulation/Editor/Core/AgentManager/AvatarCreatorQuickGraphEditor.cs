using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
namespace CollisionAvoidance{
[CustomEditor(typeof(AvatarCreatorQuickGraph), true)]
public class AvatarCreatorQuickGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        AvatarCreatorQuickGraph script = (AvatarCreatorQuickGraph)target;

        GUILayout.BeginVertical("box");

        GUILayout.Label("Avatar Create Buttons", EditorStyles.boldLabel);

        if (GUILayout.Button("Instantiate Avatars"))
        {
            script.InstantiateAvatars();
        }

        if (GUILayout.Button("Delete Avatars"))
        {
            script.DeleteAvatars();
        }
        GUILayout.EndVertical();
    }
}
}
#endif