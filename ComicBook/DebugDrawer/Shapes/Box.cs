using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.DebugDrawer.Shapes
{
    public class Box : AShape
    {
        public Vector3 Position {
            get => Transform.Position;
            set {
                Transform.Position = value;
                CalculateLines();
            }
        }

        public Vector3 Scale {
            get => Transform.Scale;
            set {
                Transform.Scale = value;
                CalculateLines();
            }
        }

        protected Line[] lines;
        protected readonly TransformComponent Transform;

        public override IEnumerable<Line> Lines => lines;

        public Box(Color color)
            : this(Vector3.Zero, Vector3.One, color)
        {
        }

        public Box(Vector3 position, Vector3 scale, Color color) 
            : base(color)
        {
            Transform = new TransformComponent();
            lines = new Line[12];

            Position = position;
            Scale = scale;

            CalculateLines();
        }

        protected virtual void CalculateLines()
        {
            float xHalf = Scale.X / 2;
            float yHalf = Scale.Y / 2;
            float zHalf = Scale.Z / 2;

            // bottom
            lines[0] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf), Color);
            lines[1] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf), Color);
            lines[2] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf), Color);
            lines[3] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf), Color);

            // vertical
            lines[4] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf), Color);
            lines[5] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf), Color);
            lines[6] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf), Color);
            lines[7] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y - yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf), Color);

            // top
            lines[8] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf), Color);
            lines[9] = new Line(
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf), Color);
            lines[10] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z + zHalf),
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf), Color);
            lines[11] = new Line(
                new Vector3(Transform.Position.X + xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf),
                new Vector3(Transform.Position.X - xHalf, Transform.Position.Y + yHalf, Transform.Position.Z - zHalf), Color);

            NotifyPropertyChanged();
        }
    }
}