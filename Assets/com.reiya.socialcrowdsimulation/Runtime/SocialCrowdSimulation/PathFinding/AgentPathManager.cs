using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CollisionAvoidance{
    public class AgentPathManager : MonoBehaviour
    {

        public float goalRadius = 2.0f;
        public AgentPathController pathController;
        public GroupNodeEventChannelSO groupNodeEventChannel;
        public UnityAction OnTargetReached;
        #region PROTECTED ATTRIBUTES

        [SerializeField, ReadOnly]
        protected QuickGraphNode _prevTargetNode = null;
        public Vector3 PrevTargetNodePosition => _prevTargetNode.transform.position;

        [SerializeField, ReadOnly]
        protected QuickGraphNode _currentTargetNode = null;
        public Vector3 CurrentTargetNodePosition => _currentTargetNode.transform.position;

        #endregion

        #region GET AND SET

        void OnEnable()
        {
            groupNodeEventChannel.OnEventRaised += UpdateGroupNode;
        }

        void OnDisable()
        {
            groupNodeEventChannel.OnEventRaised -= UpdateGroupNode;
        }

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
                return _currentTargetNode._neighbours[0];
            }
            
            List<QuickGraphNode> tmp = new List<QuickGraphNode>();
            foreach (var n in _currentTargetNode._neighbours)
            {
                if (n != _prevTargetNode)
                {
                    tmp.Add(n); 
                }
            }

            if(groupName == "Individual"){
                return tmp[Random.Range(0, tmp.Count)];
            }else{
                //TODO: Implement group pathfinding
                //If one person arrives at the target node, the rest of the group should also arrive at the target node
                QuickGraphNode groupNode =  tmp[Random.Range(0, tmp.Count)];
                groupNodeEventChannel.RaiseEvent(new GroupNode{graphNode = groupNode, groupName = groupName});
                return groupNode;
            }
        }

        public void UpdateGroupNode(GroupNode groupNode)
        {
            if(groupNode.groupName == pathController.groupName){
                SetTargetNode(groupNode.graphNode);
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