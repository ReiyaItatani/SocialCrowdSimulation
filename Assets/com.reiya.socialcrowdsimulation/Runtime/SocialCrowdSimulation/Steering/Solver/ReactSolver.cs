using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
    /// <summary>
    /// ReactSolver handles agent reactions upon collision with other agents.
    /// Agents step back or move differently based on their social relationships.
    /// </summary>
    public class ReactSolver : SpeedSolver
    {   
        protected virtual void InitReactSolver(){
            AgentCollisionDetection agentCollisionDetection = collisionAvoidance.GetAgentCollisionDetection();
            agentCollisionDetection.OnEnterTrigger += HandleAgentCollision;     
        }

        /// <summary>
        /// Handles agent collisions by checking social relationships and triggering a reaction.
        /// If the collided agent belongs to the same group, a shorter step-back occurs.
        /// If the collided agent is from a different group, the reaction is stronger.
        /// </summary>
        /// <param name="other">The collider of the other agent.</param>
        protected virtual void HandleAgentCollision(Collider other){
            //Check the social realtionship between the collided agent and the agent
            string  mySocialRelations          = GetGroupName();
            IParameterManager otherAgentParameterManager = other.GetComponent<IParameterManager>();
            string  otherAgentSocialRelations  = otherAgentParameterManager.GetGroupName();

            if(onCollide == false){
                collidedAgent = other.gameObject;
                if(mySocialRelations != "Individual" && mySocialRelations == otherAgentSocialRelations){
                    //If the collided agent is in the same group
                    float distance = Vector3.Distance(GetCurrentPosition(), otherAgentParameterManager.GetCurrentPosition());
                    if(distance < 0.4f){
                        //If the collided agent is too close
                        StartCoroutine(ReactionToCollision(0.5f, 0.0f));
                    }
                }else{
                    //If the collided agent is not in the same group
                    StartCoroutine(ReactionToCollision(1.0f, 1.0f));
                }
            }
        }

        /// <summary>
        /// Executes a reaction to collision, making the agent step back and move differently.
        /// </summary>
        /// <param name="stepBackDuration">Duration to step back.</param>
        /// <param name="goDifferentDuration">Duration to move in a different direction.</param>
        /// <returns>Coroutine for the reaction sequence.</returns>
        protected virtual IEnumerator ReactionToCollision(float stepBackDuration, float goDifferentDuration)
        {
            onCollide = true;
            yield return new WaitForSeconds(stepBackDuration);
            onMoving = true;
            yield return new WaitForSeconds(goDifferentDuration);
            onCollide = false;
            onMoving = false;
        }
    }
}
