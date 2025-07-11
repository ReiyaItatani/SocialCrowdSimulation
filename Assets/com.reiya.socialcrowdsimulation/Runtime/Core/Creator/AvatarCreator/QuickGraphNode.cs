using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    public class QuickGraphNode : CrowdSimulationMonoBehaviour
    {

        public List<QuickGraphNode> _neighbours = new List<QuickGraphNode>();

        public virtual void AddNeighbour(QuickGraphNode node)
        {
            if (!_neighbours.Contains(node))
            {
                _neighbours.Add(node);
            }
        }

        /// <summary>
        /// Ensure that the neighbours of this node accounts this node as a neighbour on their list. 
        /// </summary>
        public virtual void CheckNeighbourhood()
        {
            _neighbours.Remove(this);

            foreach (QuickGraphNode node in _neighbours)
            {
                node.AddNeighbour(this);
            }
        }

        protected virtual void OnDrawGizmos()
        {
            //Sphere
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.1f);
            //Line
            Gizmos.color = Color.black;
            
            foreach (var n in _neighbours)
            {
                Gizmos.DrawLine(transform.position, n.transform.position);
            }
        }

    }
}