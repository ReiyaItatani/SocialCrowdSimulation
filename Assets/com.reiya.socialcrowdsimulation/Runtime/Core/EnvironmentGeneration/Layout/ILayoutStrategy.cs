using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    public interface ILayoutStrategy
    {
        LayoutResult Build(LayoutConfig config, int seed, Transform parent);
    }
}
