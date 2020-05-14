using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Physics;
using System.Diagnostics;
using Stride.Rendering;
using Stride.DebugDrawer;

namespace ComicBook
{
    public class MainBehaviorScript : SyncScript
    {
        Entity selectedEntity, highlightedEntity;
        CameraComponent camera;
        Simulation simulation;
        SelectionBox selectionBox;
        SelectionBox highlightBox;
        TransformGizmo gizmo;
        Plane translationPlane;
        Plane rotationPlane;

        Entity manipulationEntity;
        TransformTRS entityTransform0;
        Vector3 entityOffset;
        Vector3 gizmoOffset0, gizmoOffset;
        Vector2 mousePositionPrev;
        float rotationAccumulator;

        /// <summary>
        /// Entity for manipulation
        /// </summary>
        public Entity SelectedEntity {
            get { return selectedEntity; }
            set {
                if (selectedEntity == value)
                {
                    return;
                }

                selectedEntity = value;
                if (selectionBox != null)
                {
                    selectionBox.Entity = value;
                    if (gizmo != null && selectedEntity != null)
                    {
                        gizmo.IsVisible = true;
                        gizmo.Root.Transform.Position = selectedEntity.Transform.GetWorldPosition();
                        gizmo.Root.Transform.Rotation = Quaternion.Identity;
                        gizmoOffset = gizmoOffset0 = Vector3.Zero;
                    }
                    else
                    {
                        gizmo.IsVisible = false;
                    }
                }
            }
        }

        /// <summary>
        /// Entity under mouse pointer
        /// </summary>
        public Entity HighlightedEntity {
            get { return highlightedEntity; }
            set {
                highlightedEntity = value;
                if (highlightBox != null)
                {
                    highlightBox.Entity = value;
                }
            }
        }

        public override void Start()
        {
            base.Start();

            camera = Entity.Components.Get<CameraComponent>();
            if (camera == null)
            {
                throw new Exception("Cannot get camera component from owner entity");
            }

            simulation = this.GetSimulation();
            if (simulation == null)
            {
                throw new Exception("Simulation is null");
            }

            // selection and highlight boxes
            Prefab selectionBoxPrefab = Content.Load<Prefab>("Gizmo/SelectionBox");
            if (selectionBoxPrefab != null)
            {
                selectionBox = new SelectionBox(selectionBoxPrefab, 1.0f);
                selectionBox.SetMaterial(Content.Load<Material>("Gizmo/SelectionMarkerMat"));

                highlightBox = new SelectionBox(selectionBoxPrefab, 1.05f);
                highlightBox.SetMaterial(Content.Load<Material>("Gizmo/HighlightMarkerMat"));
            }
            else
            {
                throw new Exception("Selection box prefab is null");
            }

            // gizmo
            gizmo = new TransformGizmo(Content, Entity.Scene, Entity, Content.Load<Prefab>("Gizmo/TransformGizmo"));
        }

        public override void Update()
        {
            if (camera == null || simulation == null || !Input.HasMouse)
            {
                return;
            }

            // LMB pressed
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                if (manipulationEntity == null)
                {
                    if (gizmo.Mode != GizmoModes.None)
                    {
                        // is shift down?
                        manipulationEntity = Input.IsKeyDown(Keys.LeftShift) || Input.IsKeyDown(Keys.RightShift)
                            ? gizmo.Root
                            : SelectedEntity;

                        entityTransform0 = manipulationEntity.Transform.GetWorldTransformation();

                        // translation
                        if (gizmo.IsTranslationMode)
                        {
                            translationPlane = gizmo.GetTranslationPlane();
                            if (FindMouseRayIntersectionWithAxisPlane(out Vector3 p, translationPlane, gizmo.Mode))
                            {
                                // translation mode start
                                entityOffset = p - entityTransform0.Position;
                                if (gizmo.IsAxialTranslationMode)
                                {
                                    if (!FindClosestPointToAxis(out entityOffset, gizmo.GetTransformAxis(), translationPlane.Normal, entityTransform0.Position, p))
                                    {
                                        manipulationEntity = null;
                                    }
                                }
                            }
                            else
                            {
                                manipulationEntity = null;
                            }
                        }

                        // rotation
                        else if (gizmo.IsRotationMode)
                        {
                            rotationPlane = new Plane(gizmo.Root.Transform.Position, gizmo.GetTransformAxis());
                            if (FindMouseRayIntersectionWithAxisPlane(out Vector3 p, rotationPlane, gizmo.Mode))
                            {
                                // rotation mode start
                                gizmoOffset0 = gizmoOffset;
                                rotationAccumulator = 0.0f;
                            }
                        }
                    }
                    else
                    {
                        // selection by mouse
                        SelectedEntity = GetEntityUnderMousePointer();
                    }
                }
            }

