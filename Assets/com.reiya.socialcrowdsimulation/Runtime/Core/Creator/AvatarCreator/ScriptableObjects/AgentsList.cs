using System;
using System.Collections.Generic;
using UnityEngine;
namespace CollisionAvoidance{
    [CreateAssetMenu(fileName = "AgentsList", menuName = "SocialCrowdSimulation/Agent List")]
    public class AgentsList : AgentListBase
    {
        public IndividualEntry individuals;
        [Tooltip("List of defined social groups with their member counts and assigned agents.")]
        public List<GroupEntry> groups;

        /// <summary>
        /// Ensures all groups have correctly sized agent lists.
        /// </summary>
        private void OnValidate()
        {
            if (groups != null)
            {
                foreach (var group in groups)
                {
                    group.Validate();
                }
            }
        }
    }
    [Serializable]
    public class GroupEntry
    {
        [Tooltip("The name of the group (e.g., Couple, Family, Team).")]
        public string groupName;

        [Tooltip("The number of members in this group. Must be 2 or more.")]
        [Min(2)]
        public int count;

        [Tooltip("List of agents belonging to this group.")]
        public List<GameObject> agents = new List<GameObject>();

        [Tooltip("The speed range for this group.")]
        public SpeedRange speedRange = new SpeedRange(0.1f, 1.0f); // Default values

        /// <summary>
        /// Ensures the agents list matches the count value.
        /// This will be triggered when changes are made in the Inspector.
        /// </summary>
        public void Validate()
        {
            if (agents.Count < count)
            {
                while (agents.Count < count)
                    agents.Add(null); // Add empty slots if count is increased
            }
            else if (agents.Count > count)
            {
                agents.RemoveRange(count, agents.Count - count); // Trim excess agents
            }
        }
    }
    [Serializable]
    public class IndividualEntry
    {
        [Tooltip("List of agents belonging to this individual.")]
        public List<GameObject> agents = new List<GameObject>();

        [Tooltip("The speed range for this individual.")]
        public SpeedRange speedRange = new SpeedRange(0.1f, 1.0f); // Default values
    }
}