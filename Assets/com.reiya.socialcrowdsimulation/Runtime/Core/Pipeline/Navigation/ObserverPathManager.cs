using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Patrol path manager for the observer (camera) agent.
    /// Instead of random walking, follows a pre-computed route
    /// and reverses direction at each endpoint (ping-pong).
    /// </summary>
    public class ObserverPathManager : AgentPathManager
    {
        [SerializeField] private List<QuickGraphNode> _patrolRoute;
        [SerializeField] private int _patrolIndex;
        [SerializeField] private int _patrolDirection = 1; // +1 forward, -1 backward

        /// <summary>
        /// Patrol route (read-only access for debugging).
        /// </summary>
        public IReadOnlyList<QuickGraphNode> PatrolRoute => _patrolRoute;

        /// <summary>
        /// Set a pre-computed patrol route and configure initial targets.
        /// </summary>
        public void InitializePatrolWithRoute(List<QuickGraphNode> route)
        {
            _patrolRoute = route ?? new List<QuickGraphNode>();

            if (_patrolRoute.Count == 0)
            {
                Debug.LogWarning("[ObserverPathManager] No patrol route. Falling back to stationary.");
                return;
            }

            // Log the computed route
            var positions = new List<string>();
            foreach (var node in _patrolRoute)
                positions.Add($"({node.transform.position.x:F1}, {node.transform.position.z:F1})");
            Debug.Log($"[ObserverPathManager] Patrol route ({_patrolRoute.Count} nodes): {string.Join(" → ", positions)}");

            if (_patrolRoute.Count == 1)
            {
                SetTargetNode(_patrolRoute[0]);
                _patrolIndex = 0;
                return;
            }

            SetTargetNode(_patrolRoute[0]);
            SetTargetNode(_patrolRoute[1]);
            _patrolIndex = 1;
            _patrolDirection = 1;
        }

        protected override void Update()
        {
            if (_patrolRoute == null || _patrolRoute.Count <= 1)
                return;

            float distanceToGoal = Vector3.Distance(
                coordinator.GetCurrentPosition(),
                CurrentTargetNodePosition);

            if (distanceToGoal < goalRadius)
            {
                int nextIndex = _patrolIndex + _patrolDirection;

                // Reverse at endpoints
                if (nextIndex < 0 || nextIndex >= _patrolRoute.Count)
                {
                    _patrolDirection *= -1;
                    nextIndex = _patrolIndex + _patrolDirection;
                }

                _patrolIndex = nextIndex;
                SetTargetNode(_patrolRoute[_patrolIndex]);
                OnTargetReached?.Invoke();
            }
        }
    }
}