            // LMB released
            if (Input.IsMouseButtonReleased(MouseButton.Left))
            {
                manipulationEntity = null;
            }

            // LMB down
            if (Input.IsMouseButtonDown(MouseButton.Left))
            {
                if (manipulationEntity != null)
                {
                    Manipulate(manipulationEntity, entityTransform0, entityOffset);
                }
            }
            else
            {
                // highlight by mouse
                HighlightedEntity = GetEntityUnderMousePointer();

                // highlight gizmo part
                Entity gizmoEntity = GetEntityUnderMousePointer(CollisionFilterGroups.CustomFilter1, CollisionFilterGroupFlags.CustomFilter1);
                gizmo.Mode = gizmo.GetGizmoModeForEntity(gizmoEntity);
            }

            DebugUpdate();

            // move gizmo to manipulation entity
            if (manipulationEntity != null)
            {
                manipulationEntity.Transform.UpdateWorldMatrix();
                if (manipulationEntity == gizmo.Root)
                {
                    gizmoOffset = SelectedEntity.Transform.GetWorldPosition() - gizmo.Root.Transform.Position;
                }
                else
                {
                    if (gizmo.IsRotationMode)
                    {
                        // rotate entity around gizmo
                        SelectedEntity.Transform.Position = gizmo.Root.Transform.Position + gizmoOffset;
                    }
                    else
                    {
                        gizmo.Root.Transform.Position = manipulationEntity.Transform.GetWorldPosition() - gizmoOffset;
                    }
                }
            }

            gizmo.Update();
            mousePositionPrev = Input.MousePosition;
        }

        private Vector3[] ProjectMouseToWorld()
        {
            Vector3 mousePosition = new Vector3(Input.MousePosition.X, Input.MousePosition.Y, 0.0f);

            Matrix invViewMatrix = Matrix.Invert(camera.ViewProjectionMatrix);
            Vector3 screenPosition = new Vector3(
                mousePosition.X * 2.0f - 1.0f,
                1.0f - mousePosition.Y * 2.0f,
                0.0f
            );

            Vector4 raycastNearPoint = Vector3.Transform(screenPosition, invViewMatrix);
            raycastNearPoint /= raycastNearPoint.W;

            screenPosition.Z = 1.0f;
            Vector4 raycastFarPoint = Vector3.Transform(screenPosition, invViewMatrix);
            raycastFarPoint /= raycastFarPoint.W;

            return new Vector3[2] { raycastNearPoint.XYZ(), raycastFarPoint.XYZ() };
        }

        private Ray GetMouseRay()
        {
            Vector3[] mousePoints = ProjectMouseToWorld();

            Vector3 dir = mousePoints[1] - mousePoints[0];
            dir.Normalize();

            return new Ray(mousePoints[0], dir);
        }

        private Entity GetEntityUnderMousePointer(CollisionFilterGroups filterGroup = CollisionFilterGroups.DefaultFilter, CollisionFilterGroupFlags flags = CollisionFilterGroupFlags.DefaultFilter)
        {
            Vector3[] mousePoints = ProjectMouseToWorld();
            HitResult hitResult = simulation.Raycast(mousePoints[0], mousePoints[1], filterGroup, flags);

            return hitResult.Succeeded
                ? hitResult.Collider.Entity
                : null;
        }

        private bool FindMouseRayIntersectionWithAxisPlane(out Vector3 intersectionPoint, Plane plane, GizmoModes mode)
        {
            plane.D *= -1.0f;

            Ray mouseRay = GetMouseRay();
            if (plane.Intersects(ref mouseRay, out Vector3 point))
            {
                intersectionPoint = point;
                return true;
            }
            else
            {
                intersectionPoint = Vector3.Zero;
                return false;
            }
        }

