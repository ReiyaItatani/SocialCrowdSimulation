using System.Collections;
using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Handles physics trigger detection and response for an agent in a virtual environment.
    /// Utilizes a CapsuleCollider for physics trigger detection and interacts with
    /// the AvatarParameterProxy to adjust the agent's movement based on collisions.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public class AgentPhysicsTrigger : MonoBehaviour
    {
        private const string AgentTag = "Agent";
        private const string WallTag = "Wall";

        [Header("Collision Handling Parameters")]
        private AvatarParameterProxy avatarParameterProxy;
        private CapsuleCollider capsuleCollider;
        // private bool isColliding;

        [Header("Repulsion Force Parameters")]
        private GameObject currentWallTarget;

        public delegate void TriggerEvent(Collider other);
        public event TriggerEvent OnEnterTrigger;

        private void Awake()
        {
            InitializeComponents();
        }

        private void Update()
        {
            SyncColliderCenter();
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleCollisionEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            HandleCollisionExit(other);
        }

        public GameObject GetCurrentWallTarget()
        {
            return currentWallTarget;
        }

        private void InitializeComponents()
        {
            avatarParameterProxy = GetComponent<AvatarParameterProxy>();
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        private void SyncColliderCenter()
        {
            if (avatarParameterProxy == null) return;

            Vector3 currentPosition = avatarParameterProxy.GetCurrentPosition();
            Vector3 offset = new Vector3(currentPosition.x - transform.position.x, capsuleCollider.center.y, currentPosition.z - transform.position.z);
            capsuleCollider.center = offset;
        }

        private void HandleCollisionEnter(Collider other)
        {
            if (other.CompareTag(AgentTag))
            {
                OnEnterTrigger?.Invoke(other);
            }
            else if (other.CompareTag(WallTag))
            {
                currentWallTarget = other.gameObject;
            }
        }

        private void HandleCollisionExit(Collider other)
        {
            if (other.CompareTag(WallTag))
            {
                currentWallTarget = null;
            }
        }
    }
}
