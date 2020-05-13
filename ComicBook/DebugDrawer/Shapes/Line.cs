using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.DebugDrawer.Shapes
{
    public class Line : AShape
    {
        private Color _color;
        private Vector3 _end;
        private Vector3 _start;

        public Line(Vector3 start, Vector3 end, Color color) : base(color)
        {
            Start = start;
            End = end;
            Lines = new[] {this};
        }

        public Vector3 Start
        {
            get => _start;
            set
            {
                _start = value;
                NotifyPropertyChanged();
            }
        }

        public Vector3 End
        {
            get => _end;
            set
            {
                _end = value;
                NotifyPropertyChanged();
            }
        }

        public override Color Color
        {
            get => _color;
            set
            {
                _color = value;
                NotifyPropertyChanged();
            }
        }

        public override IEnumerable<Line> Lines { get; }
    }
}