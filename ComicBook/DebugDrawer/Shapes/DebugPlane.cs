using Stride.Core.Mathematics;
using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stride.DebugDrawer.Shapes
{
    public class DebugPlane : Box
    {
        Vector3 normal = Vector3.UnitX;

        public DebugPlane(Color color, float scale = 1.0f)
            : base(color)
        {
            lines = new Line[6];
            Scale = new Vector3(scale);
        }

        public Vector3 Normal {
            get { return normal; }
            set {
                normal = value;
                CalculateLines();
            }
        }

        protected override void CalculateLines()
        {
            float w = Scale.X / 2.0f;
            float h = Scale.Y / 2.0f;

            // the first one is left-top, clock-wise
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(-w, -h, 0.0f);
            vertices[1] = new Vector3(w, -h, 0.0f);
            vertices[2] = new Vector3(w, h, 0.0f);
            vertices[3] = new Vector3(-w, h, 0.0f);

            Quaternion rotation = Quaternion.BetweenDirections(Vector3.UnitZ, normal);
            for (int i = 0; i < vertices.Length; i++)
            {
                rotation.Rotate(ref vertices[i]);
                vertices[i] += Transform.Position;
            }

            // contour
            lines[0] = new Line(vertices[0], vertices[1], Color);
            lines[1] = new Line(vertices[1], vertices[2], Color);
            lines[2] = new Line(vertices[2], vertices[3], Color);
            lines[3] = new Line(vertices[3], vertices[0], Color);

            // cross
            lines[4] = new Line(vertices[0], vertices[2], Color);
            lines[5] = new Line(vertices[1], vertices[3], Color);

            NotifyPropertyChanged();
        }
    }
}
