using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace ComicBook
{
    /// <summary>
    /// Simple transform description (translation, rotation, scale)
    /// </summary>
    public struct TransformTRS
    {
        public Vector3 Position;

        public Quaternion Rotation;

        public Vector3 Scale;

        public TransformTRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            Position = position;
            Rotation = rotation;
            Scale = scale;
        }

        /// <summary>
        /// Creates struct using local transform
        /// </summary>
        public TransformTRS(TransformComponent transformComponent)
        {
            Position = transformComponent.Position;
            Rotation = transformComponent.Rotation;
            Scale = transformComponent.Scale;
        }

        public override string ToString()
        {
            return $"T = {Position} R = {Rotation} S = {Scale}";
        }
    }

    public static class Extensions
    {
        public static TransformTRS GetWorldTransformation(this TransformComponent transformComponent)
        {
            transformComponent.GetWorldTransformation(out Vector3 position, out Quaternion rotation, out Vector3 scale);
            return new TransformTRS(position, rotation, scale);
        }

        public static Vector3 GetWorldPosition(this TransformComponent transformComponent)
        {
            transformComponent.GetWorldTransformation(out Vector3 position, out Quaternion rotation, out Vector3 scale);
            return position;
        }

        public static Quaternion GetWorldRotation(this TransformComponent transformComponent)
        {
            transformComponent.GetWorldTransformation(out Vector3 position, out Quaternion rotation, out Vector3 scale);
            return rotation;
        }

        public static Vector3 GetWorldScale(this TransformComponent transformComponent)
        {
            transformComponent.GetWorldTransformation(out Vector3 position, out Quaternion rotation, out Vector3 scale);
            return scale;
        }

        public static void SetWorldPosition(this TransformComponent transformComponent, Vector3 position)
        {
            transformComponent.Position = transformComponent.WorldToLocal(position);
        }

        public static void SetWorldRotation(this TransformComponent transformComponent, Quaternion rotation)
        {
            Vector3 pos = Vector3.Zero, scale = Vector3.Zero;
            transformComponent.LocalToWorld(ref pos, ref rotation, ref scale);
            transformComponent.Rotation = rotation;
        }

        public static void SetWorldScale(this TransformComponent transformComponent, Vector3 scale)
        {
            transformComponent.Scale = transformComponent.WorldToLocal(scale);
        }

        /// <summary>
        /// Applies Abs(n) to each vector component
        /// </summary>
        public static Vector3 Abs(this Vector3 v)
        {
            return new Vector3(Math.Abs(v.X), Math.Abs(v.Y), Math.Abs(v.Z));
        }
    
        public static Vector2 ProjectToScreen(this Vector3 vector, Matrix viewProjectionMatrix)
        {
            Vector4 v = Vector3.Transform(vector, viewProjectionMatrix);
            v /= v.W;

            return new Vector2((v.X + 1.0f) / 2.0f, 1.0f - (v.Y + 1.0f) / 2.0f);
        }

        public static Vector2 ProjectToScreen(this Vector3 vector, CameraComponent camera)
        {
            return ProjectToScreen(vector, camera.ViewProjectionMatrix);
        }

        public static Vector3 ProjectToWorld(this Vector2 vector, Matrix viewProjectionMatrix, float z = 0.0f)
        {
            Vector3 v = new Vector3(vector, z);
            Matrix invMatrix = Matrix.Invert(viewProjectionMatrix);

            Vector4 p = Vector3.Transform(v, invMatrix);
            p /= p.W;

            return new Vector3(p.X, p.Y, p.Z);
        }

        public static Vector3 ProjectToWorld(this Vector2 vector, CameraComponent camera, float z = 0.0f)
        {
            return ProjectToWorld(vector, camera.ViewProjectionMatrix, z);
        }
    }
}
