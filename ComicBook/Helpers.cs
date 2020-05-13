using Stride.Core.Mathematics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ComicBook
{
    public static class Helpers
    {
        /// <summary>
        /// Finds intersection point of two lines. Returns false if A and B don't intersect
        /// </summary>
        /// <param name="intersectionPoint">Point of intersection (only valid if result is true)</param>
        /// <param name="a1">Line A start</param>
        /// <param name="a2">Line A end</param>
        /// <param name="b1">Line B start</param>
        /// <param name="b2">Line B end</param>
        /// <returns>True if there is intersection of A and B</returns>
        public static bool FindLineIntersection(out Vector2 intersectionPoint, Vector2 a1, Vector2 a2, Vector2 b1, Vector2 b2)
        {
            const float zero = 0.00001f;

            float d = (a1.X - a2.X) * (b2.Y - b1.Y) - (a1.Y - a2.Y) * (b2.X - b1.X);
            float da = (a1.X - b1.X) * (b2.Y - b1.Y) - (a1.Y - b1.Y) * (b2.X - b1.X);
            float db = (a1.X - a2.X) * (a1.Y - b1.Y) - (a1.Y - a2.Y) * (a1.X - b1.X);

            if (Math.Abs(d) <= zero)
            {
                intersectionPoint = Vector2.Zero;
                return false;
            }

            float ta = da / d;
            float tb = db / d;

            if (ta > zero && ta < 1.0f && tb > zero && tb < 1.0f)
            {
                intersectionPoint = new Vector2(a1.X + ta * (a2.X - a1.X), a1.Y + ta * (a2.Y - a1.Y));
                return true;
            }
            else
            {
                intersectionPoint = Vector2.Zero;
                return false;
            }
        }

        // https://stackoverflow.com/questions/2316490/the-algorithm-to-find-the-point-of-intersection-of-two-3d-line-segment
        public static bool FindLineIntersection(out Vector3 intersectionPoint, Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
        {
            Vector3 da = a2 - a1;
            Vector3 db = b2 - b1;
            Vector3 dc = b1 - a1;

            /*
            if (Vector3.Dot(dc, Vector3.Cross(da, db)) != 0.0f)
            {
                // lines are not complanar
                intersectionPoint = Vector3.Zero;
                return false;
            }
            */
            float s = Vector3.Dot(Vector3.Cross(dc, db), Vector3.Cross(da, db)) / Vector3.Cross(da, db).LengthSquared();
            if (s >= 0.0f && s <= 1.0f)
            {
                intersectionPoint = a1 + da * new Vector3(s, s, s);
                return true;
            }
            else
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }
        }

        /// <summary>
        /// Returns angle in radians
        /// </summary>
        public static float AngleBetween(Vector2 vector1, Vector2 vector2)
        {
            double sin = vector1.X * vector2.Y - vector2.X * vector1.Y;
            double cos = vector1.X * vector2.X + vector1.Y * vector2.Y;

            return (float)Math.Atan2(sin, cos);
        }

        /// <summary>
        /// Returns angle in radians
        /// </summary>
        public static float AngleBetween(Vector3 vector1, Vector3 vector2)
        {
            vector1.Normalize();
            vector2.Normalize();

            Debug.WriteLine(Vector3.Dot(vector1, vector2));

            return Vector3.Dot(vector1, vector2) < 0.0f
                ? 3.1415927f - 2.0f * (float)Math.Asin((-vector1 - vector2).Length() / 2.0f)
                : 2.0f * (float)Math.Asin((vector1 - vector2).Length() / 2.0f);
        }
    }
}
