using System;
using UnityEngine;
using Vector2 = System.Numerics.Vector2;


namespace ChaosCompany.Scripts.Extensions;

public static class Vector2Extension
{
    /// <summary>
    /// Rotate vector2 by a degrees value
    /// </summary>
    /// <param name="degrees">The degrees value</param>
    /// <returns>The rotated vector2</returns>
    public static Vector2 RotateDegrees(this Vector2 value, float degrees)
    {
        float radians = Mathf.Deg2Rad * degrees;

        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);

        return new Vector2(cos * value.X - sin * value.Y, sin * value.X + cos * value.Y);
    }
}
