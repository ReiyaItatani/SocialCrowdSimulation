using UnityEngine;

/// <summary>
/// Base class for ScriptableObjects that need a public description field.
/// </summary>
public class DescriptionBaseSO : SerializableScriptableObject
{
	[TextArea(5, 10)] public string description;
}
