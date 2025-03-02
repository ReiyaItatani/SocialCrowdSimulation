using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
namespace CollisionAvoidance{
public class CollisionAvoidanceMenuItems
{
    [MenuItem("CollisionAvoidance/Create AvatarCreator")]
    protected static void Create()
    {
        AddTag("Agent");
        AddTag("Group");
        AddTag("Wall");
        AddTag("Object");
        GameObject avatarCreator = CreateAvatarCreator("AvatarCreatorQuickGraph");
        Debug.Log("AvatarCreator created");
    }

    // Creates the AvatarCreator game object and checks for the presence of the OVRLipSync script in the scene.
    protected static GameObject CreateAvatarCreator(string scriptName)
    {
        // Check if OVRLipSync is already present in the scene
        if (GameObject.FindObjectOfType<OVRLipSync>() == null)
        {
            // If not present, create a new game object and attach the OVRLipSync script
            GameObject ovrLipSyncObject = new GameObject("OVRLipSyncObject");
            ovrLipSyncObject.AddComponent<OVRLipSync>();
            Debug.Log("OVRLipSync game object created and script attached.");
        }

        // Create the AvatarCreator game object
        GameObject avatarCreator = new GameObject("AvatarCreator");

        // Add the script component based on the scriptName
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