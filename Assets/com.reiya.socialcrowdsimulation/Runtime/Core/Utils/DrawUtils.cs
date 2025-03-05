using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DrawUtils
{
    /// <summary>
    /// Draws a simple arrow in the Scene View using Gizmos.
    /// </summary>
    /// <param name="start">The starting point of the arrow.</param>
    /// <param name="direction">The vector direction for the arrow.</param>
    /// <param name="size">Scales the arrow length and head.</param>
    /// <param name="color">The Gizmo color.</param>
    public static void DrawArrowGizmo(Vector3 start, Vector3 direction, float size, Color color)
    {
        if (direction == Vector3.zero) return;

        Gizmos.color = color;

        // Draw the arrow "shaft"
        Vector3 end = start + direction.normalized * size;
        Gizmos.DrawLine(start, end);

        // Draw the arrow "head" (two lines forming a V shape)
        float headSize = size * 0.2f;
        Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 160f, 0) * Vector3.forward;
        Vector3 left  = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 200f, 0) * Vector3.forward;

        Gizmos.DrawLine(end, end + right * headSize);
        Gizmos.DrawLine(end, end + left * headSize);
    }
}
