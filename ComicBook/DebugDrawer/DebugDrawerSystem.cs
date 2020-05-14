using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Stride.DebugDrawer.Shapes;

namespace Stride.DebugDrawer
{
    public class DebugDrawerSystem : GameSystemBase
    {
        public static DebugDrawerSystem Instance;

        private readonly IGame game;
        private readonly IDictionary<Color, ShapeCollection> shapeCollections;
        private Entity rootEntity;
        private SceneSystem sceneSystem;

        public DebugDrawerSystem(IGame game) : base(game.Services)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));

            shapeCollections = new ConcurrentDictionary<Color, ShapeCollection>();
            this.game = game;
            Enabled = true;
            Visible = true;
            Instance = this;
        }

        public void Add<T>(T shape) where T : AShape
        {
            if (Equals(shape, default(T)))
                throw new ArgumentException(nameof(shape));

            var shapeCollection = EnsureEntities(shape.Color);
            shapeCollection.Add(shape);
        }

        public override bool BeginDraw()
        {
            foreach (var shapeCollection in shapeCollections.Values)
            {
                shapeCollection.UpdateMesh();
                foreach (AShape shape in shapeCollection.Shapes)
                {
                    shape.CountOfDraws++;
                }
            }

            return true;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            float deltaTime = (float)gameTime.Elapsed.TotalSeconds;
            var shapesToRemove = new List<AShape>();
            
            foreach (var shapeCollection in shapeCollections.Values)
            {
                foreach (AShape shape in shapeCollection.Shapes)
                {
                    if (!float.IsInfinity(shape.Lifetime))
                    {
                        shape.Lifetime -= deltaTime;
                        if (shape.Lifetime <= 0.0f && shape.CountOfDraws > 0)
                        {
                            shapesToRemove.Add(shape);
                        }
                    }
                }

                foreach (AShape shape in shapesToRemove)
                {
                    shapeCollection.Remove(shape);
                }
                shapesToRemove.Clear();
            }
        }

        public static void DrawLine(Vector3 a, Vector3 b, Color color, float lifetime = -1.0f)
        {
            Line line = new Line(a, b, color);
            line.Lifetime = lifetime;
            Instance.Add(line);
        }

        public static void DrawBox(Vector3 position, Vector3 scale, Color color, float lifetime = -1.0f)
        {
            Box box = new Box(position, scale, color);
            box.Lifetime = lifetime;
            Instance.Add(box);
        }

        public static void DrawBox(Vector3 position, float scale, Color color, float lifetime = -1.0f)
        {
            DrawBox(position, new Vector3(scale), color, lifetime);
        }

        public static void DrawPlane(Vector3 position, Vector3 normal, float scale, Color color, float lifetime = -1.0f)
        {
            DebugPlane plane = new DebugPlane(color, scale);
            plane.Position = position;
            plane.Normal = normal;
            plane.Lifetime = lifetime;
            Instance.Add(plane);
        }

        private ShapeCollection EnsureEntities(Color color)
        {
            if (rootEntity == null)
            {
                rootEntity = new Entity
                {
                    Name = "DebugRoot"
                };
                sceneSystem = game.Services.GetService<SceneSystem>();
                sceneSystem.SceneInstance.RootScene.Entities.Add(rootEntity);
            }

            return EnsureCollection(color);
        }

        private ShapeCollection EnsureCollection(Color color)
        {
            if (shapeCollections.TryGetValue(color, out var shapeCollection)) return shapeCollection;

            shapeCollection = new ShapeCollection(color, GraphicsDevice, game.GraphicsContext);
            shapeCollection.ColorChanged += OnColorChanged;
            shapeCollections[color] = shapeCollection;
            rootEntity.AddChild(shapeCollection.Entity);

            return shapeCollection;
        }

        private void OnColorChanged(AShape shape)
        {
            if(shape == null) throw new ArgumentNullException(nameof(shape));

            ShapeCollection shapeCollection = EnsureEntities(shape.Color);
            shapeCollection.Add(shape);
        }

        public void Clear()
        {
            foreach (var shapeCollection in shapeCollections.Values)
            {
                Delete(shapeCollection);
            }
        }

        private void Delete(ShapeCollection shapeCollection)
        {
            rootEntity.RemoveChild(shapeCollection.Entity);
            shapeCollection.ColorChanged -= OnColorChanged;
            shapeCollections.Remove(shapeCollection.Color);
        }
    }
}