        private bool FindClosestPointToAxis(out Vector3 result, Vector3 axis, Vector3 translationPlaneNormal, Vector3 axisOrigin, Vector3 point)
        {
            Vector3 crossProduct = Vector3.Cross(axis, translationPlaneNormal);

            Vector3 a1 = -axis * 100.0f + axisOrigin;
            Vector3 a2 = axis * 100.0f + axisOrigin;
            Vector3 b1 = -crossProduct * 100.0f + point;
            Vector3 b2 = crossProduct * 100.0f + point;

            return Helpers.FindLineIntersection(out result, a1, a2, b1, b2);
        }

        /// <summary>
        /// Perform entity manipulation with gizmo
        /// </summary>
        /// <param name="entity">Entity to manipulate</param>
        /// <param name="transform0">Entity transform before manipulation start (zero transform)</param>
        /// <param name="offset">Offset between entity and mouse cursor</param>
        private void Manipulate(Entity entity, TransformTRS transform0, Vector3 offset)
        {
            Vector3 axis = gizmo.GetTransformAxis();

            // translation
            if (gizmo.IsTranslationMode)
            {
                if (FindMouseRayIntersectionWithAxisPlane(out Vector3 p, translationPlane, gizmo.Mode))
                {
                    if (gizmo.IsPlanarTranslationMode)
                    {
                        // planar translation
                        entity.Transform.Position = (p - offset) * axis + transform0.Position * (Vector3.One - axis);
                    }
                    else
                    {
                        // axial translation
                        if (FindClosestPointToAxis(out Vector3 point, axis, translationPlane.Normal, transform0.Position, p))
                        {
                            entity.Transform.Position = point - offset + transform0.Position;
                        }
                    }
                }
            }
            // rotation
            else if (gizmo.IsRotationMode)
            {
                // rotation around gizmo center in screen-space
                Vector2 rotCenter = gizmo.Position.ProjectToScreen(camera);
                float delta = Helpers.AngleBetween(mousePositionPrev - rotCenter, Input.MousePosition - rotCenter);

                // negate rotation delta if we are rotating from gizmo back side
                Vector3 screenSpaceFactor = Vector3.TransformNormal(gizmo.GetTransformAxis(), camera.ViewProjectionMatrix);
                if (screenSpaceFactor.Z > 0.0f)
                {
                    delta = -delta;
                }

                rotationAccumulator += delta;
                Quaternion rotation = Quaternion.RotationAxis(axis, -rotationAccumulator);
                entity.Transform.Rotation = transform0.Rotation * rotation;

                if (entity != gizmo.Root)
                {
                    gizmoOffset = gizmoOffset0;
                    rotation.Rotate(ref gizmoOffset);
                }
                else
                {
                    entity.Transform.Rotation = transform0.Rotation * rotation;
                }
            }
        }

        private void DebugUpdate()
        {
            if (gizmo.IsTranslationMode && FindMouseRayIntersectionWithAxisPlane(out Vector3 pos, translationPlane, gizmo.Mode))
            {
                DebugDrawerSystem.DrawBox(pos, 0.1f, Color.Yellow);
                DebugDrawerSystem.DrawPlane(gizmo.Root.Transform.GetWorldPosition(), translationPlane.Normal, 3.0f, Color.Yellow);
            }

            if (gizmo.IsRotationMode && FindMouseRayIntersectionWithAxisPlane(out Vector3 p, rotationPlane, gizmo.Mode))
            {
                DebugDrawerSystem.DrawBox(p, 0.1f, Color.Yellow);
                DebugDrawerSystem.DrawPlane(gizmo.Root.Transform.GetWorldPosition(), rotationPlane.Normal, 3.0f, Color.Yellow);
            }

            Vector3 axis = gizmo.GetTransformAxis();
            DebugDrawerSystem.DrawLine(axis * -10.0f + gizmo.Root.Transform.Position, axis * 10.0f + gizmo.Root.Transform.Position, Color.Red);

            Vector3 axisX = gizmo.GetTransformAxis(GizmoModes.TranslationX);
            Vector3 axisY = gizmo.GetTransformAxis(GizmoModes.TranslationY);
            Vector3 axisZ = gizmo.GetTransformAxis(GizmoModes.TranslationZ);
            DebugDrawerSystem.DrawAxis(axisX, gizmo.Root.Transform.Position, Color.Red);
            DebugDrawerSystem.DrawAxis(axisY, gizmo.Root.Transform.Position, Color.Green);
            DebugDrawerSystem.DrawAxis(axisZ, gizmo.Root.Transform.Position, Color.Blue);
        }
    }
}
