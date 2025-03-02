using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
    public class ReactSolver : SpeedSolver
    {   
        protected virtual void InitReactSolver(){
            AgentCollisionDetection agentCollisionDetection = collisionAvoidance.GetAgentCollisionDetection();
            agentCollisionDetection.OnEnterTrigger += HandleAgentCollision;     
        }

        //when the agent collide with the agent in front of it, it will take a step back
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
