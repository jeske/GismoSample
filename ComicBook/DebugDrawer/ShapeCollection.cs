using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.DebugDrawer.Shapes;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.DebugDrawer
{
    internal class ShapeCollection : IShapePropetyChangedHandler
    {
        
        private readonly GraphicsDevice graphicsDevice;
        private readonly GraphicsContext graphicsContext;
        private readonly ISet<AShape> shapes;
        private readonly VertexPositionColorTexture[] vertexArray;
        private readonly int[] indexArray;
        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private readonly VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[1024];
        private readonly int[] indices = new int[1024];

        public Entity Entity { get; set; }

        public bool IsModified { get; set; }
        public Color Color { get; private set; }

        public event Action<AShape> ColorChanged;

        public ShapeCollection(Color color, GraphicsDevice graphicsDevice, GraphicsContext graphicsContext)
        {
            Color = color;
            this.graphicsDevice = graphicsDevice;
            this.graphicsContext = graphicsContext;
            shapes = new HashSet<AShape>();

            vertexArray = new VertexPositionColorTexture[1024];
            indexArray = new int[1024];
            InitEntity(color, graphicsDevice);
        }

        private void InitEntity(Color color, GraphicsDevice graphicsDevice)
        {
            vertexBuffer = Buffer.Vertex.New(this.graphicsDevice, vertexArray, GraphicsResourceUsage.Dynamic);
            indexBuffer = Buffer.Index.New(this.graphicsDevice, indexArray, GraphicsResourceUsage.Dynamic);

            var model = new Model
            {
                Meshes = {
                    new Mesh {
                        Draw = new MeshDraw
                        {
                            PrimitiveType = PrimitiveType.LineList,
                            VertexBuffers = new[] {
                                new VertexBufferBinding(vertexBuffer,
                                    VertexPositionColorTexture.Layout,
                                    vertexArray.Length * VertexPositionColorTexture.Layout.CalculateSize())
                            },
                            IndexBuffer = new IndexBufferBinding(indexBuffer, true, indexArray.Length * sizeof(int)),
                            DrawCount = vertexArray.Length
                        }
                    }
                }, Materials = { Materials.CreateDebugMaterial(color, true, graphicsDevice) }
            };

            Entity = new Entity
            {
                Name = color.ToString(),
                Components = { new ModelComponent(model) }
            };
        }

        public void Add(AShape shape)
        {
            if(shape == null) throw new ArgumentNullException(nameof(shape));

            IsModified = true;

            shape.ChangeHandler = this;

            lock (shapes)
            {
                shapes.Add(shape);
            }
        }
        
        public bool Remove(AShape shape)
        {
            if (shape == null) throw new ArgumentNullException(nameof(shape));

            IsModified = true;

            lock (shapes)
            {
                return shapes.Remove(shape);
            }
        }

        public void UpdateMesh()
        {
            if(!IsModified) return;

            List<VertexPositionColorTexture> vertices = new List<VertexPositionColorTexture>();
            var indices = new LinkedList<int>();
            lock (shapes)
            {
                foreach (var line in shapes.SelectMany(shape => shape.Lines))
                {
                    int startIndex = OptionalInsert(line.Start, vertices);
                    int endIndex = OptionalInsert(line.End, vertices);
                    indices.AddLast(startIndex);
                    indices.AddLast(endIndex);
                }
            }

            vertexArray.CopyTo(this.vertices, 0);
            indexArray.CopyTo(this.indices, 0);

            vertices.CopyTo(this.vertices, 0);
            indices.CopyTo(this.indices, 0);

            vertexBuffer.SetData(graphicsContext.CommandList, this.vertices.ToArray());
            indexBuffer.SetData(graphicsContext.CommandList, this.indices.ToArray());

            IsModified = false;
        }

        private int OptionalInsert(Vector3 point, IList<VertexPositionColorTexture> verticeList)
        {
            var vertex = new VertexPositionColorTexture(point, Color, Vector2.Zero);
            int index = verticeList.IndexOf(vertex);
            if (index >= 0)
            {
                return index;
            }

            index = verticeList.Count;
            verticeList.Insert(index, vertex);
            return index;
        }

        public bool Contains(AShape aShape)
        {
            lock (shapes)
            {
                return shapes.Contains(aShape);
            }
        }

        ~ShapeCollection()
        {
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        public void OnPropertyChanged(AShape shape)
        {
            if (shape.Color != Color)
            {
                Remove(shape);
                ColorChanged?.Invoke(shape);
            }
            else
            {
                IsModified = true;
            }
        }

        public IEnumerable<AShape> Shapes {
            get { return shapes; }
        }
    }
}