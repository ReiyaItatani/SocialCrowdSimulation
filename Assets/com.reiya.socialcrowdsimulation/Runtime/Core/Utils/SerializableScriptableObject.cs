using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A custom ScriptableObject class that automatically assigns a unique GUID to each instance for easier management and tracking within Unity projects.
/// </summary>
public class SerializableScriptableObject : ScriptableObject
{
	[SerializeField, HideInInspector] private string _guid;
	public string Guid => _guid;

#if UNITY_EDITOR
	void OnValidate()
	{
		var path = AssetDatabase.GetAssetPath(this);
		_guid = AssetDatabase.AssetPathToGUID(path);
	}
#endif
}
