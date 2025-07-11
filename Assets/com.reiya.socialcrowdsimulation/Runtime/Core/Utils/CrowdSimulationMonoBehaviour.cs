using UnityEngine;

namespace CollisionAvoidance
{

    public class CrowdSimulationMonoBehaviour : MonoBehaviour
    {

        #region PUBLIC ATTRIBUTES

        public Transform _cachedTransform
        {
            get
            {
                if (m_CachedTransform == null)
                {
                    m_CachedTransform = transform;
                }

                return m_CachedTransform;
            }
        }
        protected Transform m_CachedTransform = null;

        #endregion

    }

}


