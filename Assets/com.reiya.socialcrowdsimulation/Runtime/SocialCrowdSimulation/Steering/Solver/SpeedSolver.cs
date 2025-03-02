using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

namespace CollisionAvoidance{
    public class SpeedSolver : ForceSolver
    {
        [Header("Speed")]
        public float currentSpeed = 1.0f; //Current speed of the agent
        [Range (0.0f, 1.0f)]
        public float initialSpeed = 0.7f; //Initial speed of the agent
        public float minSpeed = 0.0f; //Minimum speed of the agent
        public float maxSpeed = 1.0f; //Maximum speed of the agent
        public bool onInSlowingArea = false; //the event when the agent is in the slowing radius
        public float slowingRadius = 2.0f;
        private UnityAction OnGoalReached; //the event when the agent reaches the goal

        protected virtual void InitSpeedSolver(){
            currentSpeed = initialSpeed;
            if (initialSpeed < minSpeed)
            {
                initialSpeed = minSpeed;
            }
        }

        protected virtual void StartUpdateSpeed(){
            //Update the speed of the agent based on the distance to the goal
            StartCoroutine(UpdateSpeed(collisionAvoidance.GetAgentGameObject()));
            StartCoroutine(UpdateSpeedBasedOnGoalDist(0.1f));
            StartCoroutine(CheckForGoalReached(0.1f));
        }

        #region SPEED ADJUSTMENT 
        protected virtual IEnumerator UpdateSpeed(GameObject myself, float updateTime = 0.1f, float speedChangeRate = 0.05f){
            if(GetGroupName() == "Individual"){
                StartCoroutine(DecreaseSpeedBaseOnUpperBodyAnimation(updateTime));
                yield return null;
            }else{
                float averageSpeed = 0.0f;
                List<GameObject> groupAgents = GetGroupAgents(); 
                foreach(GameObject go in groupAgents){
                    IParameterManager parameterManager = go.GetComponent<IParameterManager>();
                    averageSpeed += parameterManager.GetCurrentSpeed();
                }
                averageSpeed /= groupAgents.Count;

                averageSpeed = averageSpeed - 0.1f*groupAgents.Count;
                if(averageSpeed<minSpeed){
                    averageSpeed = minSpeed;
                }
                while(true){
                    Vector3 centerOfMass = CalculateCenterOfMass(groupAgents, myself);
                    Vector3 directionToCenterOfMass = (centerOfMass - (Vector3)GetCurrentPosition()).normalized;
                    Vector3 myForward = GetCurrentDirection();
                    float distFromMeToCenterOfMass = Vector3.Distance(GetCurrentPosition(), centerOfMass);

                    //0.3f is the radius of the agent
                    float safetyDistance = 0.05f;
                    float speedChangeDist = groupAgents.Count * 0.3f + safetyDistance;

                    if(distFromMeToCenterOfMass > speedChangeDist){
                        float dotProduct = Vector3.Dot(myForward, directionToCenterOfMass);
                        //lowest speed or average speed?
                        if (dotProduct > 0)
                        {
                            //accelerate when the center of mass is in front of me
                            if(GetCurrentSpeed() <= maxSpeed){
                                currentSpeed += speedChangeRate; 
                            }else{
                                currentSpeed = maxSpeed;
                            }
                        }
                        else
                        {
                            //decelerate when the center of mass is behind
                            if(GetCurrentSpeed() >= minSpeed){
                                currentSpeed -= speedChangeRate;
                            }else{
                                currentSpeed = minSpeed;
                            }
                        }
                    }else{
                        currentSpeed = averageSpeed;
                    }
                    yield return new WaitForSeconds(updateTime);
                }
            }
        }

        protected virtual IEnumerator DecreaseSpeedBaseOnUpperBodyAnimation(float updateTime){
            float initialSpeed = GetCurrentSpeed();
            while(true){
                if(collisionAvoidance.GetUpperBodyAnimationState() == UpperBodyAnimationState.SmartPhone){
                    currentSpeed = minSpeed;
                }else{
                    currentSpeed = initialSpeed;
                }
                yield return new WaitForSeconds(updateTime);
            }
        }

        protected virtual IEnumerator UpdateSpeedBasedOnGoalDist(float updateTime){
            OnGoalReached += () =>
            {
                float duration = 2.0f;
                StartCoroutine(SpeedChanger(duration, currentSpeed, initialSpeed));
            };
            while(true){
                float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
                if(distanceToGoal < slowingRadius) currentSpeed = Mathf.Lerp(minSpeed, currentSpeed, distanceToGoal / slowingRadius);
                
                yield return new WaitForSeconds(updateTime);
            }
        }

        protected virtual IEnumerator SpeedChanger(float duration, float _currentSpeed, float targetSpeed){
            float elapsedTime = 0.0f;
            while(elapsedTime < duration){
                elapsedTime += Time.deltaTime;
                currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, elapsedTime/duration);
                yield return new WaitForSeconds(Time.deltaTime);
            }
            currentSpeed = targetSpeed;

            yield return null;
        }
        #endregion

        /********************************************************************************************************************************
        * Update Attractions
        ********************************************************************************************************************************/
        #region OTHER
        protected virtual IEnumerator CheckForGoalReached(float updateTime){
            while(true){
                float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
                if(distanceToGoal < slowingRadius){
                    onInSlowingArea = true;
                }else{
                    onInSlowingArea = false;
                }
                yield return new WaitForSeconds(updateTime);
            }
        }
        #endregion        
    }
}
