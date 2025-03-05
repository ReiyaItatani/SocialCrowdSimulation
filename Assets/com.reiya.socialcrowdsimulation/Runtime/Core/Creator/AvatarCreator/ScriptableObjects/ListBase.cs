using System;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionAvoidance{
    public class AgentListBase : ScriptableObject
    {
    }

    [Serializable]
    public class SpeedRange
    {
        public float minSpeed;
        public float maxSpeed;

        public SpeedRange(float minSpeed, float maxSpeed)
        {
            this.minSpeed = minSpeed;
            this.maxSpeed = maxSpeed;
        }
    }

}
