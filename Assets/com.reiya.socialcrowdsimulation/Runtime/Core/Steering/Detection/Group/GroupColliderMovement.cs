using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace CollisionAvoidance{

    public class GroupColliderMovement : SharedFOV
    {
        public GameObject groupColliderGameObject;
        private CapsuleCollider groupCollider;
        protected virtual void InitColliderMovementCoUpdate()
        {
            groupCollider         = groupColliderGameObject.GetComponent<CapsuleCollider>();
            groupParameterManager = groupColliderGameObject.GetComponent<GroupParameterManager>();
        }

        protected virtual void GroupColliderMovementCoUpdate()
        {
            UpdateCenterOfMass();
            DistanceChecker();
        }

        // Update the center of mass of the group
        void UpdateCenterOfMass()
        {
            Vector3 combinedPosition = Vector3.zero;
            foreach (GameObject agent in agentsInCategory)
            {
                combinedPosition += agent.transform.position;
            }
            this.transform.position = combinedPosition / agentsInCategory.Count;
        }

        // Check if the agents are close enough to each other
        private void DistanceChecker(){
            float maxDistance = 0f;
            foreach (GameObject agent in agentsInCategory)
            {
                float distance = Vector3.Distance(this.transform.position, agent.transform.position);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }
            if(maxDistance <= (agentsInCategory.Count)/2){
                groupCollider.enabled = true;
                onGroupCollider = true;
            }else{
                groupCollider.enabled = false;
                onGroupCollider = false;
            }
        }
    }
}