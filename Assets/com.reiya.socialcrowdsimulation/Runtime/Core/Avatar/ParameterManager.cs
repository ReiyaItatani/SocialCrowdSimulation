using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Manages parameter retrieval for collision avoidance.
    /// Provides access to an agent's movement-related data, such as position, speed, and avoidance vectors, 
    /// by delegating calls to the associated PathController.
    /// </summary>
    public class ParameterManager : CrowdSimulationMonoBehaviour, IParameterManager
    {
        /// <summary>
        /// The PathController instance that provides movement-related data.
        /// </summary>
        public AgentPathController pathController;

        /// <summary>
        /// Gets the agent's current movement direction.
        /// </summary>
        /// <returns>The current movement direction as a Vector3.</returns>
        public Vector3 GetCurrentDirection()
        {
            return pathController.GetCurrentDirection();
        }

        /// <summary>
        /// Gets the agent's current position.
        /// </summary>
        /// <returns>The current position as a Vector3.</returns>
        public Vector3 GetCurrentPosition()
        {
            return pathController.GetCurrentPosition();
        }

        /// <summary>
        /// Gets the agent's current speed.
        /// </summary>
        /// <returns>The current speed as a float.</returns>
        public float GetCurrentSpeed()
        {
            return pathController.GetCurrentSpeed();
        }

        /// <summary>
        /// Gets the agent's social relations, which may influence its movement or decision-making.
        /// </summary>
        /// <returns>The SocialRelations instance associated with the agent.</returns>
        public string GetGroupName()
        {
            return pathController.GetGroupName();
        }

        /// <summary>
        /// Gets the AvatarCreatorBase associated with the agent.
        /// </summary>
        /// <returns>The AvatarCreatorBase instance.</returns>
        public List<GameObject> GetGroupAgents()
        {
            return pathController.GetGroupAgents();
        }

        /// <summary>
        /// Gets the current avoidance vector, representing the direction to avoid collisions.
        /// </summary>
        /// <returns>The avoidance vector as a Vector3.</returns>
        public Vector3 GetCurrentAvoidanceVector()
        {
            return pathController.GetCurrentAvoidanceVector();
        }

        /// <summary>
        /// Gets the potential target that the agent may need to avoid.
        /// </summary>
        /// <returns>The GameObject representing the avoidance target, or null if none.</returns>
        public GameObject GetPotentialAvoidanceTarget()
        {
            return pathController.GetPotentialAvoidanceTarget();
        }

        /// <summary>
        /// Gets the PathController instance managing the agent's path.
        /// </summary>
        /// <returns>The PathController instance.</returns>
        public AgentPathController GetPathController()
        {
            return pathController;
        }

        /// <summary>
        /// Gets the CollisionAvoidanceController responsible for handling collision avoidance logic.
        /// </summary>
        /// <returns>The CollisionAvoidanceController instance.</returns>
        public CollisionAvoidanceController GetCollisionAvoidanceController()
        {
            return pathController.GetCollisionAvoidanceController();
        }

        /// <summary>
        /// Checks if the agent is inside a slowing area, which may affect its movement speed.
        /// </summary>
        /// <returns>True if the agent is in a slowing area, otherwise false.</returns>
        public bool GetOnInSlowingArea()
        {
            return pathController.GetOnInSlowingArea();
        }
    }
}
