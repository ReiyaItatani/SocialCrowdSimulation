using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollisionAvoidance{
    public class AgentPathManager : MonoBehaviour
    {
        public float goalRadius = 2.0f;
        public AgentPathController pathController;
        public UnityAction OnTargetReached;
        #region PROTECTED ATTRIBUTES

        [SerializeField, ReadOnly]
        protected QuickGraphNode _prevTargetNode = null;
        public Vector3 PrevTargetNodePosition => _prevTargetNode._cachedTransform.position;

        [SerializeField, ReadOnly]
        protected QuickGraphNode _currentTargetNode = null;
        public Vector3 CurrentTargetNodePosition => _currentTargetNode._cachedTransform.position;

        #endregion

        #region GET AND SET

        public virtual void SetTargetNode(QuickGraphNode targetNode)
        {
            if (targetNode != _currentTargetNode)
            {
                _prevTargetNode = _currentTargetNode == null? targetNode : _currentTargetNode;
                _currentTargetNode = targetNode;
            }
        }

        public QuickGraphNode GetNextNode(string groupName)
        {
            if (_currentTargetNode._neighbours.Count == 1)
            {
                QuickGraphNode nextNode = _currentTargetNode._neighbours[0];
                NotifyGroupIfNeeded(nextNode);
                return nextNode;
            }

            List<QuickGraphNode> tmp = new List<QuickGraphNode>();
            foreach (var n in _currentTargetNode._neighbours)
            {
                if (n != _prevTargetNode)
                {
                    tmp.Add(n);
                }
            }

            QuickGraphNode selectedNode = tmp[Random.Range(0, tmp.Count)];
            NotifyGroupIfNeeded(selectedNode);
            return selectedNode;
        }

        private void NotifyGroupIfNeeded(QuickGraphNode node)
        {
            GroupManager gm = pathController.groupManager;
            if (gm != null)
            {
                gm.NotifyNextNode(node, gameObject);
            }
        }

        #endregion

        #region UPDATE

        protected virtual void Update()
        {
            float distanceToGoal = Vector3.Distance(pathController.GetCurrentPosition(), CurrentTargetNodePosition);
            if(distanceToGoal < goalRadius) {
                SetTargetNode(GetNextNode(pathController.groupName));
                OnTargetReached?.Invoke();
            }
        }

        #endregion

    }
}