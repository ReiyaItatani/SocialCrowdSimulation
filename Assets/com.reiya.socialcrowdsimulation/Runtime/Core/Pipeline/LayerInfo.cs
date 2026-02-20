using UnityEngine;

namespace CollisionAvoidance
{
    /// <summary>
    /// Lightweight descriptor attached to each pipeline layer GameObject.
    /// Displays a description box in the Inspector so developers can quickly
    /// understand each layer's role without reading source code.
    /// </summary>
    public class LayerInfo : MonoBehaviour
    {
        [HideInInspector]
        public string layerName;

        [HideInInspector]
        public string description;
    }
}
