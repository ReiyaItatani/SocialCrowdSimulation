using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{

    public class QuickGraph : MonoBehaviour
    {

        [System.Serializable]
        public struct Edge
        {
            public QuickGraphNode _n1;
            public QuickGraphNode _n2;
        }

        public List<QuickGraphNode> _nodes = new List<QuickGraphNode>();
        //public List<Edge> _edges = new List<Edge>();


        #region CREATION AND DESTRUCTION

        protected virtual void Awake() 
        {
            CheckNeighbourhood();        
        }

        public virtual void CheckNeighbourhood()
        {
            _nodes = new List<QuickGraphNode>(GetComponentsInChildren<QuickGraphNode>());
            foreach (var node in _nodes)
            {
                node.CheckNeighbourhood();
            }
        }

        #endregion

    }

}


