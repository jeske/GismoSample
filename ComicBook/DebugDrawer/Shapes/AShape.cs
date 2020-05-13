using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.DebugDrawer.Shapes
{
    public abstract class AShape
    {
        IShapePropetyChangedHandler changeHandler;
        Color color;

        public float Lifetime = float.PositiveInfinity;

        public int CountOfDraws = 0;

        internal IShapePropetyChangedHandler ChangeHandler
        {
            get { return changeHandler; }
            set { changeHandler = value; }
        }

        public virtual Color Color
        {
            get { return color; }
            set
            {
                color = value;
                NotifyPropertyChanged();
            }
        }

        public abstract IEnumerable<Line> Lines { get; }

        protected AShape(Color color)
        {
            Color = color;
        }

        protected void NotifyPropertyChanged()
        {
            if (ChangeHandler != null)
            {
                ChangeHandler.OnPropertyChanged(this);
            }
        }
    }
}