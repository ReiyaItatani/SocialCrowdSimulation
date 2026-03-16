using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Proxies parameter retrieval for collision avoidance.
    /// Provides access to an agent's movement-related data, such as position, speed, and avoidance vectors,
    /// by delegating calls to the associated AgentPipelineCoordinator.
    /// </summary>
    public class AvatarParameterProxy : CrowdSimulationMonoBehaviour, IParameterManager
    {
        /// <summary>
        /// The pipeline coordinator that provides all agent state.
        /// </summary>
        public AgentPipelineCoordinator coordinator;

        public Vector3 GetCurrentDirection()
        {
            return coordinator.GetCurrentDirection();
        }

        public Vector3 GetCurrentPosition()
        {
            return coordinator.GetCurrentPosition();
        }

        public float GetCurrentSpeed()
        {
            return coordinator.GetCurrentSpeed();
        }

        public string GetGroupName()
        {
            return coordinator.GetGroupName();
        }

        public List<GameObject> GetGroupAgents()
        {
            return coordinator.GetGroupAgents();
        }

        public Vector3 GetCurrentAvoidanceVector()
        {
            return coordinator.GetCurrentAvoidanceVector();
        }

        public GameObject GetPotentialAvoidanceTarget()
        {
            return coordinator.GetPotentialAvoidanceTarget();
        }

        public AgentPipelineCoordinator GetCoordinator()
        {
            return coordinator;
        }

        public CollisionAvoidanceController GetCollisionAvoidanceController()
        {
            return coordinator.GetCollisionAvoidanceController();
        }

        public bool GetOnInSlowingArea()
        {
            return coordinator.GetOnInSlowingArea();
        }
    }
}
