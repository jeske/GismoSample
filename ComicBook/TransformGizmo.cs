using SharpDX.Direct3D11;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Physics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;
using System;
using System.Collections.Generic;

namespace ComicBook
{
    /// <summary>
    /// Allows to translate and rotate entity
    /// </summary>
    class TransformGizmo
    {
        readonly Color ColorAxisX = Color.Red;
        readonly Color ColorAxisY = Color.Green;
        readonly Color ColorAxisZ = Color.Blue;
        readonly Color ColorAxisC = Color.White;

        readonly Entity gizmoRoot;
        readonly GraphicsDevice graphicsDevice;
        readonly Scene scene;

        bool isVisible;
        GizmoModes mode;

        Dictionary<Entity, GizmoModes> gizmoModesDict;
        Dictionary<GizmoModes, Entity> gizmoEntitiesDict;

        public TransformGizmo(GraphicsDevice graphicsDevice, ContentManager contentManager, Scene scene, Entity camera, Prefab prefab)
        {
            this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            this.scene = scene ?? throw new ArgumentNullException(nameof(scene));
            Camera = camera ?? throw new ArgumentNullException(nameof(camera));

            List<Entity> entities = prefab.Instantiate();
            if (entities.Count == 1)
            {
                gizmoRoot = entities[0];
            }
            else
            {
                throw new Exception("There are more than 1 root entity of gizmo");
            }

            // build gizmo modes dict
            gizmoModesDict = new Dictionary<Entity, GizmoModes>();
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationX"), GizmoModes.TranslationX);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationY"), GizmoModes.TranslationY);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationZ"), GizmoModes.TranslationZ);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationXZ"), GizmoModes.TranslationPlaneXZ);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationYZ"), GizmoModes.TranslationPlaneYZ);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationXY"), GizmoModes.TranslationPlaneXY);
            gizmoModesDict.Add(gizmoRoot.FindChild("TranslationCC"), GizmoModes.TranslationPlaneCamera);
            gizmoModesDict.Add(gizmoRoot.FindChild("RotationX"), GizmoModes.RotationX);
            gizmoModesDict.Add(gizmoRoot.FindChild("RotationY"), GizmoModes.RotationY);
            gizmoModesDict.Add(gizmoRoot.FindChild("RotationZ"), GizmoModes.RotationZ);
            gizmoModesDict.Add(gizmoRoot.FindChild("RotationC"), GizmoModes.RotationCamera);

            // build gizmo entities dict (opposite to gizmoModesDict)
            gizmoEntitiesDict = new Dictionary<GizmoModes, Entity>();
            foreach (Entity k in gizmoModesDict.Keys)
            {
                gizmoEntitiesDict.Add(gizmoModesDict[k], k);
            }

            // set materials
            SetEntityMaterial("TranslationX", ColorAxisX);
            SetEntityMaterial("TranslationYZ", ColorAxisX);
            SetEntityMaterial("RotationX", ColorAxisX);
            SetEntityMaterial("TranslationY", ColorAxisY);
            SetEntityMaterial("TranslationXZ", ColorAxisY);
            SetEntityMaterial("RotationY", ColorAxisY);
            SetEntityMaterial("TranslationZ", ColorAxisZ);
            SetEntityMaterial("TranslationXY", ColorAxisZ);
            SetEntityMaterial("RotationZ", ColorAxisZ);
            SetEntityMaterial("TranslationCC", ColorAxisC);
            SetEntityMaterial("RotationC", ColorAxisC);

            // initialize
            mode = GizmoModes.None;
            SetGizmoMode(GizmoModes.None);            
        }

        private Quaternion GetLookAtAngleQuat(Vector3 eye, Vector3 target)
        {
            Vector3 dir = eye - target;
            float pitch = (float)Math.Atan2(dir.Y, Math.Sqrt(dir.X * dir.X + dir.Z * dir.Z));
            float yaw = (float)Math.Atan2(dir.X, dir.Z);

            return Quaternion.RotationYawPitchRoll(yaw, -pitch, 0.0f);
        }

        public void Update()
        {
            // scaling
            gizmoRoot.Transform.Scale = new Vector3(0.25f); // TODO viewport-relative size
            foreach (Entity entity in gizmoModesDict.Keys)
            {
                // only works if we are updating it every frame
                entity.Get<RigidbodyComponent>().CanScaleShape = true;
            }

            // rotation ring and translation square that look at camera
            Entity rotationRing = gizmoEntitiesDict[GizmoModes.RotationCamera];
            Quaternion lookAtCameraRot = GetLookAtAngleQuat(rotationRing.Transform.GetWorldPosition(), Camera.Transform.GetWorldPosition());
            Quaternion rotZero = Quaternion.RotationYawPitchRoll(0.0f, 90.0f * 3.14f / 180.0f, 0.0f);

            // respect root entity rotation
            Quaternion rootRotation = gizmoRoot.Transform.Rotation;
            rootRotation.Invert();

            rotationRing.Transform.Rotation = rotZero * lookAtCameraRot * rootRotation;
        }

        public Plane GetTranslationPlane()
        {
            Vector3 normal;

            switch (Mode)
            {
                case GizmoModes.TranslationX:
                    normal = Vector3.UnitZ;
                    break;
                case GizmoModes.TranslationY:
                    normal = Vector3.UnitX;
                    break;
                case GizmoModes.TranslationZ:
                    normal = Vector3.UnitX;
                    break;
                case GizmoModes.TranslationPlaneXY:
                    normal = Vector3.UnitZ;
                    break;
                case GizmoModes.TranslationPlaneYZ:
                    normal = Vector3.UnitX;
                    break;
                case GizmoModes.TranslationPlaneXZ:
                    normal = Vector3.UnitY;
                    break;
                case GizmoModes.TranslationPlaneCamera:
                    normal = Camera.Transform.GetWorldPosition() - Root.Transform.GetWorldPosition();
                    normal.Normalize();
                    break;
                default:
                    throw new InvalidOperationException("Only valid for translation modes");
            }

            if (Mode != GizmoModes.TranslationPlaneCamera)
            {
                Quaternion rotation = Root.Transform.GetWorldRotation();
                rotation.Rotate(ref normal);
            }

            return new Plane(Root.Transform.GetWorldPosition(), normal);
        }

        public Vector3 GetTransformAxis(GizmoModes mode)
        {
            Vector3 axis;
            switch (mode)
            {
                case GizmoModes.None:
                    axis = Vector3.Zero;
                    break;
                case GizmoModes.TranslationX:
                    axis = Vector3.UnitX;
                    break;
                case GizmoModes.TranslationY:
                    axis = Vector3.UnitY;
                    break;
                case GizmoModes.TranslationZ:
                    axis = Vector3.UnitZ;
                    break;
                case GizmoModes.TranslationPlaneXY:
                    axis = new Vector3(1.0f, 1.0f, 0.0f);
                    break;
                case GizmoModes.TranslationPlaneYZ:
                    axis = new Vector3(0.0f, 1.0f, 1.0f);
                    break;
                case GizmoModes.TranslationPlaneXZ:
                    axis = new Vector3(1.0f, 0.0f, 1.0f);
                    break;
                case GizmoModes.TranslationPlaneCamera:
                    axis = new Vector3(1.0f, 1.0f, 1.0f);
                    break;
                case GizmoModes.RotationX:
                    axis = Vector3.UnitX;
                    break;
                case GizmoModes.RotationY:
                    axis = Vector3.UnitY;
                    break;
                case GizmoModes.RotationZ:
                    axis = Vector3.UnitZ;
                    break;
                case GizmoModes.RotationCamera:
                    axis = Camera.Transform.GetWorldPosition() - Root.Transform.GetWorldPosition();
                    axis.Normalize();
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (mode != GizmoModes.TranslationPlaneCamera && mode != GizmoModes.RotationCamera)
            {
                Quaternion rotation = Root.Transform.GetWorldTransformation().Rotation;
                rotation.Rotate(ref axis);
            }

            return axis;
        }

        public Vector3 GetTransformAxis()
        {
            return GetTransformAxis(Mode);
        }

        public bool IsVisible {
            get { return isVisible; }
            set {
                if (isVisible != value)
                {
                    isVisible = value;

                    if (isVisible)
                    {
                        scene.Entities.Add(gizmoRoot);
                    }
                    else
                    {
                        scene.Entities.Remove(gizmoRoot);
                    }
                }
            }
        }

        /// <summary>
        /// Returns gizmo mode (entity manipulation mode) associated with gizmo's entity.
        /// </summary>
        /// <param name="entityUnderMousePointer">Gizmo's entity under mouse pointer. If null or non-child then returns None</param>
        public GizmoModes GetGizmoModeForEntity(Entity entityUnderMousePointer)
        {
            if (entityUnderMousePointer != null && gizmoModesDict.ContainsKey(entityUnderMousePointer))
            {
                return gizmoModesDict[entityUnderMousePointer];
            }
            else
            {
                return GizmoModes.None;
            }
        }

        private void SetGizmoMode(GizmoModes mode)
        {
            // reset highlight
            foreach (Entity gizmoEntity in gizmoModesDict.Keys)
            {
                Material material = gizmoEntity.Get<ModelComponent>().Materials[0];
                material.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, 0.1f);
            }

            // set highlight
            if (mode != GizmoModes.None)
            {
                gizmoEntitiesDict[mode].Get<ModelComponent>().Materials[0].Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, 1.0f);
            }
        }

        private Material CreateSolidMaterial(Color color)
        {
            var descriptor = new MaterialDescriptor();
            var computeColor = new ComputeColor(color);

            var transparency = new MaterialTransparencyBlendFeature();
            transparency.Tint = new ComputeColor(Color.White);
            transparency.Alpha = new ComputeFloat(0.25f);

            descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(computeColor);
            descriptor.Attributes.Transparency = transparency;

            return Material.New(graphicsDevice, descriptor);
        }

        private void SetEntityMaterial(string name, Color color)
        {
            gizmoRoot.FindChild(name).Get<ModelComponent>().Materials[0] = CreateSolidMaterial(color);
        }

        public Entity Camera { get; set; }

        public GizmoModes Mode {
            get { return mode; }
            set {
                if (mode != value)
                {
                    mode = value;
                    SetGizmoMode(mode);
                }
            }
        }

        public Vector3 Position {
            get { return Root.Transform.Position; }
            set { Root.Transform.Position = value; }
        }

        public Quaternion Rotation {
            get { return Root.Transform.Rotation; }
            set { Root.Transform.Rotation = value; }
        }

        public bool IsTranslationMode {
            get {
                return Mode == GizmoModes.TranslationX
                    || Mode == GizmoModes.TranslationY
                    || Mode == GizmoModes.TranslationZ
                    || Mode == GizmoModes.TranslationPlaneXY
                    || Mode == GizmoModes.TranslationPlaneXZ
                    || Mode == GizmoModes.TranslationPlaneYZ
                    || Mode == GizmoModes.TranslationPlaneCamera;
            }
        }

        public bool IsRotationMode {
            get {
                return Mode == GizmoModes.RotationX
                    || Mode == GizmoModes.RotationY
                    || Mode == GizmoModes.RotationZ
                    || Mode == GizmoModes.RotationCamera;
            }
        }

        public bool IsPlanarTranslationMode {
            get {
                return Mode == GizmoModes.TranslationPlaneCamera
                    || Mode == GizmoModes.TranslationPlaneXY
                    || Mode == GizmoModes.TranslationPlaneXZ
                    || Mode == GizmoModes.TranslationPlaneYZ;
            }
        }

        public bool IsAxialTranslationMode {
            get { return IsTranslationMode && !IsPlanarTranslationMode; }
        }

        public Vector3 TranslationNormal {
            get {
                switch (Mode)
                {
                    case GizmoModes.TranslationX:
                        return new Vector3(0.0f, 1.0f, 0.0f);
                    case GizmoModes.TranslationY:
                        return new Vector3(1.0f, 0.0f, 0.0f);
                    case GizmoModes.TranslationZ:
                        return new Vector3(1.0f, 0.0f, 0.0f);
                    case GizmoModes.TranslationPlaneXY:
                        return new Vector3(0.0f, 0.0f, 1.0f);
                    case GizmoModes.TranslationPlaneYZ:
                        return new Vector3(1.0f, 0.0f, 0.0f);
                    case GizmoModes.TranslationPlaneXZ:
                        return new Vector3(0.0f, 1.0f, 0.0f);
                    case GizmoModes.TranslationPlaneCamera:
                        break;
                    default:
                        return Vector3.Zero;
                }

                return Vector3.Zero;
            }
        }

        /// <summary>
        /// Root entity with child ones (arrows, rings, etc)
        /// </summary>
        public Entity Root {
            get { return gizmoRoot; }
        }
    }
}
