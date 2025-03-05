using CollisionAvoidance;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// This class is used for Events that have one string argument
/// </summary>
[CreateAssetMenu(menuName = "SocialCrowdSimulation/Group Event Channel")]
public class GroupNodeEventChannelSO : DescriptionBaseSO
{
	public UnityAction<GroupNode> OnEventRaised;

	public void RaiseEvent(GroupNode value)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(value);
	}
}

public class GroupNode{
	public QuickGraphNode graphNode;
	public string groupName;
}
