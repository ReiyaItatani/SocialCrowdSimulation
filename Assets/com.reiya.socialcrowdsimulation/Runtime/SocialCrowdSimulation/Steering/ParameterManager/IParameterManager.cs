using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Defines the required parameters for a ParameterManager implementation.
    /// Any class implementing this interface must provide methods to retrieve 
    /// essential movement and social relation data for collision avoidance.
    /// </summary>
    public interface IParameterManager
    {
        /// <summary>
        /// Gets the agent's current movement direction.
        /// Any custom ParameterManager must implement this function.
        /// </summary>
        /// <returns>The current movement direction as a Vector3.</returns>
        Vector3 GetCurrentDirection();

        /// <summary>
        /// Gets the agent's current position.
        /// Any custom ParameterManager must implement this function.
        /// </summary>
        /// <returns>The current position as a Vector3.</returns>
        Vector3 GetCurrentPosition();

        /// <summary>
        /// Gets the agent's current avoidance vector, which represents 
        /// the direction used to avoid collisions.
        /// Any custom ParameterManager must implement this function.
        /// </summary>
        /// <returns>The avoidance vector as a Vector3.</returns>
        Vector3 GetCurrentAvoidanceVector();

        /// <summary>
        /// Gets the agent's current speed.
        /// Any custom ParameterManager must implement this function.
        /// </summary>
        /// <returns>The current speed as a float.</returns>
        float GetCurrentSpeed();

        /// <summary>
        /// Gets the social relations associated with the agent, which 
        /// may influence movement behavior.
        /// Any custom ParameterManager must implement this function.
        /// </summary>
        /// <returns>The SocialRelations instance.</returns>
        string GetGroupName();
    }
}
