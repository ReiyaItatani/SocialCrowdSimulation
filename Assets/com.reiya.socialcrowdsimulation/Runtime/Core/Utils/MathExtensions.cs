using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CollisionAvoidance
{
    public static class Math
    {    
        public static Vector3 CalculateCenterOfMass(List<GameObject> groupAgents, GameObject myself = null)
        {
            if (groupAgents == null || groupAgents.Count == 0)
            {
                return Vector3.zero;
            }

            var validAgents = groupAgents.Where(go => go != null && go != myself).ToList();
            if (validAgents.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 sumOfPositions = validAgents.Aggregate(Vector3.zero, (sum, go) => sum + go.transform.position);
            return sumOfPositions / validAgents.Count;
        }
    }
}
