using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;

namespace CollisionAvoidance{
    /// <summary>
    /// Manages the speed of an agent, adjusting based on distance to the goal and group dynamics.
    /// </summary>
    public class SpeedSolver : ForceSolver
    {
        [Header("Speed Settings")]
        [Range (0.0f, 1.0f)]
        public float initialSpeed = 0.7f; // Initial speed of the agent
        public float minSpeed = 0.0f; // Minimum speed limit
        public float maxSpeed = 1.0f; // Maximum speed limit
        public float slowingRadius = 3.0f; // Radius within which speed slows down
        
        private bool onInSlowingArea = false; // Indicates if the agent is within the slowing radius
        private UnityAction OnGoalReached; // Event triggered when the agent reaches the goal

        /// <summary>
        /// Initializes the speed solver, setting the initial speed and subscribing to the goal event.
        /// </summary>
        protected virtual void InitSpeedSolver(){
            CurrentSpeed = initialSpeed;
            if (initialSpeed < minSpeed)
            {
                initialSpeed = minSpeed;
            }
        }

        protected virtual void OnEnableSpeedSolver()
        {
            agentPathManager.OnTargetReached += () =>
            {
                OnGoalReached?.Invoke();
            };

            StartCoroutine(UpdateSpeed(collisionAvoidance.GetAgentGameObject()));
            StartCoroutine(UpdateSpeedBasedOnGoalDist(0.1f));
            StartCoroutine(CheckForGoalReached(0.1f));
        }

        protected virtual void OnDisableSpeedSolver()
        {
            agentPathManager.OnTargetReached -= () =>
            {
                OnGoalReached?.Invoke();
            };

            StopAllCoroutines();
        }

        #region SPEED ADJUSTMENT 
        /// <summary>
        /// Adjusts the speed dynamically based on group movement or individual behavior.
        /// </summary>
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
                    Vector3 centerOfMass = Math.CalculateCenterOfMass(groupAgents, myself);
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
                                CurrentSpeed += speedChangeRate; 
                            }else{
                                CurrentSpeed = maxSpeed;
                            }
                        }
                        else
                        {
                            //decelerate when the center of mass is behind
                            if(GetCurrentSpeed() >= minSpeed){
                                CurrentSpeed -= speedChangeRate;
                            }else{
                                CurrentSpeed = minSpeed;
                            }
                        }
                    }else{
                        CurrentSpeed = averageSpeed;
                    }
                    yield return new WaitForSeconds(updateTime);
                }
            }
        }

        /// <summary>
        /// Decreases speed if the agent is using a smartphone animation.
        /// </summary>
        protected virtual IEnumerator DecreaseSpeedBaseOnUpperBodyAnimation(float updateTime){
            float initialSpeed = GetCurrentSpeed();
            while(true){
                if(collisionAvoidance.GetUpperBodyAnimationState() == UpperBodyAnimationState.SmartPhone){
                    CurrentSpeed = minSpeed;
                }else{
                    CurrentSpeed = initialSpeed;
                }
                yield return new WaitForSeconds(updateTime);
            }
        }

        /// <summary>
        /// Updates speed based on the distance to the goal, slowing down when approaching.
        /// </summary>
        protected virtual IEnumerator UpdateSpeedBasedOnGoalDist(float updateTime){
            OnGoalReached += () =>
            {
                float duration = 2.0f;
                StartCoroutine(SpeedChanger(duration, CurrentSpeed, initialSpeed));
            };
            while(true){
                float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
                if(distanceToGoal < slowingRadius) CurrentSpeed = Mathf.Lerp(minSpeed, CurrentSpeed, distanceToGoal / slowingRadius);
                
                yield return new WaitForSeconds(updateTime);
            }
        }

        /// <summary>
        /// Smoothly transitions the speed over a duration.
        /// </summary>
        protected virtual IEnumerator SpeedChanger(float duration, float _currentSpeed, float targetSpeed){
            float elapsedTime = 0.0f;
            while(elapsedTime < duration){
                elapsedTime += Time.deltaTime;
                CurrentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, elapsedTime/duration);
                yield return new WaitForSeconds(Time.deltaTime);
            }
            CurrentSpeed = targetSpeed;

            yield return null;
        }
        #endregion

        /********************************************************************************************************************************
        * Update Attractions
        ********************************************************************************************************************************/
        #region OTHER
        /// <summary>
        /// Monitors whether the agent has entered the slowing area.
        /// </summary>
        protected virtual IEnumerator CheckForGoalReached(float updateTime){
            while(true){
                float distanceToGoal = Vector3.Distance(GetCurrentPosition(), GetCurrentGoal());
                onInSlowingArea = distanceToGoal < slowingRadius;
                yield return new WaitForSeconds(updateTime);
            }
        }
        #endregion    

        /// <summary>
        /// Returns whether the agent is in the slowing area.
        /// </summary>
        public bool GetOnInSlowingArea(){
            return onInSlowingArea;
        }    
    }
}
