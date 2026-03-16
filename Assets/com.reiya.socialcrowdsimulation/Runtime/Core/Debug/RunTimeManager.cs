using UnityEngine;

#if UNITY_EDITOR
namespace CollisionAvoidance
{
    /// <summary>
    /// Manages the runtime speed of the simulation by adjusting the time scale.
    /// The speed can be modified in real-time using a slider in the Unity Inspector.
    /// Editor-only: this component should not be included in production builds.
    /// </summary>
    public class RunTimeManager : MonoBehaviour
    {
        /// <summary>
        /// Controls the speed of the simulation, ranging from 0 to 10.
        /// Adjusting this value changes Unity's time scale.
        /// </summary>
        [Range(0f, 10f)]
        public float runTimeSpeed = 1.0f;

        /// <summary>
        /// Updates the time scale whenever the value is modified in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            Time.timeScale = runTimeSpeed;
        }
    }
}
#endif
