using UnityEngine;

namespace CollisionAvoidance.EnvironmentGeneration
{
    /// <summary>
    /// Factory for creating wall, floor, and obstacle GameObjects
    /// with correct tags, colliders, and NormalVector components.
    ///
    /// Wall convention:
    ///   localScale = (thickness, height, spanLength)
    ///   transform.forward = spanDirection (used by NormalVector)
    ///   local X = wall thickness, local Y = wall height, local Z = wall span
    /// </summary>
    public static class WallFactory
    {
        private static readonly Color WallColor = new Color(0.7f, 0.7f, 0.7f);
        private static readonly Color FloorColor = new Color(0.85f, 0.85f, 0.8f);
        private static readonly Color ObstacleColor = new Color(0.5f, 0.5f, 0.6f);

        /// <summary>
        /// Creates a wall cube with "Wall" tag, BoxCollider, and NormalVector.
        /// </summary>
        /// <param name="position">World center of the wall.</param>
        /// <param name="thickness">Wall thickness (local X).</param>
        /// <param name="height">Wall height (local Y).</param>
        /// <param name="spanLength">Wall span length (local Z = forward direction).</param>
        /// <param name="spanDirection">The direction along which the wall spans.
        /// NormalVector uses transform.forward as the wall direction for repulsion.</param>
        /// <param name="parent">Parent transform.</param>
        /// <param name="name">Name for the GameObject.</param>
        public static GameObject CreateWall(
            Vector3 position,
            float thickness,
            float height,
            float spanLength,
            Vector3 spanDirection,
            Transform parent,
            string name = "Wall")
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.tag = "Wall";
            wall.transform.parent = parent;
            wall.transform.position = position;

            // Set rotation FIRST so localScale applies in rotated local space.
            // forward (local Z) = span direction = NormalVector's wallDirection.
            if (spanDirection.sqrMagnitude > 0.001f)
            {
                wall.transform.rotation = Quaternion.LookRotation(spanDirection.normalized, Vector3.up);
            }

            // Scale in local space: X=thickness, Y=height, Z=spanLength
            wall.transform.localScale = new Vector3(thickness, height, spanLength);

            wall.AddComponent<NormalVector>();
            SetColor(wall, WallColor);
            wall.isStatic = true;

            return wall;
        }

        /// <summary>
        /// Creates a floor plane.
        /// Unity Plane is 10x10 units by default, scaled accordingly.
        /// </summary>
        public static GameObject CreateFloor(
            Vector3 center,
            float width,
            float depth,
            Transform parent,
            string name = "Floor")
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = name;
            floor.transform.parent = parent;
            floor.transform.position = center;
            // Unity Plane default is 10x10, so scale = desired / 10
            floor.transform.localScale = new Vector3(width / 10f, 1f, depth / 10f);

            SetColor(floor, FloorColor);
            floor.isStatic = true;

            return floor;
        }

        /// <summary>
        /// Creates an obstacle cube with "Obstacle" tag, BoxCollider, and NormalVector.
        /// </summary>
        public static GameObject CreateObstacle(
            Vector3 position,
            Vector3 size,
            Transform parent,
            string name = "Obstacle")
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.tag = "Obstacle";
            obstacle.transform.parent = parent;
            obstacle.transform.position = new Vector3(position.x, position.y + size.y / 2f, position.z);
            obstacle.transform.localScale = size;

            obstacle.AddComponent<NormalVector>();
            SetColor(obstacle, ObstacleColor);
            obstacle.isStatic = true;

            return obstacle;
        }

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = color
                };
            }
        }
    }
}
