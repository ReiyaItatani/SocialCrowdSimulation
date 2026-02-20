using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace CollisionAvoidance{
public class CollisionAvoidanceMenuItems
{
    // Menu item moved to SocialCrowdSimulationWindow.
    // Keeping utility methods (CreateAvatarCreator, AddTag) for shared use.

    public static void SetupSceneTags()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        AddTag("Obstacle");
    }

    protected static GameObject CreateAvatarCreator(string scriptName)
    {
        GameObject avatarCreator = new GameObject("AvatarCreator");

        if (scriptName == "AvatarCreatorQuickGraph")
        {
            avatarCreator.AddComponent<AvatarCreatorQuickGraph>();
        }
        return avatarCreator;
    }

    protected static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag)) { found = true; break; }
        }

        if (!found)
        {
            tagsProp.arraySize++;
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
}
#endif