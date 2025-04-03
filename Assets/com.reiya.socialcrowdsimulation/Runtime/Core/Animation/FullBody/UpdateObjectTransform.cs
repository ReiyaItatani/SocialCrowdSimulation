using UnityEngine;
using Unity.Mathematics;

namespace CollisionAvoidance
{
    public class UpdateObjectTransform : MonoBehaviour
    {
        public Transform TargetTransform;
        public AgentPathController CharacterController;
        // Maximum turn angle per second (in degrees)
        [SerializeField]
        private float maxTurnAnglePerSecond = 90f;

        // Stores the previous direction (in world coordinates)
        private Vector3 _lastDirection;

        private void Awake()
        {
            InitTransform();
            // Initialize the last direction
            _lastDirection = transform.forward;
        }

        private void InitTransform()
        {
            transform.position = CharacterController.GetWorldInitPosition();
            Vector3 initDir = CharacterController.GetWorldInitDirection();
            transform.rotation = quaternion.LookRotation(initDir, Vector3.up);

            // Keep _lastDirection in sync
            _lastDirection = initDir.normalized;
        }

        public void UpdateTransform(){
            if(TargetTransform != null) UpdateTransform(TargetTransform);
        }

        private void UpdateTransform(Transform targetTransform)
        {
            // Update position
            targetTransform.position = CharacterController.GetCurrentPosition();

            // Calculate target direction for this frame
            Vector3 targetDirection = CharacterController.GetCurrentDirection().normalized;

            // Determine the angle between last frame's direction and this frame's target direction
            float angleDiff = Vector3.Angle(_lastDirection, targetDirection);

            // Determine the maximum allowed rotation this frame (in degrees)
            float maxTurnAngleThisFrame = maxTurnAnglePerSecond * Time.deltaTime;

            // If the angle difference exceeds our maximum allowed turn angle,
            // interpolate (slerp) between the old direction and the target direction
            if (angleDiff > maxTurnAngleThisFrame)
            {
                float t = maxTurnAngleThisFrame / angleDiff;
                targetDirection = Vector3.Slerp(_lastDirection, targetDirection, t);
            }

            // Update rotation
            targetTransform.rotation = quaternion.LookRotation(targetDirection, Vector3.up);

            // Store the new direction as the last direction
            _lastDirection = targetDirection;
        }
    }
}
