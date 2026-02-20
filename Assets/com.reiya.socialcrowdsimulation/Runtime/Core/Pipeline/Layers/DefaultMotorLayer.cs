using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// L5: Motor Constraints layer.
    /// Speed management and path simulation.
    /// Applies max speed, acceleration limits, and goal slowing.
    ///
    /// All external data (goal position, animation state, speed config, group data)
    /// is received per-tick via MotorContext and GroupContext. No hidden dependencies.
    /// </summary>
    public class DefaultMotorLayer : MonoBehaviour, IMotorLayer
    {
        // Speed management state (persists across ticks)
        private float currentSpeed;
        private bool isInSlowingArea;
        private bool groupSpeedInitialized;
        private float groupAverageSpeed;

        // Speed transition state
        private bool isTransitioning;
        private float transitionTimer;
        private float transitionDuration;
        private float transitionFromSpeed;
        private float transitionToSpeed;

        // Timer for periodic speed updates
        private float speedUpdateTimer;
        private const float SpeedUpdateInterval = 0.1f;

        private bool initialized;

        public void Initialize(float initialSpeed)
        {
            currentSpeed = initialSpeed;
            initialized = true;
        }

        public void UpdateInitialSpeed(float newInitialSpeed, float minSpeed)
        {
            if (newInitialSpeed < minSpeed)
            {
                newInitialSpeed = minSpeed;
            }
            currentSpeed = newInitialSpeed;
        }

        public float GetCurrentSpeed() => currentSpeed;

        public MotorOutput Tick(DecisionOutput decision, AgentFrame frame, MotorContext motor,
            GroupContext group, float deltaTime)
        {
            if (!initialized) return new MotorOutput(frame.Position, frame.Direction, frame.Speed, false);

            // Update speed (periodic, like coroutine intervals)
            speedUpdateTimer += deltaTime;
            if (speedUpdateTimer >= SpeedUpdateInterval)
            {
                speedUpdateTimer = 0f;
                UpdateSpeed(frame, motor, group);
                UpdateSpeedBasedOnGoalDist(frame.Position, motor);
                CheckForGoalReached(frame.Position, motor);
            }

            // Handle speed transition (smooth lerp)
            if (isTransitioning)
            {
                transitionTimer += deltaTime;
                if (transitionTimer >= transitionDuration)
                {
                    currentSpeed = transitionToSpeed;
                    isTransitioning = false;
                }
                else
                {
                    currentSpeed = Mathf.Lerp(transitionFromSpeed, transitionToSpeed, transitionTimer / transitionDuration);
                }
            }

            // Apply motor constraints
            Vector3 direction = decision.DesiredDirection;
            Vector3 nextPosition = frame.Position + direction * currentSpeed * deltaTime;

            return new MotorOutput(nextPosition, direction, currentSpeed, isInSlowingArea);
        }

        #region Speed Management

        private void UpdateSpeed(AgentFrame frame, MotorContext motor, GroupContext group)
        {
            if (!group.IsInGroup)
            {
                UpdateIndividualSpeed(motor);
                return;
            }

            if (group.Members == null || group.Members.Count == 0)
            {
                UpdateIndividualSpeed(motor);
                return;
            }

            // Initialize group average speed once
            if (!groupSpeedInitialized)
            {
                float totalSpeed = 0f;
                int count = 0;
                foreach (GroupMember member in group.Members)
                {
                    totalSpeed += member.Speed;
                    count++;
                }
                if (count > 0)
                {
                    groupAverageSpeed = totalSpeed / count - 0.1f * (count + 1); // +1 for self
                    if (groupAverageSpeed < motor.MinSpeed) groupAverageSpeed = motor.MinSpeed;
                    groupSpeedInitialized = true;
                }
                return;
            }

            // Group speed adjustment based on distance to center of mass
            Vector3 centerOfMass = group.CenterOfMass;
            Vector3 dirToCom = (centerOfMass - frame.Position).normalized;
            float distToCom = Vector3.Distance(frame.Position, centerOfMass);

            float safetyDistance = 0.05f;
            float speedChangeDist = (group.Members.Count + 1) * 0.3f + safetyDistance; // +1 for self
            float speedChangeRate = 0.05f;

            if (distToCom > speedChangeDist)
            {
                float dotProduct = Vector3.Dot(frame.Direction, dirToCom);
                if (dotProduct > 0)
                {
                    currentSpeed = Mathf.Min(currentSpeed + speedChangeRate, motor.MaxSpeed);
                }
                else
                {
                    currentSpeed = Mathf.Max(currentSpeed - speedChangeRate, motor.MinSpeed);
                }
            }
            else
            {
                currentSpeed = groupAverageSpeed;
            }
        }

        private void UpdateIndividualSpeed(MotorContext motor)
        {
            if (motor.AnimationState == UpperBodyAnimationState.SmartPhone)
            {
                currentSpeed = motor.MinSpeed;
            }
            else if (currentSpeed < motor.InitialSpeed)
            {
                // Restore speed when transitioning back from SmartPhone to Walk/Talk
                currentSpeed = motor.InitialSpeed;
            }
        }

        private void UpdateSpeedBasedOnGoalDist(Vector3 currentPosition, MotorContext motor)
        {
            // Skip slowing during speed transition (transition handles recovery after goal switch)
            if (isTransitioning) return;

            float distToGoal = Vector3.Distance(currentPosition, motor.GoalPosition);
            if (distToGoal < motor.SlowingRadius)
            {
                // Use MaxSpeed as reference to avoid cumulative exponential decay.
                // Previous approach used currentSpeed which decayed exponentially each tick,
                // causing agents to nearly stop before reaching goalRadius.
                float desiredSpeed = Mathf.Lerp(motor.MinSpeed, motor.MaxSpeed, distToGoal / motor.SlowingRadius);
                currentSpeed = Mathf.Min(currentSpeed, desiredSpeed);
            }
        }

        private void CheckForGoalReached(Vector3 currentPosition, MotorContext motor)
        {
            float distToGoal = Vector3.Distance(currentPosition, motor.GoalPosition);
            isInSlowingArea = distToGoal < motor.SlowingRadius;
        }

        /// <summary>
        /// Called when a path target is reached to smoothly transition speed back to initial.
        /// </summary>
        public void OnTargetReached(float targetInitialSpeed)
        {
            isTransitioning = true;
            transitionTimer = 0f;
            transitionDuration = 0.5f;
            transitionFromSpeed = currentSpeed;
            transitionToSpeed = targetInitialSpeed;
        }

        #endregion

    }
}
